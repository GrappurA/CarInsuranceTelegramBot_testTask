using CarInsuranceTelegramBot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MyTelegramBot
{
	class Program
	{
		static string token = "8068426185:AAHxN2uBeLvZyQuoZIR49g1KEyVHV5mQZmY";
		static long? myTelegramId = 875371626;

		static string helpMessage = "/start - Get started with the bot\n/help - Display help message.";
		static string photoReceivedMessage = "I received your info. Please wait while I analyze it.";
		static string greetingsMessage = "<b>Greetings!</b>\nI am here to help you buy insurance.\nPlease send a photo of your identity and vehicle document.";
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

			Host bot = new(token);
			bot.Start();
			bot.OnMessage += OnMessage;
			bot.OnCallback += OnCallback;
			Console.ReadLine();
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
					await client.SendMessage(id, photoReceivedMessage);
					await Task.Delay(1000);

					var mindee = new MindeeClient();
					var data = await mindee.ExtractedDataAsync("passport.jpg", "vehicle.jpg");

					await client.SendMessage(id,
						$"<b>Name:</b> {data.passportInfo.FullName}\n" +
						$"<b>Date of Birth:</b> {data.passportInfo.DateOfBirth}\n" +
						$"<b>Passport Number:</b> {data.passportInfo.PassportNumber}\n\n" +
						$"<b>Vehicle Brand:</b> {data.vehicleInfo.Brand}\n" +
						$"<b>Vehicle Model:</b> {data.vehicleInfo.Model}\n" +
						$"<b>Vehicle Year:</b> {data.vehicleInfo.Year}\n" +
						$"<b>Vehicle VIN:</b> {data.vehicleInfo.Vin}",
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
