using System.Diagnostics;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace TGBotEZPC
{
    class Program
    {
        // Переменные для бота
        private const string _token = "6215214413:AAE2GxGrUbgCqP_QyuZ7bNG-GGm_jeC1rTE";
        private static ITelegramBotClient _bot = new TelegramBotClient(_token);

        // Донные о товарах
        private static dynamic _jsonFile;

        // Списки id товаров по категориям
        private static JArray _processorsIdsArray;
        private static JArray _videocardsIdsArray;
        private static JArray _memoryHddIdsArray;
        private static JArray _memorySsdM2IdsArray;
        private static JArray _memorySsdUsualIdsArray;
        private static JArray _ramDdr3IdsArray;
        private static JArray _ramDdr4IdsArray;
        private static JArray _ramDdr5IdsArray;
        private static JArray _motherboardIdsArray;
        private static JArray _powerIdsArray;
        private static JArray _bodyIdsArray;
        private static JArray _coolersIdsArray;

        // Данные для каждого пользователя
        private static Dictionary<string, Dictionary<string, string>> _usersData =
            new Dictionary<string, Dictionary<string, string>>();

        // bot part ----------------------------------------------------------------------------------------------------

        //Эта функция запускает бота и выводит некоторую информацию для отладки
        private static void StartBot()
        {
            // Подгрузка файлов из JSON файла
            GetDataFromJson();

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

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                if (message.Text != null)
                {
                    if (message.Text.ToLower() == "/start")
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Привет, человек!");
                        return;
                    }

                    await botClient.SendTextMessageAsync(message.Chat.Id, "Дада, я тут");
                }
            }
        }

        // Обработчик ошибок бота
        private async static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        // parser part -------------------------------------------------------------------------------------------------

        //Эта функция запускает парсер и выводит некоторую информацию для отладки
        private static void StartParser()
        {
            Console.WriteLine("Запущен парсер, производится обновление файлов");

            // Получение пути до папки с файлами
            var exePath = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var binPath = exePath.Parent.Parent.FullName;

            // Здесь может быть проблема с несоответствием пути к интерпретатору,
            // нужно его поменять, мой закомментируй, свой допиши (строчка ниже)

            var pythonPath = @"C:\Users\User\AppData\Local\Programs\Python\Python38-32\python.exe"; // Мишаня ПК
            // var pythonPath = @"ТВОЙ ПУТЬ К python.exe"; // Санечка
            // var pythonPath = @"C:\Users\misha\AppData\Local\Programs\Python\Python311\python.exe"; // Мишаня ноут

            var pythonScriptName = "\\PythonParser.py";

            Console.WriteLine("Запущен Python скрипт");
            DoPythonScript(pythonPath, binPath + pythonScriptName, binPath);
        }

        // Эта функция запускает Python скрипт и передает в него информацию о текущей директории
        private static void DoPythonScript(string pythonPath, string scriptPath, string binPath)
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

            using (var process = Process.Start(psi))
            {
                errors = process.StandardError.ReadToEnd();
                results = process.StandardOutput.ReadToEnd();
            }

            Console.WriteLine("----------------------------------------------------------------------------------");
            Console.WriteLine($"Вывод во время выполнения скрипта: {results}");
            Console.WriteLine("..................................................................................");
            Console.WriteLine($"Ошибки во время выполнения скрипта: {errors}");
            Console.WriteLine("----------------------------------------------------------------------------------");

            Console.WriteLine("Выполнение Python скрипта завершено");
        }

        // Эта функция принимает данные из JSON файла
        private static void GetDataFromJson()
        {
            var exePath = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var binPath = exePath.Parent.Parent.FullName;
            var jsonPath = $"{binPath}\\data_for_bot.json";

            _jsonFile = JsonConvert.DeserializeObject(File.ReadAllText(jsonPath));

            _processorsIdsArray = JArray.Parse((_jsonFile["Процессоры"]["productIDs"]).ToString());
            _videocardsIdsArray = JArray.Parse((_jsonFile["Видеокарты"]["productIDs"]).ToString());
            _memoryHddIdsArray = JArray.Parse((_jsonFile["Жесткие диски"]["HDD_ids"]).ToString());
            _memorySsdM2IdsArray = JArray.Parse((_jsonFile["Жесткие диски"]["SSD_m2_ids"]).ToString());
            _memorySsdUsualIdsArray = JArray.Parse((_jsonFile["Жесткие диски"]["SSD_usual_ids"]).ToString());
            _ramDdr3IdsArray = JArray.Parse((_jsonFile["Оперативная память"]["DDR3_ids"]).ToString());
            _ramDdr4IdsArray = JArray.Parse((_jsonFile["Оперативная память"]["DDR4_ids"]).ToString());
            _ramDdr5IdsArray = JArray.Parse((_jsonFile["Оперативная память"]["DDR5_ids"]).ToString());
            _motherboardIdsArray = JArray.Parse((_jsonFile["Материнские платы"]["productIDs"]).ToString());
            _powerIdsArray = JArray.Parse((_jsonFile["Блоки питания"]["productIDs"]).ToString());
            _bodyIdsArray = JArray.Parse((_jsonFile["Корпуса"]["productIDs"]).ToString());
            _coolersIdsArray = JArray.Parse((_jsonFile["Кулеры для процессоров"]["productIDs"]).ToString());
        }

        // main function -----------------------------------------------------------------------------------------------

        //Функция запуска программы
        static void Main(string[] args)
        {
            // Интерфейс выбора режима программы
            Console.WriteLine($"Введите цифру:\n" +
                              $"1 - для запуска бота Telegram\n" +
                              $"2 - для запуска парсера и обновления перечня комплектующих\n" +
                              $"    Процесс займет от 3 до 8 мин.\n" +
                              $"    НЕ ЗАБУДЬТЕ ОБНОВИТЬ ФАЙЛЫ COOKIE И HEADERS В СООТВЕТСТВУЮЩЕМ ФАЙЛЕ\n" +
                              $"    (Не рекомендуется использовать чаще нескольких раз в месяц)");

            var inpt = Console.ReadLine();

            // Обработка выбора
            if (inpt == "1")
            {
                // Запускается бот
                StartBot();
            }
            else if (inpt == "2")
            {
                // Запускается парсер
                StartParser();
            }
            else
            {
                // Обработка ошибочного ввода, чтобы была
                Console.WriteLine("Что-то пошло не так, попробуйте снова :( ");
                GetDataFromJson();
            }
        }
    }
}