using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace CarInsuranceTelegramBot
{
	public class Host
	{
		public Action<ITelegramBotClient, Update>? OnMessage;
		public Action<ITelegramBotClient, Update>? OnUpdate;
		public Action<ITelegramBotClient, CallbackQuery>? OnCallback;


		private TelegramBotClient bot;

		public Host(string token)
		{
			bot = new(token);
		}

		public void Start()
		{
			bot.StartReceiving(UpdateHandler, ErrorHandler);
			Console.WriteLine("Bot started.");
		}

		private async Task ErrorHandler(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
		{
			Console.WriteLine("Error occured: " + exception.Message);
			await Task.CompletedTask;
		}

		private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
		{   
			Console.WriteLine(update.Message?.Text ?? "Not a text message");

			if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery && update.CallbackQuery != null)
			{
				OnCallback?.Invoke(client, update.CallbackQuery);
			}
			else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message && update.Message != null)
			{
				OnMessage?.Invoke(client, update);
			}

			await Task.CompletedTask;
		}


	}
}
