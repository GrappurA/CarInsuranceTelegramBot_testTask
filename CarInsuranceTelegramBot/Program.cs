using CarInsuranceTelegramBot;
using Mindee;
using Mindee.Input;
using Mindee.Product.DriverLicense;
using PdfSharp.Fonts;
using PdfSharp.Quality;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MyTelegramBot
{
	class Program
	{
		static long? myTelegramId = 875371626;
		static string telegramToken = File.ReadAllText("G:\\Studying\\telegramBot\\config\\telegram.txt");
		static string mindeeApiKey = File.ReadAllText("G:\\Studying\\telegramBot\\config\\mindee.txt");
		static string GeminiApiKey = File.ReadAllText("G:\\Studying\\telegramBot\\config\\gemini.txt");

		static string helpMessage = "/start - Get started with the bot\n/help - Display help message.";
		static string photoReceivedMessage = "I received your info. Please wait while I analyze it.\n<b>Processing...</b>";
		static string greetingsMessage = "<b>Greetings!</b>\nI am here to help you buy insurance.\nPlease send a photo of your vehicle registration certificate";
		static string pricingMessage = "Our current insurance price is <b>100 USD</b>.\nAre you comfortable with the price?";
		static string verifyPricingMessage = "Unfortunately, <b>100 USD</b> is our only pricing plan.";
		static string pricingAcceptedMessage = "<b>Great!</b>\nGenerating your insurance policy...";
		static string unplannedMessage = "Please, send the documents as requested.\nI am not designed to respond to messages other than those needed to provide you with <b>insurance policy</b>";
		static string botWorkflowExpalinedMessage = "If you're interested, i am going to scan your documents, reveal all neccessary data to create an <b>insurance policy</b>.\nYour data being:<b>Name, Age, License Number, License Class etc.</b>";
		static string registrationConfirmedMessage = "<b>✅Great!\n</b>Now, send the photo of your driving license please.";

		static DrivingLicenseInfo driverLicenseInfo = new();
		static VehicleInfo vehicleInfo = new();

		static Dictionary<long, BotState> userStates = new();
		static int verifyMessageId;
		static Message? verifyMessage;
		static Message? editPricingMessage;
		static Message? editRegistrationMessage;
		static Message? editLicenseMessage;

		static bool flag = false;

		static Queue<registrationFieldToConfirm> registrationFields = new Queue<registrationFieldToConfirm>();
		static registrationFieldToConfirm currentRegistrationField;
		static InlineKeyboardMarkup verifyRegistrationKeyboard = new InlineKeyboardMarkup(new[]
					{
						new[]
						{
							InlineKeyboardButton.WithCallbackData("✅ Yes", "registration_yes"),
							InlineKeyboardButton.WithCallbackData("❌ No", "registration_no")
						}
					});

		static Queue<licenseFieldToConfirm> licenseFields = new Queue<licenseFieldToConfirm>();
		static licenseFieldToConfirm currentLicenseField;
		static InlineKeyboardMarkup verifyLicenseKeyboard = new InlineKeyboardMarkup(new[]
					{
						new[]
						{
							InlineKeyboardButton.WithCallbackData("✅ Yes", "license_yes"),
							InlineKeyboardButton.WithCallbackData("❌ No", "license_no")
						}
					});

		static async Task Main(string[] args)
		{
			try
			{
				Host bot = new(telegramToken);
				await bot.Start();
				bot.OnMessage += OnMessage;
				bot.OnCallback += OnCallback;

				Console.WriteLine("Bot is running. Press Enter to quit.");
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Unhandled exception: " + ex);
				Console.WriteLine("Press Enter to exit.");
				Console.ReadLine();
			}
		}

		private static string GetRegistrationField(registrationFieldToConfirm field, VehicleInfo info)
		{
			return field switch
			{
				registrationFieldToConfirm.Vin => info.Vin,
				registrationFieldToConfirm.Plate => info.Plate,
				registrationFieldToConfirm.Brand => info.Brand,
				registrationFieldToConfirm.Year => info.Year,
				_ => "N/A"
			};
		}

		private static string GetLicenseField(licenseFieldToConfirm field, DrivingLicenseInfo info)
		{
			return field switch
			{
				licenseFieldToConfirm.Name => info.fullName,
				licenseFieldToConfirm.DateOfBirth => info.dateOfBirth,
				licenseFieldToConfirm.LicenseNumber => info.licenseNumber,
				licenseFieldToConfirm.LicenseClass => info.licenseClass,
				licenseFieldToConfirm.ExpiryDate => info.expiryDate,
				licenseFieldToConfirm.Adress => info.countryCode,
				_ => "N/A"
			};
		}

		private static async Task<byte[]> DownloadPhotoAsync(ITelegramBotClient client, PhotoSize photo)
		{
			try
			{
				var fileInfo = await client.GetFile(photo.FileId);
				if (fileInfo.FilePath == null)
					return null;

				var fileStream = new MemoryStream();
				await client.DownloadFile(fileInfo.FilePath, fileStream);
				return fileStream.ToArray();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error downloading photo: " + ex.Message);
				return null;
			}
		}

		private static async void OnMessage(ITelegramBotClient client, Update update)
		{
			long id = update.Message?.Chat.Id ?? myTelegramId ?? 0;
			string text = update.Message?.Text ?? "";

			if (!userStates.ContainsKey(id))
				userStates[id] = BotState.Start;

			if (update.Type == UpdateType.Message)
			{
				Console.WriteLine($"New Message: [{id}] -> [{update.Message?.Type}] => {text}");

				switch (text)
				{
					case "/start":
						await client.SendMessage(id, greetingsMessage, parseMode: ParseMode.Html);
						userStates[id] = BotState.sendRegistration;
						flag = false;
						return;
					case "/help":
						await client.SendMessage(id, helpMessage);
						return;

					case "/debug":
						await client.SendMessage(id, "send photo");
						userStates[id] = BotState.sendLicense;

						return;
				}

				if (update.Message?.Type == MessageType.Photo && userStates[id] == BotState.sendLicense)
				{
					await client.SendMessage(id, photoReceivedMessage, parseMode: ParseMode.Html);

					try
					{
						var photo = update.Message.Photo[^1];
						var photoBytes = await DownloadPhotoAsync(client, photo);

						if (photoBytes == null)
						{
							await client.SendMessage(id,
								"Couldn't analyze picture, please, retake the photo and send it again");
							return;
						}

						ExtractedData extractedData = new();
						driverLicenseInfo = await extractedData.GetDriverLicense(photo, photoBytes);
						Console.WriteLine(Path.GetPathRoot(mindeeApiKey));

						licenseFields.Enqueue(licenseFieldToConfirm.Name);
						licenseFields.Enqueue(licenseFieldToConfirm.DateOfBirth);
						licenseFields.Enqueue(licenseFieldToConfirm.LicenseNumber);
						licenseFields.Enqueue(licenseFieldToConfirm.LicenseClass);
						licenseFields.Enqueue(licenseFieldToConfirm.ExpiryDate);
						licenseFields.Enqueue(licenseFieldToConfirm.Adress);

						currentLicenseField = licenseFields.Peek();

						vehicleInfo = await ExtractedData.GetVehicleRegistarion(photo, photoBytes);

						string message = $"Is this data correct?\n" +
							$"<b>{currentLicenseField}: {GetLicenseField(currentLicenseField, driverLicenseInfo)}</b>";

						editLicenseMessage = await client.SendMessage(id, text: message,
							replyMarkup: verifyLicenseKeyboard,
							parseMode: ParseMode.Html
							);
						userStates[id] = BotState.ConfirmLicense;

					}
					catch (Exception ex)
					{
						Console.WriteLine("Error processing driver's licence: " + ex.Message);
						await client.SendMessage(
							id,
						"❌ Error processing your driver license." +
						" Please make sure the photo is clear and try again.");
					}
				}

				else if (update.Message?.Type == MessageType.Text && userStates[id] == BotState.Start && userStates[id] != BotState.EnterRegistrationField && userStates[id] != BotState.EnterLicenseField)
				{
					await client.SendMessage(id, "Type /start to begin.");
				}

				else if (update.Message.Type == MessageType.Text && userStates[id] != BotState.Start && userStates[id] != BotState.EnterRegistrationField && userStates[id] != BotState.EnterLicenseField)
				{
					string input = update.Message.Text;
					if (input.Contains("what") || input.Contains("why") || input.Contains("when"))
					{
						await client.SendMessage(id, text: botWorkflowExpalinedMessage, parseMode: ParseMode.Html);
					}
					else
						await client.SendMessage(id, parseMode: ParseMode.Html, text: unplannedMessage);

				}

				else if (update.Message.Type == MessageType.Photo && userStates[id] == BotState.sendRegistration)
				{
					var photo = update.Message.Photo[^1];
					var photoBytes = await DownloadPhotoAsync(client, photo);

					userStates[id] = BotState.ConfirmRegistration;

					registrationFields.Enqueue(registrationFieldToConfirm.Vin);
					registrationFields.Enqueue(registrationFieldToConfirm.Plate);
					registrationFields.Enqueue(registrationFieldToConfirm.Brand);
					registrationFields.Enqueue(registrationFieldToConfirm.Year);

					currentRegistrationField = registrationFields.Peek();

					vehicleInfo = await ExtractedData.GetVehicleRegistarion(photo, photoBytes);

					string message = $"Is this data correct?\n" +
						$"<b>{currentRegistrationField}: {GetRegistrationField(currentRegistrationField, vehicleInfo)}</b>";

					editRegistrationMessage = await client.SendMessage(id, text: message,
						replyMarkup: verifyRegistrationKeyboard,
						parseMode: ParseMode.Html
						);
				}

				else if (update.Message.Type == MessageType.Text && userStates[id] == BotState.EnterRegistrationField)
				{
					switch (currentRegistrationField)
					{
						case registrationFieldToConfirm.Vin:
							vehicleInfo.Vin = update.Message.Text;
							break;
						case registrationFieldToConfirm.Plate:
							vehicleInfo.Plate = update.Message.Text;
							break;
						case registrationFieldToConfirm.Brand:
							vehicleInfo.Brand = update.Message.Text;
							break;
						case registrationFieldToConfirm.Year:
							vehicleInfo.Year = update.Message.Text;
							break;
						default:
							break;
					}

					registrationFields.Dequeue();
					currentRegistrationField = registrationFields.Peek();

					string message = $"Is this data correct?\n" +
						$"<b>{currentRegistrationField}: {GetRegistrationField(currentRegistrationField, vehicleInfo)}</b>";

					Console.WriteLine($"{vehicleInfo.Vin}\n" +
						$"{vehicleInfo.Plate}\n" +
						$"{vehicleInfo.Brand}\n" +
						$"{vehicleInfo.Year}");

					await client.EditMessageText(id, editRegistrationMessage.Id,
						text: message,
						parseMode: ParseMode.Html,
						replyMarkup: verifyRegistrationKeyboard);

					userStates[id] = BotState.ConfirmRegistration;
				}

				else if (update.Type == UpdateType.Message && userStates[id] == BotState.EnterLicenseField)
				{
					switch (currentLicenseField)
					{
						case licenseFieldToConfirm.Name:
							driverLicenseInfo.fullName = update.Message.Text;
							break;
						case licenseFieldToConfirm.DateOfBirth:
							driverLicenseInfo.dateOfBirth = update.Message.Text;
							break;
						case licenseFieldToConfirm.LicenseNumber:
							driverLicenseInfo.licenseNumber = update.Message.Text;

							break;
						case licenseFieldToConfirm.LicenseClass:
							driverLicenseInfo.licenseClass = update.Message.Text;

							break;
						case licenseFieldToConfirm.ExpiryDate:
							driverLicenseInfo.expiryDate = update.Message.Text;

							break;
						case licenseFieldToConfirm.Adress:
							driverLicenseInfo.countryCode = update.Message.Text;

							break;

						default:
							break;
					}
					licenseFields.Dequeue();

					string message = $"Is this data correct?\n" +
						$"<b>{currentLicenseField}: {GetLicenseField(currentLicenseField, driverLicenseInfo)}</b>";

					Console.WriteLine($"{driverLicenseInfo.fullName}\n" +
						$"{driverLicenseInfo.licenseNumber}\n" +
						$"{driverLicenseInfo.licenseClass}\n" +
						$"{driverLicenseInfo.expiryDate}");

					await client.EditMessageText(id, editLicenseMessage.Id,
						text: message,
						parseMode: ParseMode.Html,
						replyMarkup: verifyLicenseKeyboard);

					userStates[id] = BotState.ConfirmLicense;
				}

			}
		}

		private static async void OnCallback(ITelegramBotClient client, CallbackQuery? callbackQuery)
		{
			long id = callbackQuery.Message?.Chat.Id ?? myTelegramId ?? 0;
			string data = callbackQuery.Data ?? "";

			if (!userStates.ContainsKey(id))
				userStates[id] = BotState.Start;

			var pricingButtons = new InlineKeyboardMarkup(new[]
			{
				new[]
				{
					InlineKeyboardButton.WithCallbackData("✅ Yes", "pricing_yes"),
					InlineKeyboardButton.WithCallbackData("❌ No", "pricing_no")
				}
			});
			switch (data)
			{
				case "license_yes" when userStates[id] == BotState.ConfirmLicense:
					if (licenseFields.Count > 1)
					{
						licenseFields.Dequeue();
						currentLicenseField = licenseFields.Peek();

						string message = $"Is this data correct?\n" +
							$"<b>{currentLicenseField}: {GetLicenseField(currentLicenseField, driverLicenseInfo)}</b>";

						await client.EditMessageText(id, editLicenseMessage.Id, text: message, replyMarkup: verifyLicenseKeyboard, parseMode: ParseMode.Html);
					}
					else
					{
						userStates[id] = BotState.AgreeToPricing;
						await client.EditMessageText(id,
							editLicenseMessage.Id,
							text: "<b>Great!</b>\nAll license data processed!\nWe will now continue to pricing",
							parseMode: ParseMode.Html
							);

						editPricingMessage = await client.SendMessage(id,
								text: "Currently our only pricing plan is<b>100 USD</b>, are you comfortable with the price?",
								parseMode: ParseMode.Html,
								replyMarkup: pricingButtons);
					}
					break;

				case "license_no" when userStates[id] == BotState.ConfirmLicense:

					await client.EditMessageText(id,
						editLicenseMessage.Id,
						"❌ Please, enter the field manually.");
					userStates[id] = BotState.EnterLicenseField;
					break;

				case "pricing_yes" when userStates[id] == BotState.AgreeToPricing:
					await client.EditMessageText(id,
						messageId: editPricingMessage.Id,
						pricingAcceptedMessage,
						parseMode: ParseMode.Html
						);

					userStates[id] = BotState.sendPolicy;
					//policy here

					GlobalFontSettings.FontResolver = new DefaultFontResolver_unused();
					var policyBytes = PolicyPdfGenerator.GeneratePolicyPdf(driverLicenseInfo, vehicleInfo);

					Console.WriteLine($"Generated PDF length: {policyBytes?.Length}");

					// 1. Save to local file for inspection
					var fileName = $"CarInsurancePolicy_{DateTime.Today:yyyy-MM-dd}.pdf";
					var filePath = Path.Combine(AppContext.BaseDirectory, fileName); // or any folder you choose
					try
					{
						File.WriteAllBytes(filePath, policyBytes);
						Console.WriteLine($"PDF saved locally at: {filePath}");
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Error saving PDF locally: {ex.Message}");
					}

					// 2. Send the saved file via Telegram
					try
					{
						using var fileStream = File.OpenRead(filePath);
						var doc = InputFile.FromStream(fileStream, fileName);
						await client.SendDocument(
							chatId: id,
							document: doc,
							caption: "📄 Here is your generated insurance policy.\nHave a nice day!"
						);
						Console.WriteLine("PDF sent via Telegram.");
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Error sending PDF via Telegram: {ex.Message}");
					}
					userStates[id] = BotState.Start;
					break;

				case "pricing_no" when userStates[id] == BotState.AgreeToPricing && !flag:
					var confrimPricingButtons = new InlineKeyboardMarkup(new[]
					{
						new[]
						{
							InlineKeyboardButton.WithCallbackData("✅ Yes", "pricing_yes"),
							InlineKeyboardButton.WithCallbackData("❌ No", "pricingConfirm_no")
						}
					});

					await client.EditMessageText(id,
						messageId: editPricingMessage.Id,
						verifyPricingMessage,
						parseMode: ParseMode.Html,
						replyMarkup: confrimPricingButtons);
					flag = true;
					break;

				case "registration_yes" when userStates[id] == BotState.sendRegistration || userStates[id] == BotState.ConfirmRegistration:
					if (registrationFields.Count > 1)
					{
						registrationFields.Dequeue();
						currentRegistrationField = registrationFields.Peek();

						string message = $"Is this data correct?\n" +
							$"<b>{currentRegistrationField}: {GetRegistrationField(currentRegistrationField, vehicleInfo)}</b>";

						await client.EditMessageText(id, editRegistrationMessage.Id,
							text: message,
							parseMode: ParseMode.Html,
							replyMarkup: verifyRegistrationKeyboard);
					}
					else
					{
						userStates[id] = BotState.sendLicense;
						await client.EditMessageText(id,
							editRegistrationMessage.Id,
							text: "<b>Great!</b>\nAll vehicle data was processed!\nNow, please send a photo of your <b>driving license</b>",
							parseMode: ParseMode.Html);
					}
					break;

				case "registration_no" when userStates[id] == BotState.sendRegistration || userStates[id] == BotState.ConfirmRegistration:
					await client.EditMessageText(id,
						editRegistrationMessage.Id,
						"❌ Please, enter the field manually.");
					userStates[id] = BotState.EnterRegistrationField;
					break;

				default:
					//await client.SendMessage(callbackQuery.Id, "⚠️ Unexpected response or invalid state.");
					break;
			}
		}

	}

	public enum BotState
	{
		Start,
		sendLicense,
		ConfirmLicense,
		sendRegistration,
		EnterRegistrationField,
		EnterLicenseField,
		ConfirmRegistration,
		AgreeToPricing,
		sendPolicy
	}

	enum registrationFieldToConfirm
	{
		Vin,
		Plate,
		Brand,
		Year
	}

	enum licenseFieldToConfirm
	{
		Name,
		DateOfBirth,
		LicenseNumber,
		LicenseClass,
		ExpiryDate,
		Adress
	}
}
