using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TGBotEZPC
{
    class Program
    {
        /*private const string TEXT_1 = "Один";
        private const string TEXT_2 = "Два";
        private const string TEXT_3 = "Три";
        private const string TEXT_4 = "Четыре";*/

        // bot part --------------------------------------------------------------------------------------------------
        private static string _token = "6215214413:AAE2GxGrUbgCqP_QyuZ7bNG-GGm_jeC1rTE";
        static ITelegramBotClient _bot = new TelegramBotClient(_token);

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
            {
                new KeyboardButton[] { "Help me" },
                new KeyboardButton[] { "Call me ☎️" },
            })
            {
                ResizeKeyboard = true
            };

            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                if (message.Text.ToLower() == "/start")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Привет, человек!",
                        replyMarkup: replyKeyboardMarkup);
                    return;
                }

                await botClient.SendTextMessageAsync(message.Chat, "Дада, я тут");
            }
        }

        /*
        private static IReplyMarkup GetButtons()
        {
            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                {

                    new List<KeyboardButton>
                        { new KeyboardButton{Text = TEXT_1 }, new KeyboardButton{Text = TEXT_2 }, },
                    new List<KeyboardButton> 
                        { new KeyboardButton{Text = TEXT_3 }, new KeyboardButton{Text = TEXT_4 }, }


                }
            };
        }
        */

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        // parser part ----------------------------------------------------------------------------------------------

        //Эта функция запускает парсер и выводит некоторую информацию для отладки
        static void StartParser()
        {
            Console.WriteLine("Запущен парсер, производится обновление файлов");

            // Получение пути до папки с файлами
            var exePath = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var binPath = exePath.Parent.Parent.FullName; // Возможно здесь есть ошибка в пути, но вроде работает

            /*
            Здесь может быть проблема с несоответствием пути к интерпретатору,
            нужно его поменять, мой закомментируй, свой допиши (строчка ниже)
            */

            // var pythonPath = @"C:\Users\User\AppData\Local\Programs\Python\Python38-32\python.exe"; // Мишаня ПК
            // var pythonPath = @"ТВОЙ ПУТЬ К python.exe"; // Санечка
            var pythonPath = @"C:\Users\misha\AppData\Local\Programs\Python\Python311\python.exe"; // Мишаня ноут

            var pythonScriptName = "\\PythonParser.py";

            Console.WriteLine("Запущен Python скрипт");
            DoPythonScript(pythonPath, binPath + pythonScriptName, binPath);
        }

        // Эта функция запускает Python скрипт и передает в него информацию о текущей директории
        static void DoPythonScript(string pythonPath, string scriptPath, string binPath)
        {
            var psi = new ProcessStartInfo();
            var errors = "";
            var results = "";

            psi.FileName = pythonPath;
            psi.Arguments = $"\"{scriptPath}\" \"{binPath}\"";
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            Console.WriteLine($"Выполняется Python скрипт \"{scriptPath}\"");
            Console.WriteLine("----------------------------------------------------------------------------------");

            using (var process = Process.Start(psi))
            {
                errors = process.StandardError.ReadToEnd();
                results = process.StandardOutput.ReadToEnd();
            }

            Console.WriteLine($"Вывод во время выполнения скрипта: {results}");
            Console.WriteLine("..................................................................................");
            Console.WriteLine($"Ошибки во время выполнения скрипта: {errors}");

            Console.WriteLine("----------------------------------------------------------------------------------");
            Console.WriteLine("Выполнение Python скрипта завершено");
        }

        //main function
        static void Main(string[] args)
        {
            // Интерфейс выбора режима программы
            Console.WriteLine($"Введите 1 для запуска бота, " +
                              $"Введите 2 для запуска парсера и обновления перечня комплектующих " +
                              $"НЕ ЗАБУДЬТЕ ОБНОВИТЬ ФАЙЛЫ COOKIE В СООТВЕТСТВУЮЩЕМ ФАЙЛЕ " +
                              $"(не рекомендуется выполнять чаще нескольких раз в месяц)");
            var inpt = Console.ReadLine();

            // Обработка выбора
            if (inpt == "1")
            {
                // Запускается бота
                /*Нужно засунуть это в отдельную функцию*/
                Console.WriteLine("Запущен бот " + _bot.GetMeAsync().Result.FirstName);

                var cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = { },
                };
                _bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cancellationToken);
                Console.ReadLine();
            }
            else if (inpt == "2")
            {
                // Запускается парсер
                StartParser();
            }
            else
            {
                // Обработка ввода, чтобы была
                Console.WriteLine("Что-то пошло не так, попробуйте снова :( ");
            }
        }
    }
}