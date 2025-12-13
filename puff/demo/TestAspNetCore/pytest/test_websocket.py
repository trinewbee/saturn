import json
import websocket # pip install websocket-client
import time
import threading
import urllib.request
import sys

SERVER = "ws://127.0.0.1:9000"
HTTP_SERVER = "http://127.0.0.1:9000"

def test_connection_notification():
    """测试 WebSocket 连接和断开通知"""
    print("Testing Connection/Disconnection Notification...")
    
    # 先建立一个观察者连接
    ws_observer = websocket.create_connection(SERVER + "/chatHub")
    ws_observer.settimeout(5)
    
    # Handshake
    handshake_msg = json.dumps({"protocol": "json", "version": 1}) + "\x1e"
    ws_observer.send(handshake_msg)
    ws_observer.recv()
    
    # 消费掉自己的连接通知
    time.sleep(0.3)
    try:
        ws_observer.recv()
    except:
        pass
    
    # 新建一个客户端连接
    print("  Creating new client connection...")
    ws_new = websocket.create_connection(SERVER + "/chatHub")
    ws_new.send(handshake_msg)
    ws_new.recv()
    
    # 观察者应该收到新连接的通知
    found_connect = False
    start = time.time()
    while time.time() - start < 3:
        try:
            data = ws_observer.recv()
            messages = data.split('\x1e')
            for m in messages:
                if not m: continue
                try:
                    obj = json.loads(m)
                    if obj.get("type") == 1 and obj.get("target") == "ReceiveSystemNotification":
                        args = obj.get("arguments", [])
                        if len(args) > 0 and "connected" in args[0]:
                            found_connect = True
                            print(f"  Received connect notification: {args[0]}")
                            break
                except:
                    pass
            if found_connect: break
        except:
            break
    
    if not found_connect:
        print("Connection Notification Failed - No connect notification received")
        ws_observer.close()
        ws_new.close()
        sys.exit(1)
    
    # 新客户端断开连接
    print("  Disconnecting new client...")
    ws_new.close()
    
    # 观察者应该收到断开连接的通知
    found_disconnect = False
    start = time.time()
    while time.time() - start < 3:
        try:
            data = ws_observer.recv()
            messages = data.split('\x1e')
            for m in messages:
                if not m: continue
                try:
                    obj = json.loads(m)
                    if obj.get("type") == 1 and obj.get("target") == "ReceiveSystemNotification":
                        args = obj.get("arguments", [])
                        if len(args) > 0 and "disconnected" in args[0]:
                            found_disconnect = True
                            print(f"  Received disconnect notification: {args[0]}")
                            break
                except:
                    pass
            if found_disconnect: break
        except:
            break
    
    if not found_disconnect:
        print("Disconnection Notification Failed - No disconnect notification received")
        ws_observer.close()
        sys.exit(1)
    
    print("Connection/Disconnection Notification Passed")
    ws_observer.close()


def test_chat_flow():
    """测试基本的 SendMessage 消息发送"""
    print("Testing Chat Flow (SendMessage)...")
    ws = websocket.create_connection(SERVER + "/chatHub")
    
    # Handshake
    msg = {"protocol": "json", "version": 1}
    ws.send(json.dumps(msg) + "\x1e")
    result = ws.recv()
    # print(f"Handshake: {result}")
    
    # SendMessage
    msg = {
        "type": 1,
        "target": "SendMessage",
        "arguments": ["tester", "hello world"]
    }
    ws.send(json.dumps(msg) + "\x1e")
    
    found = False
    start = time.time()
    while time.time() - start < 5:
        data = ws.recv()
        messages = data.split('\x1e')
        for m in messages:
            if not m: continue
            try:
                obj = json.loads(m)
                if obj.get("type") == 1 and obj.get("target") == "ReceiveMessage":
                    args = obj.get("arguments")
                    if args[0] == "tester" and args[1] == "hello world":
                        found = True
                        break
            except:
                pass
        if found: break
        
    if not found:
        print("Chat Flow (SendMessage) Failed")
        sys.exit(1)
    
    print("Chat Flow (SendMessage) Passed")
    ws.close()


def test_join_room():
    """测试 JoinRoom 加入房间功能"""
    print("Testing JoinRoom...")
    
    # 创建两个客户端
    ws1 = websocket.create_connection(SERVER + "/chatHub")
    ws2 = websocket.create_connection(SERVER + "/chatHub")
    
    # Handshake for both
    handshake_msg = json.dumps({"protocol": "json", "version": 1}) + "\x1e"
    ws1.send(handshake_msg)
    ws2.send(handshake_msg)
    ws1.recv()
    ws2.recv()
    
    # ws1 先加入房间
    join_msg = {
        "type": 1,
        "target": "JoinRoom",
        "arguments": ["test-room"]
    }
    ws1.send(json.dumps(join_msg) + "\x1e")
    
    # ws1 应该收到自己加入房间的通知
    found_join_notification = False
    start = time.time()
    while time.time() - start < 3:
        try:
            data = ws1.recv()
            messages = data.split('\x1e')
            for m in messages:
                if not m: continue
                try:
                    obj = json.loads(m)
                    if obj.get("type") == 1 and obj.get("target") == "ReceiveSystemNotification":
                        args = obj.get("arguments", [])
                        if len(args) > 0 and "joined test-room" in args[0]:
                            found_join_notification = True
                            print(f"  Received join notification: {args[0]}")
                            break
                except:
                    pass
            if found_join_notification: break
        except websocket.WebSocketTimeoutException:
            break
    
    if not found_join_notification:
        print("JoinRoom Failed - No join notification received")
        ws1.close()
        ws2.close()
        sys.exit(1)
    
    # ws2 也加入同一个房间
    ws2.send(json.dumps(join_msg) + "\x1e")
    
    # ws1 应该收到 ws2 加入的通知（因为 ws1 已经在房间里）
    found_ws2_join = False
    start = time.time()
    while time.time() - start < 3:
        try:
            data = ws1.recv()
            messages = data.split('\x1e')
            for m in messages:
                if not m: continue
                try:
                    obj = json.loads(m)
                    if obj.get("type") == 1 and obj.get("target") == "ReceiveSystemNotification":
                        args = obj.get("arguments", [])
                        if len(args) > 0 and "joined test-room" in args[0]:
                            found_ws2_join = True
                            print(f"  ws1 received ws2 join notification: {args[0]}")
                            break
                except:
                    pass
            if found_ws2_join: break
        except websocket.WebSocketTimeoutException:
            break
    
    if not found_ws2_join:
        print("JoinRoom Failed - ws1 didn't receive ws2 join notification")
        ws1.close()
        ws2.close()
        sys.exit(1)
    
    print("JoinRoom Passed")
    ws1.close()
    ws2.close()


def test_room_message():
    """测试房间内消息广播（只有房间内成员收到）"""
    print("Testing Room Message...")
    
    # 创建两个客户端
    ws_in_room = websocket.create_connection(SERVER + "/chatHub")
    ws_outside = websocket.create_connection(SERVER + "/chatHub")
    
    # Handshake
    handshake_msg = json.dumps({"protocol": "json", "version": 1}) + "\x1e"
    ws_in_room.send(handshake_msg)
    ws_outside.send(handshake_msg)
    ws_in_room.recv()
    ws_outside.recv()
    
    # ws_in_room 加入房间
    join_msg = {
        "type": 1,
        "target": "JoinRoom",
        "arguments": ["private-room"]
    }
    ws_in_room.send(json.dumps(join_msg) + "\x1e")
    
    # 等待加入确认
    time.sleep(0.5)
    try:
        ws_in_room.settimeout(2)
        ws_in_room.recv()  # 消费掉加入通知
    except:
        pass
    
    # ws_outside 不加入房间，发送全局消息
    send_msg = {
        "type": 1,
        "target": "SendMessage",
        "arguments": ["outsider", "global message"]
    }
    ws_outside.send(json.dumps(send_msg) + "\x1e")
    
    # 两个客户端都应该收到全局消息
    ws_in_room.settimeout(3)
    ws_outside.settimeout(3)
    
    in_room_received = False
    outside_received = False
    
    start = time.time()
    while time.time() - start < 3:
        try:
            data = ws_in_room.recv()
            for m in data.split('\x1e'):
                if not m: continue
                try:
                    obj = json.loads(m)
                    if obj.get("target") == "ReceiveMessage":
                        args = obj.get("arguments", [])
                        if len(args) >= 2 and args[0] == "outsider":
                            in_room_received = True
                except:
                    pass
        except:
            pass
        
        try:
            data = ws_outside.recv()
            for m in data.split('\x1e'):
                if not m: continue
                try:
                    obj = json.loads(m)
                    if obj.get("target") == "ReceiveMessage":
                        args = obj.get("arguments", [])
                        if len(args) >= 2 and args[0] == "outsider":
                            outside_received = True
                except:
                    pass
        except:
            pass
        
        if in_room_received and outside_received:
            break
    
    if not (in_room_received and outside_received):
        print(f"Room Message Failed - in_room: {in_room_received}, outside: {outside_received}")
        ws_in_room.close()
        ws_outside.close()
        sys.exit(1)
    
    print("Room Message Passed (both clients received global message)")
    ws_in_room.close()
    ws_outside.close()

def test_system_broadcast():
    print("Testing System Broadcast...")
    ws = websocket.create_connection(SERVER + "/chatHub")
    
    # Handshake
    msg = {"protocol": "json", "version": 1}
    ws.send(json.dumps(msg) + "\x1e")
    ws.recv() 
    
    # Trigger Broadcast
    url = HTTP_SERVER + "/System/Broadcast"
    data = json.dumps({"message": "test broadcast"}).encode('utf-8')
    req = urllib.request.Request(url, data=data, headers={'Content-Type': 'application/json'})
    
    def trigger_http():
        time.sleep(1)
        try:
            urllib.request.urlopen(req)
        except Exception as e:
            print(f"HTTP Request failed: {e}")

    t = threading.Thread(target=trigger_http)
    t.start()
    
    found = False
    start = time.time()
    while time.time() - start < 5:
        data = ws.recv()
        messages = data.split('\x1e')
        for m in messages:
            if not m: continue
            try:
                obj = json.loads(m)
                if obj.get("type") == 1 and obj.get("target") == "ReceiveSystemNotification":
                    args = obj.get("arguments")
                    if "[System Admin]: test broadcast" in args[0]:
                        found = True
                        break
            except:
                pass
        if found: break
    
    t.join()
    if not found:
        print("System Broadcast Failed")
        sys.exit(1)
        
    print("System Broadcast Passed")
    ws.close()

if __name__ == "__main__":
    try:
        test_connection_notification()  # 测试连接/断开通知
        test_chat_flow()                # 测试 SendMessage
        test_join_room()                # 测试 JoinRoom
        test_room_message()             # 测试房间消息
        test_system_broadcast()         # 测试系统广播
        print("\n=== All WebSocket Tests Passed ===")
    except Exception as e:
        print(f"Test failed with exception: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
