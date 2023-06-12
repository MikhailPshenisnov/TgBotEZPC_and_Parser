import json
import math
import random
import sys
import time

import requests

cur_directory = sys.argv[1]

# Функция получает путь до файла из его названия
def getPath(file_name):
    global cur_directory
    return cur_directory + "\\" + file_name


# Получение данных cookie и headers из файла
with open(getPath("cookies_and_headers.json"), "r", encoding="utf8") as file:
    data = json.load(file)
COOKIES = data["cookies"]
HEADERS = data["headers"]

# Словарь для результата
DATA_DICTIONARY = {}


# Функция выполняет базовые общие запросы по категории товара
def getData(categoryID):
    global COOKIES, HEADERS

    session = requests.Session()

    # Делаем тестовый запрос для того, чтобы узнать количество страниц
    test_params_for_ids = {
        "categoryId": categoryID,
        "offset": "0",
        "limit": "24",
        "filterParams": "WyJ0b2xrby12LW5hbGljaGlpIiwiLTEyIiwiZGEiXQ==",
        "doTranslit": "true"
    }

    test_response_for_ids = session.get("https://www.mvideo.ru/bff/products/listing",
                                        params=test_params_for_ids,
                                        cookies=COOKIES,
                                        headers=HEADERS
                                        ).json()
    time.sleep(random.randint(3, 6))  # Задержка между запросами нужна для избежания блокировки сервером

    number_of_items = int(test_response_for_ids["body"]["total"])

    if number_of_items is None:
        return [[], [], {}]

    number_of_pages = math.ceil(number_of_items / 24)

    # Получаем полный список ID товаров через новые запросы по страницам
    list_of_ids = []

    for page in range(number_of_pages):
        offset = f"{page * 24}"

        params_for_ids = {
            "categoryId": categoryID,
            "offset": offset,

            "limit": "24",

            "filterParams": "WyJ0b2xrby12LW5hbGljaGlpIiwiLTEyIiwiZGEiXQ==",
            "doTranslit": "true",
        }

        response_for_ids = session.get("https://www.mvideo.ru/bff/products/listing",
                                       params=params_for_ids,
                                       cookies=COOKIES,
                                       headers=HEADERS
                                       ).json()
        time.sleep(random.randint(3, 6))

        page_ids = response_for_ids["body"]["products"]
        list_of_ids.extend(page_ids)

    # Получаем запрос с характеристиками товаров
    response_for_properties = []
    for i in range(0, len(list_of_ids), 50):
        tmp = list_of_ids[i:i + 50:] if i + 50 < len(list_of_ids) else list_of_ids[i::]
        json_data_for_properties = {
            "productIds": tmp,
            "mediaTypes": ["images"],
            "category": True,
            "status": True,
            "brand": True,
            "propertyTypes": ["KEY"],
            "propertiesConfig": {"propertiesPortionSize": 20},
            "multioffer": False,
        }

        tmp_response_for_properties = session.post("https://www.mvideo.ru/bff/product-details/list",
                                                   cookies=COOKIES,
                                                   headers=HEADERS,
                                                   json=json_data_for_properties
                                                   ).json()
        time.sleep(random.randint(3, 6))

        response_for_properties.append(tmp_response_for_properties["body"]["products"])

    # Получаем данные о ценах на товары на страницах
    prices = {}

    for i in range(0, len(list_of_ids), 50):
        tmp = list_of_ids[i:i + 50:] if i + 50 < len(list_of_ids) else list_of_ids[i::]
        params_for_prices = {
            "productIds": ",".join(tmp),
            "addBonusRubles": "true",
            "isPromoApplied": "true",
        }

        response_for_prices = session.get("https://www.mvideo.ru/bff/products/prices",
                                          params=params_for_prices,
                                          cookies=COOKIES,
                                          headers=HEADERS
                                          ).json()
        time.sleep(random.randint(3, 6))

        material_prices = response_for_prices["body"]["materialPrices"]

        for product in material_prices:
            product_id = product["price"]["productId"]
            product_base_price = product["price"]["basePrice"]
            prices[product_id] = {"price": product_base_price}

    return [list_of_ids, response_for_properties, prices]


# Функция получает детализированные данные для процессоров
def getInfoForProcessors():
    global DATA_DICTIONARY

    categotyID = "5431"
    categoryName = "Процессоры"
    processorsData = {}

    ids, properties_response, prices = getData(categotyID)

    for properties_response_part in properties_response:
        for product in properties_response_part:
            product_id = product["productId"]
            product_name = product["name"]
            product_brand_name = product["brandName"]
            product_link = f"https://www.mvideo.ru/products/{product['nameTranslit']}-{product['productId']}"
            product_socket = "-"
            product_cores = "-"
            product_frequency = "-"
            product_turbo_frequency = "-"
            product_heat = "-"

            propertiesPortion = product["propertiesPortion"]
            for property in propertiesPortion:
                if property["name"] == "Сокет":
                    product_socket = property["value"]
                elif property["name"] == "Количество ядер":
                    product_cores = property["value"]
                elif property["name"] == "Тактовая частота":
                    product_frequency = f"{property['value']} {property['measure']}"
                elif property["name"] == "Частота в режиме Turbo":
                    product_turbo_frequency = f"{property['value']} {property['measure']}"
                elif property["name"] == "Тепловыделение":
                    product_heat = f"{property['value']} {property['measure']}"

            if product_heat == "-":
                product_heat = "125 Вт"

            processorsData[product_id] = {"name": product_name,
                                          "brand_name": product_brand_name,
                                          "price": prices[product_id]["price"],
                                          "link": product_link,
                                          "socket": product_socket,
                                          "cores": product_cores,
                                          "frequency": product_frequency,
                                          "turbo_frequency": product_turbo_frequency,
                                          "heat": product_heat}

    # Добавление полученных данных в общий словарь (не обновляет JSON файл)
    DATA_DICTIONARY[categoryName] = {"categoryId": categotyID,
                                     "productIDs": ids,
                                     "data": processorsData}


# Функция получает детализированные данные для видеокарт
def getInfoForVideoCards():
    global DATA_DICTIONARY
    categotyID = "5429"
    categoryName = "Видеокарты"
    videocardsData = {}

    ids, properties_response, prices = getData(categotyID)

    for properties_response_part in properties_response:
        for product in properties_response_part:
            product_id = product["productId"]
            product_name = product["name"]
            product_brand_name = product["brandName"]
            product_link = f"https://www.mvideo.ru/products/{product['nameTranslit']}-{product['productId']}"
            product_video_memory = "-"
            product_video_memory_type = "-"
            product_processor_frequency = "-"
            product_memory_frequency = "-"
            product_HDMI = "-"
            product_DP = "-"
            product_recommended_power = "-"

            propertiesPortion = product["propertiesPortion"]
            for property in propertiesPortion:
                if property["name"] == "Объем видеопамяти":
                    product_video_memory = f"{property['value']} {property['measure']}"
                elif property["name"] == "Тип видеопамяти":
                    product_video_memory_type = property["value"]
                elif property["name"] == "Частота графического процессора":
                    product_processor_frequency = f"{property['value']} {property['measure']}"
                elif property["name"] == "Частота памяти":
                    product_memory_frequency = f"{property['value']} {property['measure']}"
                elif property["name"] == "Выход HDMI":
                    product_HDMI = property["value"]
                elif property["name"] == "DisplayPort":
                    product_DP = property["value"]
                elif property["name"] == "Рекомендуемая мощность БП":
                    product_recommended_power = f"{property['value']} {property['measure']}"

            if product_recommended_power == "-":
                product_recommended_power = "600 Вт"

            videocardsData[product_id] = {"name": product_name,
                                          "brand_name": product_brand_name,
                                          "price": prices[product_id]["price"],
                                          "link": product_link,
                                          "video_memory": product_video_memory,
                                          "video_memory_type": product_video_memory_type,
                                          "processor_frequency": product_processor_frequency,
                                          "memory_frequency": product_memory_frequency,
                                          "HDMI": product_HDMI,
                                          "DP": product_DP,
                                          "recommended_power": product_recommended_power}
    DATA_DICTIONARY[categoryName] = {"categoryId": categotyID,
                                     "productIDs": ids,
                                     "data": videocardsData}


# Функция получает детализированные данные для жестких дисков
def getInfoForMemory():
    global DATA_DICTIONARY
    categotyID = "5436"
    categoryName = "Жесткие диски"
    memoryData = {"HDD": {}, "SSD_m2": {}, "SSD_usual": {}}
    HDD_ids, SSD_m2_ids, SSD_usual_ids = [], [], []

    ids, properties_response, prices = getData(categotyID)

    for properties_response_part in properties_response:
        for product in properties_response_part:
            product_id = product["productId"]
            product_name = product["name"]
            product_brand_name = product["brandName"]
            product_link = f"https://www.mvideo.ru/products/{product['nameTranslit']}-{product['productId']}"

            product_form_factor = "-"

            product_hdd_memory = "-"
            product_spin_speed = "-"
            product_buffer_memory = "-"

            product_ssd_memory = "-"  # Для SSD данные о памяти будут получаться по-другому
            product_connection_interface = "-"
            product_flash_memory_type = "-"
            product_max_read_speed = "-"
            product_max_write_speed = "-"

            propertiesPortion = product["propertiesPortion"]
            if product["category"]["name"] == "HDD":
                for property in propertiesPortion:
                    if property["name"] == "Объем HDD":
                        product_hdd_memory = f"{property['value']} {property['measure']}"
                    elif property["name"] == "Скорость вращения шпинделя HDD":
                        product_spin_speed = f"{property['value']} {property['measure']}"
                    elif "Объем буферной памяти" in property["name"]:
                        product_buffer_memory = f"{property['value']} {property['measure']}"
                    elif property["name"] == "Форм-фактор":
                        product_form_factor = property["value"]
                memoryData["HDD"][product_id] = {"name": product_name,
                                                 "brand_name": product_brand_name,
                                                 "price": prices[product_id]["price"],
                                                 "link": product_link,
                                                 "hdd_memory": product_hdd_memory,
                                                 "form_factor": product_form_factor,
                                                 "spin_speed": product_spin_speed,
                                                 "buffer_memory": product_buffer_memory}
                HDD_ids.append(product_id)
            elif product["category"]["name"] == "SSD":
                for temp_string in product_name.split():
                    if "tb" in temp_string.lower() or "gb" in temp_string.lower():
                        if "(" in temp_string:
                            product_ssd_memory = product_name.split()[5]
                        else:
                            product_ssd_memory = temp_string
                for property in propertiesPortion:
                    if property["name"] == "Интерфейс подключения":
                        product_connection_interface = property['value']
                    elif property["name"] == "Тип флеш-памяти":
                        product_flash_memory_type = property['value']
                    elif property["name"] == "Максимальная скорость чтения до":
                        product_max_read_speed = f"{property['value']} {property['measure']}"
                    elif property["name"] == "Максимальная скорость записи до":
                        product_max_write_speed = f"{property['value']} {property['measure']}"
                    elif property["name"] == "Форм-фактор":
                        product_form_factor = property["value"]
                if "M.2" in product_form_factor:
                    tag = "SSD_m2"
                    SSD_m2_ids.append(product_id)
                else:
                    tag = "SSD_usual"
                    SSD_usual_ids.append(product_id)
                memoryData[tag][product_id] = {"name": product_name,
                                               "brand_name": product_brand_name,
                                               "price": prices[product_id]["price"],
                                               "link": product_link,
                                               "ssd_memory": product_ssd_memory,
                                               "form_factor": product_form_factor,
                                               "connection_interface": product_connection_interface,
                                               "flash_memory_type": product_flash_memory_type,
                                               "max_read_speed": product_max_read_speed,
                                               "max_write_speed": product_max_write_speed}
    DATA_DICTIONARY[categoryName] = {"categoryId": categotyID,
                                     "HDD_ids": HDD_ids,
                                     "SSD_m2_ids": SSD_m2_ids,
                                     "SSD_usual_ids": SSD_usual_ids,
                                     "data": memoryData}


# Функция получает детализированные данные для оперативной памяти
def getInfoForRAM():
    global DATA_DICTIONARY
    categotyID = "5433"
    categoryName = "Оперативная память"
    RAMData = {"DDR3": {}, "DDR4": {}, "DDR5": {}}
    DDR3_ids, DDR4_ids, DDR5_ids = [], [], []

    ids, properties_response, prices = getData(categotyID)
    extra_ids = []

    for properties_response_part in properties_response:
        for product in properties_response_part:
            product_id = product["productId"]
            product_name = product["name"]
            product_brand_name = product["brandName"]
            product_link = f"https://www.mvideo.ru/products/{product['nameTranslit']}-{product['productId']}"

            product_type = "-"
            for temp_string in product_name.split():
                if "ddr" in temp_string.lower():
                    if "ddr3" in temp_string.lower():
                        product_type = "DDR3"
                        DDR3_ids.append(product_id)
                    elif "ddr4" in temp_string.lower():
                        product_type = "DDR4"
                        DDR4_ids.append(product_id)
                    elif "ddr5" in temp_string.lower():
                        product_type = "DDR5"
                        DDR5_ids.append(product_id)
            if product_type == "-":  # Есть сломанные товары, где не указан тип памяти, их мы игнорируем
                extra_ids.append(product_id)
                continue

            product_module_memory = "-"
            product_number_of_modules = "-"
            product_memory_frequency = "-"
            product_throughput = "-"
            product_form_factor = "-"

            propertiesPortion = product["propertiesPortion"]
            for property in propertiesPortion:
                if property["name"] == "Объем одного модуля":
                    product_module_memory = f"{property['value']} {property['measure']}"
                elif property["name"] == "Количество модулей в комплекте":
                    product_number_of_modules = property['value']
                elif property["name"] == "Частота памяти":
                    product_memory_frequency = f"{property['value']} {property['measure']}"
                elif property["name"] == "Пропускная способность":
                    product_throughput = f"{property['value']} {property['measure']}"
                elif property["name"] == "Форм-фактор":
                    product_form_factor = property['value']

                if product_module_memory == "-":
                    for temp_string in product_name.split():
                        if "gb" in temp_string.lower():
                            product_module_memory = temp_string.upper()
            RAMData[product_type][product_id] = {"name": product_name,
                                                 "brand_name": product_brand_name,
                                                 "price": prices[product_id]["price"],
                                                 "link": product_link,
                                                 "product_module_memory": product_module_memory,
                                                 "product_number_of_modules": product_number_of_modules,
                                                 "product_memory_frequency": product_memory_frequency,
                                                 "product_form_factor": product_form_factor,
                                                 "product_throughput": product_throughput}
    for i in extra_ids:
        ids.remove(i)
    DATA_DICTIONARY[categoryName] = {"categoryId": categotyID,
                                     "DDR3_ids": DDR3_ids,
                                     "DDR4_ids": DDR4_ids,
                                     "DDR5_ids": DDR5_ids,
                                     "data": RAMData}


# Функция получает детализированные данные для материнских плат
def getInfoForMotherBoards():
    global DATA_DICTIONARY
    categotyID = "5432"
    categoryName = "Материнские платы"
    MotherBoardsData = {}

    ids, properties_response, prices = getData(categotyID)

    for properties_response_part in properties_response:
        for product in properties_response_part:
            product_id = product["productId"]
            product_name = product["name"]
            product_brand_name = product["brandName"]
            product_link = f"https://www.mvideo.ru/products/{product['nameTranslit']}-{product['productId']}"

            product_form_factor = "-"
            product_raid = "-"
            product_socket = "-"
            product_memory_type = "-"
            product_ram_number = "-"
            product_pci_e_x1 = "-"
            product_pci_e_x16 = "-"
            product_pci_e_m2 = "-"
            product_wifi = "-"
            product_bluetooth = "-"
            product_hdmi = "-"
            product_dp = "-"

            propertiesPortion = product["propertiesPortion"]
            for property in propertiesPortion:
                if property["name"] == "Форм-фактор":
                    product_form_factor = property['value']
                elif property["name"] == "Поддержка RAID":
                    product_raid = property['value']
                elif property["name"] == "Сокет":
                    product_socket = property['value']
                elif property["name"] == "Тип памяти":
                    product_memory_type = property['value']
                elif property["name"] == "Количество слотов памяти":
                    product_ram_number = property['value']
                elif property["name"] == "PCI-Express 3.0 x16" or property["name"] == "PCI-Express x16":
                    product_pci_e_x1 = property['value']
                elif property["name"] == "PCI-Express x1":
                    product_pci_e_x16 = property['value']
                elif property["name"] == "PCI-E M.2":
                    product_pci_e_m2 = property['value']
                elif property["name"] == "Поддержка Wi-Fi":
                    product_wifi = property['value']
                elif property["name"] == "Версия Bluetooth":
                    product_bluetooth = property['value']
                elif property["name"] == "Выход HDMI":
                    product_hdmi = property['value']
                elif property["name"] == "DisplayPort":
                    product_dp = property['value']

                if product_form_factor == "-":
                    product_form_factor = "ATX"

            MotherBoardsData[product_id] = {"name": product_name,
                                            "brand_name": product_brand_name,
                                            "price": prices[product_id]["price"],
                                            "link": product_link,
                                            "form_factor": product_form_factor,
                                            "raid": product_raid,
                                            "socket": product_socket,
                                            "memory_type": product_memory_type,
                                            "ram_number": product_ram_number,
                                            "pci_e_x1": product_pci_e_x1,
                                            "pci_e_x16": product_pci_e_x16,
                                            "pci_e_m2": product_pci_e_m2,
                                            "wifi": product_wifi,
                                            "bluetooth": product_bluetooth,
                                            "hdmi": product_hdmi,
                                            "dp": product_dp}
    DATA_DICTIONARY[categoryName] = {"categoryId": categotyID,
                                     "productIDs": ids,
                                     "data": MotherBoardsData}


# Функция получает детализированные данные для блоков питания
def getInfoForPower():
    global DATA_DICTIONARY
    categotyID = "5435"
    categoryName = "Блоки питания"
    PowerData = {}

    ids, properties_response, prices = getData(categotyID)

    for properties_response_part in properties_response:
        for product in properties_response_part:
            product_id = product["productId"]
            product_name = product["name"]
            product_brand_name = product["brandName"]
            product_link = f"https://www.mvideo.ru/products/{product['nameTranslit']}-{product['productId']}"

            product_power = "-"
            product_standard = "-"
            product_overload_protection = "-"
            product_surge_protection = "-"

            propertiesPortion = product["propertiesPortion"]
            for property in propertiesPortion:
                if property["name"] == "Мощность":
                    product_power = f"{property['value']} {property['measure']}"
                elif property["name"] == "Стандарт":
                    product_standard = property['value']
                elif property["name"] == "Защита от перегрузки":
                    product_overload_protection = property['value']
                elif property["name"] == "Защита от перенапряжения":
                    product_surge_protection = property['value']

            PowerData[product_id] = {"name": product_name,
                                     "brand_name": product_brand_name,
                                     "price": prices[product_id]["price"],
                                     "link": product_link,
                                     "power": product_power,
                                     "standard": product_standard,
                                     "overload_protection": product_overload_protection,
                                     "surge_protection": product_surge_protection}
    DATA_DICTIONARY[categoryName] = {"categoryId": categotyID,
                                     "productIDs": ids,
                                     "data": PowerData}


# Функция получает детализированные данные для корпусов
def getInfoForBody():
    global DATA_DICTIONARY
    categotyID = "5434"
    categoryName = "Корпуса"
    BodyData = {}

    sizes = {"Midi-Tower": ["Mini-ITX", "Micro-ATX", "ATX"],
             "Mini-Tower": ["Mini-ITX", "Micro-ATX"],
             "Slim Desktop": ["Mini-ITX", "Micro-ATX"],
             "DeskTop": ["Mini-ITX", "Micro-ATX", "ATX"]}

    ids, properties_response, prices = getData(categotyID)
    extra_ids = []

    for properties_response_part in properties_response:
        for product in properties_response_part:
            product_id = product["productId"]
            product_name = product["name"]
            product_brand_name = product["brandName"]
            product_link = f"https://www.mvideo.ru/products/{product['nameTranslit']}-{product['productId']}"

            product_size = "-"
            product_3_5 = "-"
            product_usb_3_0 = "-"
            product_weight = "-"
            product_material = "-"
            product_color = "-"

            propertiesPortion = product["propertiesPortion"]
            for property in propertiesPortion:
                if property["name"] == "Типоразмер":
                    product_size = property['value']
                elif "Внутренние отсеки 3.5" in property["name"]:
                    product_3_5 = property['value']
                elif property["name"] == "Порт USB 3.0":
                    product_usb_3_0 = property['value']
                elif property["name"] == "Вес":
                    product_weight = f"{property['value']} {property['measure']}"
                elif property["name"] == "Материал корпуса":
                    product_material = property['value']
                elif property["name"] == "Цвет":
                    product_color = property['value']

            if product_size == "-":
                extra_ids.append(product_id)
                continue
            else:
                product_size_properties = {"size": product_size,
                                           "suitable_motherboards": sizes[product_size]}

            BodyData[product_id] = {"name": product_name,
                                    "brand_name": product_brand_name,
                                    "price": prices[product_id]["price"],
                                    "link": product_link,
                                    "size_properties": product_size_properties,
                                    "3_5": product_3_5,
                                    "usb_3_0": product_usb_3_0,
                                    "weight": product_weight,
                                    "material": product_material,
                                    "color": product_color}
    for i in extra_ids:
        ids.remove(i)
    DATA_DICTIONARY[categoryName] = {"categoryId": categotyID,
                                     "productIDs": ids,
                                     "data": BodyData}


# Функция получает детализированные данные для процессорных кулеров
def getInfoForProcessorCoolers():
    global DATA_DICTIONARY
    categotyID = "7927"
    categoryName = "Кулеры для процессоров"
    ProcessorCoolersData = {}

    ids, properties_response, prices = getData(categotyID)
    extra_ids = []

    for properties_response_part in properties_response:
        for product in properties_response_part:
            product_id = product["productId"]
            product_name = product["name"]
            product_brand_name = product["brandName"]
            product_link = f"https://www.mvideo.ru/products/{product['nameTranslit']}-{product['productId']}"

            product_max_power_dissipation = "-"
            product_height = "-"
            product_fans = "-"
            product_noise = "-"

            propertiesPortion = product["propertiesPortion"]
            for property in propertiesPortion:
                if property["name"] == "Максимальная рассеиваемая мощность":
                    product_max_power_dissipation = f"{property['value']} {property['measure']}"
                elif property["name"] == "Высота кулера":
                    product_height = f"{property['value']} {property['measure']}"
                elif property["name"] == "Количество вентиляторов":
                    product_fans = property['value']
                elif property["name"] == "Уровень шума":
                    product_noise = property['value']

            if product_max_power_dissipation == "-" or "." in product_max_power_dissipation:
                extra_ids.append(product_id)
                continue

            ProcessorCoolersData[product_id] = {"name": product_name,
                                                "brand_name": product_brand_name,
                                                "price": prices[product_id]["price"],
                                                "link": product_link,
                                                "max_power_dissipation": product_max_power_dissipation,
                                                "height": product_height,
                                                "fans": product_fans,
                                                "noise": product_noise}
    for i in extra_ids:
        ids.remove(i)
    DATA_DICTIONARY[categoryName] = {"categoryId": categotyID,
                                     "productIDs": ids,
                                     "data": ProcessorCoolersData}


# Функция обновляет JSON файл с данными
def updateDataFile():
    with open(getPath("data_for_bot.json"), "w", encoding="utf8") as data:
        json.dump(DATA_DICTIONARY, data, indent=4, ensure_ascii=False)


# Действия по обновлению информации
getInfoForProcessors()
getInfoForVideoCards()
getInfoForMemory()
getInfoForRAM()
getInfoForMotherBoards()
getInfoForPower()
getInfoForBody()
getInfoForProcessorCoolers()
updateDataFile()
