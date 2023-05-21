using System;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Exceptions;

namespace TelegramBotExperiments
{
    class Program
    {
        // bot part
        private static string TOKEN = "6215214413:AAE2GxGrUbgCqP_QyuZ7bNG-GGm_jeC1rTE";
        static ITelegramBotClient bot = new TelegramBotClient(TOKEN);

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                if (message.Text.ToLower() == "/start")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Привет, человек!");
                    return;
                }

                await botClient.SendTextMessageAsync(message.Chat, "Дада, я тут");
            }
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }
        
        // parser part
        
        
        //main function
        static void Main(string[] args)
        {
            Console.WriteLine($"Введите 1 для запуска бота, " +
                              $"введите 2 для запуска парсера и обновления перечня комплектующих " +
                              $"(не рекомендуется выполнять чаще нескольких раз в месяц)");
            var inpt = Console.ReadLine()?.ToString();
            if (inpt == "1")
            {
                Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);

                var cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = { }, // receive all update types
                };
                bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cancellationToken);
                Console.ReadLine();
            }
            else if (inpt == "2")
            {
                Console.Write("Я - парсер, и я обновляю данные");
            }
        }
    }
}