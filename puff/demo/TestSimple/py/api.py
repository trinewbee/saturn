#encoding=utf8

import json
import urllib.request

def RequestUrl(url, data):
    try:
        r = urllib.request.urlopen(url, data.encode('utf-8') if data else None)
        cookies = r.headers.get("Set-Cookie")
        if cookies != None:
            print ("Set-Cookie: ") + cookies
        data = r.read()
        r.close()
        return (r.code, data)
    except urllib.error.HTTPError as e:
        return (e.code, None)  

def RequestApi(url, **kwargs):
    data = json.dumps(kwargs) if kwargs != None else None
    r = RequestUrl(url, data)
    assert r[0] == 200
    return json.loads(r[1])

def RequestApiObj(url, obj):
    data = json.dumps(obj) if obj != None else None
    r = RequestUrl(url, data)
    assert r[0] == 200
    return json.loads(r[1])

server = "http://localhost:8080"

# api

def Ping():
    url = server + "/api/ping"
    return RequestApi(url)

def Hello(name, age):
    url = server + "/api/hello"
    return RequestApi(url, name=name, age=age)

def Info(name, age):
    url = server + "/api/info"
    return RequestApi(url, name=name, age=age)

def InfoV(name, age):
    url = server + "/api/InfoV"
    return RequestApi(url, name=name, age=age)

def InfoQ(name, age):
    url = server + ("/api/info?name=%s&age=%d" % (name, age))
    return RequestApiObj(url, None)

def InfoCG(name, age):
    url = server + "/api/InfoCG"
    return RequestApi(url, name=name, age=age)

def InfoDO(name, age):
    url = server + "/api/InfoDO"
    return RequestApi(url, name=name, age=age)

def Throw(s):
    url = server + "/api/throw"
    return RequestApi(url, s=s)

def Stat():
    url = server + "/api/Stat"
    return RequestApi(url)

def NoStat():
    url = server + "/api/NoStat"
    return RequestApi(url)

def Raw():
    url = server + "/api/raw"
    return RequestUrl(url, None)
