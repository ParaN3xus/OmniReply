import json
import threading
import requests
import websocket
import base64
from time import sleep
from enum import Enum


import miraicle


ws_uri = "ws://localhost:8080"
ws = websocket.WebSocket()

qq = 
verify_key = "yirimirai"
port = 8082

bot = miraicle.Mirai(qq=qq, verify_key=verify_key, port=port, adapter="ws")


class message_type(Enum):
    ID_GIVING = 0
    CHAT_MESSAGE = 1

class message_part_type(Enum):
    TEXT = 0
    IMAGE = 1
    Record=  2

connecting = False

def connect_to_websocket():
    global connecting
    if(connecting == None):
        connecting = False
    if(connecting):
        return
    connecting = True
    while True:
        try:
            ws.connect(ws_uri)
            print(f"Connected to {ws_uri}")
            give_id("mirai")
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


def react_to_host(msg):
    if(msg['type'] == message_type.CHAT_MESSAGE.value):
        receiver = msg['receiver']
        msg_chain = []
        for part in msg['content']['parts']:
            if(part['type'] == message_part_type.TEXT.value):
                msg_chain.append(miraicle.Plain(part['data']))
            elif(part['type'] == message_part_type.IMAGE.value):
                msg_chain.append(miraicle.Image(base64=base64.b64decode(part['data'])))
        if(receiver["group_id"]):
            bot.send_group_msg(group=receiver["group_id"], msg=msg_chain)
        else:
            bot.send_friend_msg(qq=receiver["user_id"], msg=msg_chain)



def give_id(id):
    json_obj = {
        "type": str(message_type.ID_GIVING.value),
        "id": id
    }
    json_text = json.dumps(json_obj)
    send_to_websocket(json_text)



def download_file_to_base64(url):
    response = requests.get(url)

    if response.status_code == 200:
        base64_str = base64.b64encode(response.content).decode("utf-8")
        return base64_str
    else:
        print('Failed to download file, status code:', response.status_code)
        return None

def send_chat_message_to_host(sourceGroup, sourceUser, msg, isGroupChat=False):
    json_obj = {
        "type": message_type.CHAT_MESSAGE.value,
        "sender": {
            "group_id": str(sourceGroup) if isGroupChat else None,
            "user_id": str(sourceUser)
        },
        "content": {
            "id": str(msg.id),
            "parts": [
            ]
        }
    }

    for part in msg.chain:
        if isinstance(part, miraicle.Plain):
            json_obj["content"]["parts"].append({
                "type": message_part_type.TEXT.value,
                "data": part.text
            })
        elif isinstance(part, miraicle.Image):
            json_obj["content"]["parts"].append({
                "type": message_part_type.IMAGE.value,
                "data": download_file_to_base64(part.url)
            })


    json_text = json.dumps(json_obj)
    send_to_websocket(json_text)



@miraicle.Mirai.receiver('GroupMessage')
def hello_to_group(bot: miraicle.Mirai, msg: miraicle.GroupMessage):
    send_chat_message_to_host(msg.group, msg.sender, msg, True)


@miraicle.Mirai.receiver('FriendMessage')
def hello_to_friend(bot: miraicle.Mirai, msg: miraicle.FriendMessage):
    send_chat_message_to_host(None, msg.sender, msg, False)


if __name__ == '__main__':
    connect_to_websocket()

    th1 = threading.Thread(target=receive_from_websocket)
    th1.start()

    bot.run()

    th1.join()
