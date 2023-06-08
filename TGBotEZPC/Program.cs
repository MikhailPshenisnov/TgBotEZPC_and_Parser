using System.Diagnostics;
using System.Runtime.InteropServices;
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

        // Данные о процентном соотношении комплектующих
        private static Dictionary<string, double> _percentage = new Dictionary<string, double>()
        {
            { "Процессор", 0.179 },
            { "Материнская плата", 0.1297 },
            { "Видеокарта", 0.3592 },
            { "Кулер", 0.0309 },
            { "Оперативная память", 0.0533 },
            { "Блок питания", 0.0717 },
            { "HDD", 0.0434 },
            { "SSD", 0.0618 },
            { "Корпус", 0.071 }
        };

        // Словарь для данных для каждого пользователя
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

            // Запуск отловщика событий
            _bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cancellationToken);

            Console.ReadLine();
        }

        // Отловщик событий Telegram бота, здесь используется как отловщик сообщений
        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            // Вывод данных о полученом сообщении, полезно для дебага, не влияет на работу программы
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));

            string hint;

            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;

                if (message.Text != null)
                {
                    // Получаем информацию о текущем пользователе
                    string curUser = message.Chat.Id.ToString();

                    // Если пользователь есть в словаре, то действуем исходя из шага, на котором сейчас пользователь
                    if (_usersData.ContainsKey(curUser))
                    {
                        string newMessageText;

                        if (message.Text == "/start")
                        {
                            _usersData[curUser] = GetEmptyUserDictionary();

                            newMessageText = $"Давайте начнём заново. Для начала введите " +
                                             $"желаемую стоимость вашей сборки (в рублях)";
                            await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                        }
                        else if (_usersData[curUser]["step"] == "0")
                        {
                            hint = $"Напишите стоимость сборки в рублях, числом, без дополнительных символов";

                            if (int.TryParse(message.Text, out var inputSumma))
                            {
                                _usersData[curUser]["summa"] = inputSumma.ToString();
                                _usersData[curUser]["step"] = "1";

                                newMessageText = $"Замечательно, теперь перейдем к выбору процессора. " +
                                                 $"Напишите 1, если вам необходим процессор Intel, или " +
                                                 $"напишите 2, если вам нужен процессор от AMD.";
                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                            }
                            else
                            {
                                newMessageText = $"Похоже что-то пошло не так, попробуйте снова.";
                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                await botClient.SendTextMessageAsync(message.Chat.Id, hint);
                            }
                        }
                        else if (_usersData[curUser]["step"] == "1")
                        {
                            hint = $"Напишите 1, если вам необходим процессор Intel, или " +
                                   $"напишите 2, если вам нужен процессор от AMD.";

                            var processorPrice = double.Parse(_usersData[curUser]["summa"]) * _percentage["Процессор"];
                            var processors = new List<string>();

                            if (message.Text == "1")
                            {
                                _usersData[curUser]["processor_brand"] = "Intel";
                            }
                            else if (message.Text == "2")
                            {
                                _usersData[curUser]["processor_brand"] = "AMD";
                            }
                            else
                            {
                                newMessageText = $"Похоже что-то пошло не так, попробуйте снова.";
                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                await botClient.SendTextMessageAsync(message.Chat.Id, hint);
                                return;
                            }

                            foreach (var processorId in _processorsIdsArray)
                            {
                                if (_jsonFile["Процессоры"]["data"][processorId.ToString()]["brand_name"]
                                        .ToString() == _usersData[curUser]["processor_brand"] &&
                                    Math.Abs(int.Parse(
                                        _jsonFile["Процессоры"]["data"][processorId.ToString()]["price"]
                                            .ToString()) - processorPrice) <= 2500)
                                {
                                    processors.Add(processorId.ToString());
                                }
                            }

                            if (processors.Count != 0)
                            {
                                _usersData[curUser]["step"] = "2";
                                _usersData[curUser]["possible_processors"] = string.Join(';', processors);


                                newMessageText = $"Мне удалось найти несколько подходящих моделей";
                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);

                                newMessageText = $"Выберите наиболее подходящий вариант и напишите его номер\n\n";
                                for (int i = 0; i < processors.Count; i++)
                                {
                                    newMessageText +=
                                        $"{i + 1} - {_jsonFile["Процессоры"]["data"][processors[i]]["name"].ToString()} " +
                                        $"- {_jsonFile["Процессоры"]["data"][processors[i]]["price"].ToString()} Руб.\n" +
                                        $"Ссылка: {_jsonFile["Процессоры"]["data"][processors[i]]["link"].ToString()}\n" +
                                        $"\n";
                                }

                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                            }
                            else
                            {
                                newMessageText = $"Кажется, в настоящий момент нельзя подобрать " +
                                                 $"сбалансированную сборку по данной цене. " +
                                                 $"Измените желаемую стоимость сборки и попытайтесь снова.";
                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                _usersData.Remove(curUser);

                                newMessageText = $"Для начала напишите /start";
                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                            }
                        }
                        else if (_usersData[curUser]["step"] == "2")
                        {
                            var processors = new List<string>(_usersData[curUser]["possible_processors"].Split(';'));

                            hint = $"Выберите наиболее подходящий вариант и напишите его номер\n\n";
                            for (int i = 0; i < processors.Count; i++)
                            {
                                hint +=
                                    $"{i + 1} - {_jsonFile["Процессоры"]["data"][processors[i]]["name"].ToString()} " +
                                    $"- {_jsonFile["Процессоры"]["data"][processors[i]]["price"].ToString()} Руб.\n" +
                                    $"Ссылка: {_jsonFile["Процессоры"]["data"][processors[i]]["link"].ToString()}\n" +
                                    $"\n";
                            }

                            if (int.TryParse(message.Text, out var processorInd) &&
                                (1 <= processorInd && processorInd <= processors.Count))
                            {
                                _usersData[curUser]["processor"] = processors[processorInd - 1];

                                newMessageText = $"Процессор выбран - переходим к материнской плате";
                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);

                                var motherboards = new List<string>();
                                var motherboardPrice = double.Parse(_usersData[curUser]["summa"]) *
                                                       _percentage["Материнская плата"];
                                foreach (var motherboardId in _motherboardIdsArray)
                                {
                                    if (_jsonFile["Материнские платы"]["data"][motherboardId.ToString()]["socket"]
                                            .ToString() ==
                                        _jsonFile["Процессоры"]["data"][_usersData[curUser]["processor"]]["socket"]
                                            .ToString() &&
                                        Math.Abs(int.Parse(
                                            _jsonFile["Материнские платы"]["data"][motherboardId.ToString()]["price"]
                                                .ToString()) - motherboardPrice) <= 2500)
                                    {
                                        motherboards.Add(motherboardId.ToString());
                                    }
                                }

                                if (motherboards.Count != 0)
                                {
                                    _usersData[curUser]["step"] = "3";
                                    _usersData[curUser]["possible_motherboards"] = string.Join(';', motherboards);

                                    newMessageText = $"Мне удалось найти несколько подходящих моделей";
                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);

                                    newMessageText = $"Выберите наиболее подходящий вариант и напишите его номер\n\n";
                                    for (int i = 0; i < motherboards.Count; i++)
                                    {
                                        newMessageText +=
                                            $"{i + 1} - {_jsonFile["Материнские платы"]["data"][motherboards[i]]["name"].ToString()} " +
                                            $"- {_jsonFile["Материнские платы"]["data"][motherboards[i]]["price"].ToString()} Руб.\n" +
                                            $"Ссылка: {_jsonFile["Материнские платы"]["data"][motherboards[i]]["link"].ToString()}\n" +
                                            $"\n";
                                    }

                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                }
                                else
                                {
                                    newMessageText = $"Кажется, в настоящий момент нельзя подобрать " +
                                                     $"сбалансированную сборку по данной цене. " +
                                                     $"Измените желаемую стоимость сборки и попытайтесь снова.";
                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                    _usersData.Remove(curUser);

                                    newMessageText = $"Для начала напишите /start";
                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                }
                            }
                            else
                            {
                                newMessageText = $"Похоже что-то пошло не так, попробуйте снова.";
                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                await botClient.SendTextMessageAsync(message.Chat.Id, hint);
                            }
                        }
                        else if (_usersData[curUser]["step"] == "3")
                        {
                            var motherboards =
                                new List<string>(_usersData[curUser]["possible_motherboards"].Split(';'));

                            hint = $"Выберите наиболее подходящий вариант и напишите его номер\n\n";
                            for (int i = 0; i < motherboards.Count; i++)
                            {
                                hint +=
                                    $"{i + 1} - {_jsonFile["Материнские платы"]["data"][motherboards[i]]["name"].ToString()} " +
                                    $"- {_jsonFile["Материнские платы"]["data"][motherboards[i]]["price"].ToString()} Руб.\n" +
                                    $"Ссылка: {_jsonFile["Материнские платы"]["data"][motherboards[i]]["link"].ToString()}\n" +
                                    $"\n";
                            }

                            if (int.TryParse(message.Text, out var motherboardInd) &&
                                (1 <= motherboardInd && motherboardInd <= motherboards.Count))
                            {
                                _usersData[curUser]["motherboard"] = motherboards[motherboardInd - 1];

                                newMessageText = $"С материнской платой закончили, дальше будем выбирать видеокарту";
                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);

                                var videocards = new List<string>();
                                var videocardPrice = double.Parse(_usersData[curUser]["summa"]) *
                                                     _percentage["Видеокарта"];
                                foreach (var videocardsId in _videocardsIdsArray)
                                {
                                    if (Math.Abs(int.Parse(
                                            _jsonFile["Видеокарты"]["data"][videocardsId.ToString()]["price"]
                                                .ToString()) - videocardPrice) <= 3000)
                                    {
                                        videocards.Add(videocardsId.ToString());
                                    }
                                }

                                if (videocards.Count != 0)
                                {
                                    _usersData[curUser]["step"] = "4";
                                    _usersData[curUser]["possible_videocards"] = string.Join(';', videocards);

                                    newMessageText = $"Мне удалось найти несколько подходящих моделей";
                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);

                                    newMessageText = $"Выберите наиболее подходящий вариант и напишите его номер\n\n";
                                    for (int i = 0; i < videocards.Count; i++)
                                    {
                                        newMessageText +=
                                            $"{i + 1} - {_jsonFile["Видеокарты"]["data"][videocards[i]]["name"].ToString()} " +
                                            $"- {_jsonFile["Видеокарты"]["data"][videocards[i]]["price"].ToString()} Руб.\n" +
                                            $"Ссылка: {_jsonFile["Видеокарты"]["data"][videocards[i]]["link"].ToString()}\n" +
                                            $"\n";
                                    }

                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                }
                                else
                                {
                                    newMessageText = $"Кажется, в настоящий момент нельзя подобрать " +
                                                     $"сбалансированную сборку по данной цене. " +
                                                     $"Измените желаемую стоимость сборки и попытайтесь снова.";
                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                    _usersData.Remove(curUser);

                                    newMessageText = $"Для начала напишите /start";
                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                }
                            }
                            else
                            {
                                newMessageText = $"Похоже что-то пошло не так, попробуйте снова.";
                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                await botClient.SendTextMessageAsync(message.Chat.Id, hint);
                            }
                        }
                        else if (_usersData[curUser]["step"] == "4")
                        {
                            var videocards = new List<string>(_usersData[curUser]["possible_videocards"].Split(';'));

                            hint = $"Выберите наиболее подходящий вариант и напишите его номер\n\n";
                            for (int i = 0; i < videocards.Count; i++)
                            {
                                hint +=
                                    $"{i + 1} - {_jsonFile["Видеокарты"]["data"][videocards[i]]["name"].ToString()} " +
                                    $"- {_jsonFile["Видеокарты"]["data"][videocards[i]]["price"].ToString()} Руб.\n" +
                                    $"Ссылка: {_jsonFile["Видеокарты"]["data"][videocards[i]]["link"].ToString()}\n" +
                                    $"\n";
                            }

                            if (int.TryParse(message.Text, out var videocardsInd) &&
                                (1 <= videocardsInd && videocardsInd <= videocards.Count))
                            {
                                _usersData[curUser]["videocard"] = videocards[videocardsInd - 1];

                                newMessageText = $"Переходим к выбору оперативной памяти. Выберите модель, и " +
                                                 $"я добавлю в вашу сборку 2 таких плашки, это оптимальное количество, " +
                                                 $"ведь у вас еще останутся свободные слоты, если вы захотите улучшить ваш ПК";
                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);

                                var ram = new List<string>();
                                var ramPrice = double.Parse(_usersData[curUser]["summa"]) *
                                    _percentage["Оперативная память"] / 2;
                                string ramType =
                                    _jsonFile["Материнские платы"]["data"][_usersData[curUser]["motherboard"]][
                                        "memory_type"].ToString();

                                var tmpArray = new JArray();
                                switch (ramType)
                                {
                                    case "DDR3":
                                        tmpArray = _ramDdr3IdsArray;
                                        break;
                                    case "DDR4":
                                        tmpArray = _ramDdr4IdsArray;
                                        break;
                                    case "DDR5":
                                        tmpArray = _ramDdr5IdsArray;
                                        break;
                                }

                                foreach (var ramId in tmpArray)
                                {   
                                    try
                                    {
                                        var test = _jsonFile["Оперативная память"]["data"][ramType][ramId.ToString()];
                                    }
                                    catch (Exception e)
                                    {
                                        continue;
                                    }
                                    if (_jsonFile["Оперативная память"]["data"][ramType][ramId.ToString()][
                                            "product_number_of_modules"].ToString() == "1" &&
                                        Math.Abs(int.Parse(
                                            _jsonFile["Оперативная память"]["data"][ramType][ramId.ToString()]["price"]
                                                .ToString()) - ramPrice) <= 1500)
                                    {
                                        ram.Add(ramId.ToString());
                                    }
                                }

                                if (ram.Count != 0)
                                {
                                    _usersData[curUser]["step"] = "5";
                                    _usersData[curUser]["possible_ram"] = string.Join(';', ram);

                                    newMessageText = $"Мне удалось найти несколько подходящих моделей";
                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);

                                    newMessageText = $"Выберите наиболее подходящий вариант и напишите его номер\n\n";
                                    for (int i = 0; i < ram.Count; i++)
                                    {
                                        newMessageText +=
                                            $"{i + 1} - {_jsonFile["Оперативная память"]["data"][ramType][ram[i]]["name"].ToString()} " +
                                            $"- {_jsonFile["Оперативная память"]["data"][ramType][ram[i]]["price"].ToString()} Руб. за 1 плашку " +
                                            $"(Итого вы заплатите {int.Parse(_jsonFile["Оперативная память"]["data"][ramType][ram[i]]["price"].ToString()) * 2} Руб.)\n" +
                                            $"Ссылка: {_jsonFile["Оперативная память"]["data"][ramType][ram[i]]["link"].ToString()}\n" +
                                            $"\n";
                                    }

                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                }
                                else
                                {
                                    newMessageText = $"Кажется, в настоящий момент нельзя подобрать " +
                                                     $"сбалансированную сборку по данной цене. " +
                                                     $"Измените желаемую стоимость сборки и попытайтесь снова.";
                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                    _usersData.Remove(curUser);

                                    newMessageText = $"Для начала напишите /start";
                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                }
                            }
                            else
                            {
                                newMessageText = $"Похоже что-то пошло не так, попробуйте снова.";
                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                await botClient.SendTextMessageAsync(message.Chat.Id, hint);
                            }
                        }
                        else if (_usersData[curUser]["step"] == "5")
                        {
                            var ram = new List<string>(_usersData[curUser]["possible_ram"].Split(';'));
                            string ramType =
                                _jsonFile["Материнские платы"]["data"][_usersData[curUser]["motherboard"]][
                                    "memory_type"].ToString();

                            hint = $"Выберите наиболее подходящий вариант и напишите его номер\n\n";
                            for (int i = 0; i < ram.Count; i++)
                            {
                                hint +=
                                    $"{i + 1} - {_jsonFile["Оперативная память"]["data"][ramType][ram[i]]["name"].ToString()} " +
                                    $"- {_jsonFile["Оперативная память"]["data"][ramType][ram[i]]["price"].ToString()} Руб. за 1 плашку " +
                                    $"(Итого вы заплатите {int.Parse(_jsonFile["Оперативная память"]["data"][ramType][ram[i]]["price"].ToString()) * 2} Руб.)\n" +
                                    $"Ссылка: {_jsonFile["Оперативная память"]["data"][ramType][ram[i]]["link"].ToString()}\n" +
                                    $"\n";
                            }

                            if (int.TryParse(message.Text, out var ramInd) &&
                                (1 <= ramInd && ramInd <= ram.Count))
                            {
                                _usersData[curUser]["ram"] = ram[ramInd - 1];

                                newMessageText =
                                    $"Кажется, с оперативной памятью всё. Теперь нужно выбрать блок питания";
                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);

                                var power = new List<string>();
                                var powerPrice = double.Parse(_usersData[curUser]["summa"]) *
                                                 _percentage["Блок питания"];
                                if (powerPrice > 10000) powerPrice = 7500;

                                foreach (var powerId in _powerIdsArray)
                                {
                                    if (_jsonFile["Блоки питания"]["data"][powerId.ToString()]["power"]
                                            .ToString() == "-")
                                    {
                                        continue;
                                    }

                                    double powerPower = double.Parse(
                                        (_jsonFile["Блоки питания"]["data"][powerId.ToString()]["power"]
                                            .ToString()).Split(' ')[0]);
                                    double videocardPower = double.Parse(
                                        (_jsonFile["Видеокарты"]["data"][_usersData[curUser]["videocard"]][
                                                "recommended_power"]
                                            .ToString()).Split(' ')[0]);

                                    if ((videocardPower <= powerPower) &&
                                        (powerPower - videocardPower) < 100 &&
                                        Math.Abs(int.Parse(
                                            _jsonFile["Блоки питания"]["data"][powerId.ToString()]["price"]
                                                .ToString()) - powerPrice) <= 2500)
                                    {
                                        power.Add(powerId.ToString());
                                    }
                                }

                                if (power.Count != 0)
                                {
                                    _usersData[curUser]["step"] = "6";
                                    _usersData[curUser]["possible_power"] = string.Join(';', power);

                                    newMessageText = $"Мне удалось найти несколько подходящих моделей";
                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);

                                    newMessageText = $"Выберите наиболее подходящий вариант и напишите его номер\n\n";
                                    for (int i = 0; i < power.Count; i++)
                                    {
                                        newMessageText +=
                                            $"{i + 1} - {_jsonFile["Блоки питания"]["data"][power[i]]["name"].ToString()} " +
                                            $"- {_jsonFile["Блоки питания"]["data"][power[i]]["price"].ToString()} Руб.\n" +
                                            $"Ссылка: {_jsonFile["Блоки питания"]["data"][power[i]]["link"].ToString()}\n" +
                                            $"\n";
                                    }

                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                }
                                else
                                {
                                    newMessageText = $"Кажется, в настоящий момент нельзя подобрать " +
                                                     $"сбалансированную сборку по данной цене. " +
                                                     $"Измените желаемую стоимость сборки и попытайтесь снова.";
                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                    _usersData.Remove(curUser);

                                    newMessageText = $"Для начала напишите /start";
                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                }
                            }
                            else
                            {
                                newMessageText = $"Похоже что-то пошло не так, попробуйте снова.";
                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                await botClient.SendTextMessageAsync(message.Chat.Id, hint);
                            }
                        }
                        else if (_usersData[curUser]["step"] == "6")
                        {
                            var power = new List<string>(_usersData[curUser]["possible_power"].Split(';'));

                            hint = $"Выберите наиболее подходящий вариант и напишите его номер\n\n";
                            for (int i = 0; i < power.Count; i++)
                            {
                                hint +=
                                    $"{i + 1} - {_jsonFile["Блоки питания"]["data"][power[i]]["name"].ToString()} " +
                                    $"- {_jsonFile["Блоки питания"]["data"][power[i]]["price"].ToString()} Руб.\n" +
                                    $"Ссылка: {_jsonFile["Блоки питания"]["data"][power[i]]["link"].ToString()}\n" +
                                    $"\n";
                            }

                            if (int.TryParse(message.Text, out var powerInd) &&
                                (1 <= powerInd && powerInd <= power.Count))
                            {
                                _usersData[curUser]["power"] = power[powerInd - 1];

                                newMessageText = $"Выбор блока питания завершен. Переходим к охлаждению процессора";
                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);

                                var coolers = new List<string>();
                                var coolerPrice = double.Parse(_usersData[curUser]["summa"]) *
                                                  _percentage["Кулер"];
                                if (coolerPrice > 5000) coolerPrice = 4500;

                                double coolerHeat = 0.0;
                                double processorHeat = 0.0;
                                
                                foreach (var coolerId in _coolersIdsArray)
                                {
                                    coolerHeat = double.Parse(
                                        (_jsonFile["Кулеры для процессоров"]["data"][coolerId.ToString()][
                                                "max_power_dissipation"]
                                            .ToString()).Split(' ')[0]);
                                    processorHeat = double.Parse(
                                        (_jsonFile["Процессоры"]["data"][_usersData[curUser]["processor"]]["heat"]
                                            .ToString()).Split(' ')[0]);
                                    if (processorHeat <= coolerHeat)
                                    {
                                        if (((coolerHeat - processorHeat) < 100) && ((Math.Abs(
                                                int.Parse(
                                                    _jsonFile["Кулеры для процессоров"]["data"][coolerId.ToString()][
                                                        "price"].ToString())) - coolerPrice) <= 1000))
                                        {
                                            coolers.Add(coolerId.ToString());
                                        }
                                    }
                                }

                                if (power.Count != 0)
                                {
                                    _usersData[curUser]["step"] = "7";
                                    _usersData[curUser]["possible_coolers"] = string.Join(';', coolers);

                                    newMessageText = $"Мне удалось найти несколько подходящих моделей";
                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);

                                    newMessageText = $"Выберите наиболее подходящий вариант и напишите его номер\n\n";
                                    for (int i = 0; i < coolers.Count; i++)
                                    {
                                        newMessageText +=
                                            $"{i + 1} - {_jsonFile["Кулеры для процессоров"]["data"][coolers[i]]["name"].ToString()} " +
                                            $"- {_jsonFile["Кулеры для процессоров"]["data"][coolers[i]]["price"].ToString()} Руб.\n" +
                                            $"Ссылка: {_jsonFile["Кулеры для процессоров"]["data"][coolers[i]]["link"].ToString()}\n" +
                                            $"\n";
                                    }

                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                }
                                else
                                {
                                    newMessageText = $"Кажется, в настоящий момент нельзя подобрать " +
                                                     $"сбалансированную сборку по данной цене. " +
                                                     $"Измените желаемую стоимость сборки и попытайтесь снова.";
                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                    _usersData.Remove(curUser);

                                    newMessageText = $"Для начала напишите /start";
                                    await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                }
                            }
                            else
                            {
                                newMessageText = $"Похоже что-то пошло не так, попробуйте снова.";
                                await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                                await botClient.SendTextMessageAsync(message.Chat.Id, hint);
                            }
                        }
                    }

                    // Иначе мы подсказываем ему как начать работу и создаем ему запись в словаре
                    else
                    {
                        if (message.Text == "/start")
                        {
                            var newMessageText = $"Хорошо, давайте начнём! Для начала введите " +
                                                 $"желаемую стоимость вашей сборки (в рублях)";
                            _usersData.Add(key: curUser, value: GetEmptyUserDictionary());
                            await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                        }
                        else
                        {
                            var newMessageText = $"Здравствуйте, я - бот по подбору комплектующих для ПК. " +
                                                 $"Я могу помочь вам с созданием собственной сборки.\n" +
                                                 $"Для начала напишите /start";
                            await botClient.SendTextMessageAsync(message.Chat.Id, newMessageText);
                        }
                    }
                }
            }
        }

        // Функция возвращает пустой (базовый) словарь для пользователя,
        // чтобы быстрее начинать новый сеанс с пользователем
        private static Dictionary<string, string> GetEmptyUserDictionary()
        {
            return new Dictionary<string, string>()
            {
                { "username", "test" },
                { "step", "0" },
                { "summa", "" },
                { "processor_brand", "" },
                { "possible_processors", "" },
                { "processor", "" },
                { "possible_motherboards", "" },
                { "motherboard", "" },
                { "possible_videocards", "" },
                { "videocard", "" },
                { "possible_ram", "" },
                { "ram", "" },
                { "possible_power", "" },
                { "power", "" },
                { "possible_coolers", "" },
                { "cooler", "" },
                { "possible_hdd", "" },
                { "hdd", "" },
                { "ssd_type", "" },
                { "possible_ssd", "" },
                { "ssd", "" }
            };
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

            // Получает объект с данными из JSON файла
            _jsonFile = JsonConvert.DeserializeObject(File.ReadAllText(jsonPath));

            // Создает списки ID товаров по категориям, чтобы можно было использовать их как итерируемый объект
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
        private static void Main(string[] args)
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