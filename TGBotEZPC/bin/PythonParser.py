import json
import math
import random
import sys
import time

from pprint import pprint

import requests

cur_directory = sys.argv[1]


# cur_directory = "D:\\RiderPojects\\ForTestsWithoutGIT\\ForTestsWithoutGIT\\bin"  # FOR DEBUG


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
    time.sleep(5)  # Это чтобы сервак нас не забанил

    number_of_items = int(test_response_for_ids["body"]["total"])
    number_of_items %= 25

    if number_of_items is None:
        return [[], {}, {}]

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
        time.sleep(5)

        page_ids = response_for_ids["body"]["products"]
        list_of_ids.extend(page_ids)

    # Получаем запрос с характеристиками товаров
    json_data_for_properties = {
        "productIds": list_of_ids,
        "mediaTypes": ["images"],
        "category": True,
        "status": True,
        "brand": True,
        "propertyTypes": ["KEY"],
        # Эта штука отвечает за количество характерист | ик
        # Если не все есть, то возможно поможет увелич V ение этого значения
        "propertiesConfig": {"propertiesPortionSize": 20},
        "multioffer": False,
    }

    response_for_properties = session.post("https://www.mvideo.ru/bff/product-details/list",
                                           cookies=COOKIES,
                                           headers=HEADERS,
                                           json=json_data_for_properties
                                           ).json()
    pprint(response_for_properties)
    response_for_properties = response_for_properties["body"]["products"]
    time.sleep(5)

    # Получаем данные о ценах на товары на страницах
    prices = {}

    params_for_prices = {
        "productIds": ",".join(list_of_ids),
        "addBonusRubles": "true",
        "isPromoApplied": "true",
    }

    response_for_prices = session.get("https://www.mvideo.ru/bff/products/prices",
                                      params=params_for_prices,
                                      cookies=COOKIES,
                                      headers=HEADERS
                                      ).json()
    time.sleep(5)

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

    for product in properties_response:
        product_id = product["productId"]
        product_name = product["name"]
        product_brand_name = product["brandName"]
        product_link = f"https://www.mvideo.ru/products/{product['nameTranslit']}-{product['productId']}"
        product_socket = "-"
        product_cores = "-"
        product_frequency = "-"
        product_turbo_frequency = "-"

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

        processorsData[product_id] = {"name": product_name,
                                      "brand_name": product_brand_name,
                                      "price": prices[product_id]["price"],
                                      "link": product_link,
                                      "socket": product_socket,
                                      "cores": product_cores,
                                      "frequency": product_frequency,
                                      "turbo_frequency": product_turbo_frequency}

    # Добавление полученных данных в общий словарь (не обновляет JSON файл)
    DATA_DICTIONARY[categotyID] = {"categoryName": categoryName,
                                   "data": processorsData}


# Функция получает детализированные данные для видеокарт (НЕ ДОДЕЛАНО)
def getInfoForVideoCards():
    global DATA_DICTIONARY
    categotyID = "5429"
    categoryName = "Видеокарты"
    videocardsData = {}

    ids, properties_response, prices = getData(categotyID)
    
    for product in properties_response:
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
                product_video_memory = property["value"]
            elif property["name"] == "Тип видеопамяти":
                product_video_memory_type = property["value"]
            elif property["name"] == "Частота графического процессора":
                product_processor_frequency = f"{property['value']} {property['measure']}"
            elif property["name"] == "Частота памяти":
                product_memory_frequency = f"{property['value']} {property['measure']}"
            elif property["name"] == "Выход HDMI":
                product_HDMI = f"{property['value']} {property['measure']}"
            elif property["name"] == "DisplayPort":
                product_DP = f"{property['value']} {property['measure']}"
            elif property["name"] == "Рекомендуемая мощность БП":
                product_recommended_power = f"{property['value']} {property['measure']}"

        videocardsData[product_id] = {"name": product_name,
                                      "brand_name": product_brand_name,
                                      "price": prices[product_id]["price"],
                                      "link": product_link,
                                      "product_video_memory": product_video_memory,
                                      "product_video_memory_type": product_video_memory_type,
                                      "product_processor_frequency": product_processor_frequency,
                                      "product_memory_frequency": product_memory_frequency, 
                                      "product_HDMI": product_HDMI, 
                                      "product_DP": product_DP, 
                                      "product_recommended_power": product_recommended_power}
    DATA_DICTIONARY[categotyID] = {"categoryName": categoryName,
                                   "data": videocardsData}


# Функция получает детализированные данные для жестких дисков (НЕ ДОДЕЛАНО)
def getInfoForMemory():
    global DATA_DICTIONARY
    categotyID = "5446"
    categoryName = "Жесткие диски"
    memoryData = {}

    ids, properties_response, prices = getData(categotyID)
    pprint(properties_response)
    
    # for product in properties_response:
    #     product_id = product["productId"]
    #     product_name = product["name"]
    #     product_brand_name = product["brandName"]
    #     product_link = f"https://www.mvideo.ru/products/{product['nameTranslit']}-{product['productId']}"
    #     product_video_memory = "-"
    #     product_video_memory_type = "-"
    #     product_processor_frequency = "-"
    #     product_memory_frequency = "-"
    #     product_HDMI = "-"
    #     product_DP = "-"
    #     product_recommended_power = "-"
    # 
    #     propertiesPortion = product["propertiesPortion"]
    #     for property in propertiesPortion:
    #         if property["name"] == "Объем видеопамяти":
    #             product_video_memory = property["value"]
    #         elif property["name"] == "Тип видеопамяти":
    #             product_video_memory_type = property["value"]
    #         elif property["name"] == "Частота графического процессора":
    #             product_processor_frequency = f"{property['value']} {property['measure']}"
    #         elif property["name"] == "Частота памяти":
    #             product_memory_frequency = f"{property['value']} {property['measure']}"
    #         elif property["name"] == "Выход HDMI":
    #             product_HDMI = f"{property['value']} {property['measure']}"
    #         elif property["name"] == "DisplayPort":
    #             product_DP = f"{property['value']} {property['measure']}"
    #         elif property["name"] == "Рекомендуемая мощность БП":
    #             product_recommended_power = f"{property['value']} {property['measure']}"
    # 
    #     videocardsData[product_id] = {"name": product_name,
    #                                   "brand_name": product_brand_name,
    #                                   "price": prices[product_id]["price"],
    #                                   "link": product_link,
    #                                   "product_video_memory": product_video_memory,
    #                                   "product_video_memory_type": product_video_memory_type,
    #                                   "product_processor_frequency": product_processor_frequency,
    #                                   "product_memory_frequency": product_memory_frequency,
    #                                   "product_HDMI": product_HDMI,
    #                                   "product_DP": product_DP,
    #                                   "product_recommended_power": product_recommended_power}
    # DATA_DICTIONARY[categotyID] = {"categoryName": categoryName,
    #                                "data": videocardsData}


# Функция получает детализированные данные для оперативной памяти (НЕ ДОДЕЛАНО)
def getInfoForRAM():
    pass


# Функция получает детализированные данные для материнских плат (НЕ ДОДЕЛАНО)
def getInfoForMotherBoards():
    pass


# Функция получает детализированные данные для блоков питания (НЕ ДОДЕЛАНО)
def getInfoForPower():
    pass


# Функция получает детализированные данные для корпусов (НЕ ДОДЕЛАНО)
def getInfoForBody():
    pass


# Функция обновляет JSON файл с данными
def updateDataFile():
    with open(getPath("data_for_bot.json"), "w", encoding="utf8") as data:
        json.dump(DATA_DICTIONARY, data, indent=4, ensure_ascii=False)


# Действия по обновлению информации
# getInfoForProcessors()
# getInfoForVideoCards()
getInfoForMemory()
#
#
#
#
# updateDataFile()
