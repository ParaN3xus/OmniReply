import json
import os
import random
import string
import threading
import time
import websocket
import base64
from time import sleep
from enum import Enum
from PIL import Image
from io import BytesIO

import lib.itchat as itchat
from lib.itchat.content import *

ws_uri = "ws://localhost:8080"
ws = websocket.WebSocket()

class message_type(Enum):
    ID_GIVING = 0
    CHAT_MESSAGE = 1

class message_part_type(Enum):
    TEXT = 0
    IMAGE = 1
    Record=  2

connecting=False

def connect_to_websocket():
    global connecting
    if(connecting):
        return
    connecting = True
    while True:
        try:
            ws.connect(ws_uri)
            print(f"Connected to {ws_uri}")
            give_id("wechat")
            break
        except Exception as e:
            print(f"Failed to connect to WebSocket: {e}. Retrying...")
            sleep(3)
    connecting = False
    

def send_to_websocket(message):
    try:
        ws.send(message)
        print(f"Sent to WebSocket: {message}")
    except:
        print("WebSocket connection closed. Reconnecting...")
        connect_to_websocket()

def receive_from_websocket():
    while True:
        try:
            response = ws.recv()
            json_response = json.loads(response.decode(encoding='utf-8'))
            react_to_host(json_response)
        except Exception as e:
            print("WebSocket connection closed. Reconnecting...")
            connect_to_websocket()

def send_text_to_wechat(receiver, message):
    if(receiver["group_id"]):
        itchat.send(message, toUserName=receiver["group_id"])
    else:
        itchat.send(message, toUserName=receiver["user_id"])

def send_image_to_wechat(receiver, img):
    image_buffer = BytesIO(img)
    image = Image.open(image_buffer)

    filename = "temp_" + "".join(random.choices(string.ascii_letters + string.digits, k=10)) + ".png"
    image.save(filename)

    image_buffer.close()
    image.close()

    if(receiver["group_id"]):
        itchat.send_image(filename, toUserName=receiver["group_id"])
    else:
        itchat.send_image(filename, toUserName=receiver["user_id"])

    os.remove(filename)

def react_to_host(msg):
    if(msg['type'] == message_type.CHAT_MESSAGE.value):
        for part in msg['content']['parts']:
            if(part['type'] == message_part_type.TEXT.value):
                send_text_to_wechat(msg['receiver'], part['data'])
            elif(part['type'] == message_part_type.IMAGE.value):
                send_image_to_wechat(msg['receiver'], base64.b64decode(part['data']))


def give_id(id):
    json_obj = {
        "type": message_type.ID_GIVING.value,
        "id": id
    }
    json_text = json.dumps(json_obj)
    send_to_websocket(json_text)

def send_chat_message_to_host(msg, isGroupChat=False):

    if(msg["CreateTime"] < login_time):
        return

    json_obj = {
        "type": message_type.CHAT_MESSAGE.value,
        "sender": {
            "group_id": msg["FromUserName"] if isGroupChat else None,
            "user_id": msg["ActualUserName"] if isGroupChat else msg["FromUserName"]
        },
        "content": {
            "id": msg["MsgId"],
            "parts": [
            ]
        }
    }

    if msg["Type"] == TEXT or msg["Type"] == CARD or msg["Type"] == NOTE or msg["Type"] == SHARING:
        json_obj["content"]["parts"].append({
            "type": 0,
            "data": msg["Text"]
        })
    elif msg["Type"] == PICTURE:
        json_obj["content"]["parts"].append({
            "type": 1,
            "data": base64.b64encode(msg.Text()).decode('utf-8')
        })

    json_text = json.dumps(json_obj)
    send_to_websocket(json_text)

@itchat.msg_register([TEXT, MAP, CARD, NOTE, SHARING, PICTURE])
def on_chat_message(msg):
    send_chat_message_to_host(msg)


@itchat.msg_register([TEXT, MAP, CARD, NOTE, SHARING, PICTURE], isGroupChat=True)
def on_chat_message(msg):
    send_chat_message_to_host(msg, True)



if __name__ == '__main__':
    global login_time
    
    itchat.auto_login(hotReload=True)
    login_time = time.time()

    connect_to_websocket()

    th1 = threading.Thread(target=receive_from_websocket)
    th1.start()

    itchat.run(True)

    th1.join()
