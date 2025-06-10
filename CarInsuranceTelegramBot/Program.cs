using CarInsuranceTelegramBot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Mindee;
using Mindee.Input;
using Mindee.Product.DriverLicense;

namespace MyTelegramBot
{
	class Program
	{

		static string telegramToken = "8068426185:AAHxN2uBeLvZyQuoZIR49g1KEyVHV5mQZmY";
		static string mindeeApiKey = "a9851a63943dab08b4a8bbbb9fc9c313";
		static long? myTelegramId = 875371626;

		static string helpMessage = "/start - Get started with the bot\n/help - Display help message.";
		static string photoReceivedMessage = "I received your info. Please wait while I analyze it.\n<b>Processing...</b>";
		static string greetingsMessage = "<b>Greetings!</b>\nI am here to help you buy insurance.\nPlease send a photo of driver license";
		static string pricingMessage = "Our current insurance price is <b>100 USD</b>.\nAre you comfortable with the price?";
		static string verifyPricingMessage = "Unfortunately, <b>100 USD</b> is our only pricing plan.";
		static string pricingAcceptedMessage = "<b>Great!</b>\nGenerating your insurance policy...";

		static Dictionary<long, BotState> userStates = new();
		static int verifyMessageId;
		static Message? verifyMessage;
		static Message? editPricingMessage;

		static bool flag = false;

		static async Task Main(string[] args)
		{
			Console.OutputEncoding = System.Text.Encoding.UTF8;

			Host bot = new(telegramToken);
			bot.Start();
			bot.OnMessage += OnMessage;
			bot.OnCallback += OnCallback;
			Console.ReadLine();
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
						userStates[id] = BotState.sendPhoto;
						flag = false;
						return;
					case "/help":
						await client.SendMessage(id, helpMessage);
						return;
				}

				if (update.Message?.Type == MessageType.Photo && userStates[id] == BotState.sendPhoto)
				{
					await client.SendMessage(id, photoReceivedMessage, parseMode: ParseMode.Html);
					await Task.Delay(600);

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

						MindeeClient mindeeClient = new(mindeeApiKey);
						var inputSource = new LocalInputSource(photoBytes, $"license_{photo.FileId}.jpg");
						//input here
						var response = await mindeeClient.EnqueueAndParseAsync<DriverLicenseV1>(inputSource);
						var driverLicense = response.Document.Inference.Prediction;

						string fullName = driverLicense.FirstName?.Value + " " + driverLicense.LastName?.Value ?? "Not found";
						string dateOfBirth = driverLicense.DateOfBirth?.Value ?? "Not found";
						string licenseNumber = driverLicense.Id?.Value ?? "Not found";
						string licenseClass = driverLicense.Category?.Value ?? "Not found";
						string expiryDate = driverLicense.ExpiryDate?.Value ?? "Not found";
						string countryCode = driverLicense.CountryCode?.Value ?? "Not found";

						await client.SendMessage(id,
							$"<b>Full Name:</b> {fullName}\n" +
							$"<b>Date of Birth:</b> {dateOfBirth}\n" +
							$"<b>License Number:</b> {licenseNumber}\n" +
							$"<b>License Class:</b> {licenseClass}\n" +
							$"<b>Expiry Date:</b> {expiryDate}\n" +
							$"<b>Address:</b> {countryCode}",
							parseMode: ParseMode.Html);

						var verifyButtons = new InlineKeyboardMarkup(new[]
						{
							new[]
							{
								InlineKeyboardButton.WithCallbackData("✅ Yes", "confirm_yes"),
								InlineKeyboardButton.WithCallbackData("❌ No", "confirm_no")
							}
						});

						verifyMessage = await client.SendMessage(id, "Is this data correct?", replyMarkup: verifyButtons);
						verifyMessageId = verifyMessage.MessageId;
						userStates[id] = BotState.ConfirmPhotoData;

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
				else if (update.Message?.Type == MessageType.Text && userStates[id] == BotState.Start)
				{
					await client.SendMessage(id, "Type /start to begin.");
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
				case "confirm_yes" when userStates[id] == BotState.ConfirmPhotoData:
					await client.EditMessageText(id, verifyMessageId, "✅ Thank you! We will now continue to pricing.");


					editPricingMessage = await client.SendMessage(id,
						pricingMessage,
						parseMode: ParseMode.Html,
						replyMarkup: pricingButtons);

					userStates[id] = BotState.AgreeToPricing;
					break;

				case "confirm_no" when userStates[id] == BotState.ConfirmPhotoData:
					await client.EditMessageText(id, verifyMessageId, "❌ Please, retake the photos and send them again.");
					userStates[id] = BotState.sendPhoto;
					break;

				case "pricing_yes" when userStates[id] == BotState.AgreeToPricing:
					await client.EditMessageText(id,
						messageId: editPricingMessage.Id,
						pricingAcceptedMessage,
						parseMode: ParseMode.Html
						);

					userStates[id] = BotState.sendPolicy;
					//policy here

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

				case "pricingConfirm_no" when userStates[id] == BotState.AgreeToPricing:
					await client.EditMessageText(id,
						editPricingMessage.Id,
						"Sorry, i can't help you then.");
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
		sendPhoto,
		ConfirmPhotoData,
		AgreeToPricing,
		sendPolicy
	}
}
