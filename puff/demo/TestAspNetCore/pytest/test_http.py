#encoding=utf8

import json
import urllib.request
import urllib.error

def RequestRaw(url, headers = {}, data = None):
    try:        
        # 在Python 3中，POST数据需要编码为bytes
        if data and isinstance(data, str):
            data = data.encode('utf-8')
        request = urllib.request.Request(url=url, headers=headers, data=data)
        r = urllib.request.urlopen(request)
        data = r.read()
        r.close()
        # 解码响应数据
        if isinstance(data, bytes):
            data = data.decode('utf-8')
        return (r.code, r.headers, data)
    except urllib.error.HTTPError as e:
        return (e.code, None, None)

def RequestHttp(url, headers = {}, data = None):
    r = RequestRaw(url, headers, data)
    assert r[0] == 200
    return json.loads(r[2])

server = "http://127.0.0.1:5000"

if __name__ == "__main__":
    print("TestHttp")
    url = server + "/my/http"

    # GET    
    response = RequestRaw(url)
    assert response[0] == 200
    headers = response[1]
    assert headers["Content-Type"] == "application/json; charset=utf-8"
    assert int(headers["Content-Length"]) == len(response[2])
    assert not ("Set-Cookie" in headers)
    assert not ("Secret" in headers)
    r = json.loads(response[2])
    assert r["url"] == url and r["path"] == "/my/http" and r["method"] == "GET"
    assert r["qs"] == "" and r["ctype"] == None and r["clen"] == None
    assert r["cookie"] == {} and r["query"] == {}
    assert r["body"] == ""

    # GET with Query String
    r = RequestHttp(url + "?id=1&name=mao")
    assert r["url"] == url and r["path"] == "/my/http" and r["qs"] == "?id=1&name=mao"
    assert r["query"] == {"id":"1", "name":"mao"}

    # POST with Query String
    headers = {"Content-Type":"text/plain"}
    r = RequestHttp(url + "?id=1", headers=headers, data="mao")
    assert r["method"] == "POST" and r["ctype"] == "text/plain" and r["clen"] == 3
    assert r["query"] == {"id":"1"} and r["body"] == "mao"
    assert r["header"]["Content-Type"] == "text/plain"

    # Cookie
    headers = {"Cookie":"name=tao; value=2"}
    response = RequestRaw(url, headers=headers)
    assert response[0] == 200
    headers = response[1]
    assert "Set-Cookie" in headers
    assert headers["Set-Cookie"] == "name=tao; path=/, value=2; path=/"
    r = json.loads(response[2])
    assert r["header"]["Cookie"] == "name=tao; value=2"
    assert r["cookie"] == {"name":"tao", "value":"2"}

    # Custom Header
    headers = {"secret":"kuma"}
    response = RequestRaw(url, headers=headers)
    assert response[0] == 200
    headers = response[1]
    assert "Secret" in headers
    assert headers["Secret"] == "x-kuma"

    print("ok")
