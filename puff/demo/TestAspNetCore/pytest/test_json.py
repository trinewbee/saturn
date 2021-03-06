#encoding=utf8

import json
import urllib2

def RequestRaw(url, headers = {}, data = None):
    try:        
        request = urllib2.Request(url=url, headers=headers, data=data)
        r = urllib2.urlopen(request)
        data = r.read()
        r.close()
        return (r.code, r.headers, data)
    except urllib2.HTTPError, e:
        return (e.code, None, None)

def RequestApi(url, **kwargs):
    data = json.dumps(kwargs) if kwargs != None else None
    r = RequestRaw(url, data=data)
    assert r[0] == 200
    return json.loads(r[2])

def RequestApiObj(url, obj):
    data = json.dumps(obj) if obj != None else None
    r = RequestRaw(url, data=data)
    assert r[0] == 200
    return json.loads(r[2])

server = "http://10.211.55.8:5000"

if __name__ == "__main__":
    print "TestJson"

    # Plain GET
    url = server + "/my/ping"
    response = RequestRaw(url)
    assert response[0] == 200
    headers = response[1]
    assert headers["Content-Type"] == "application/json; charset=utf-8"
    assert int(headers["Content-Length"]) == len(response[2])
    assert not ("Set-Cookie" in headers)
    r = json.loads(response[2])
    assert r == {"stat":"ok"}

    url = server + "/my/echo?name=first&count=1&wps_sid=xx"
    r = RequestApiObj(url, None)
    assert r == {"stat":"ok", "name":"first", "count":1}

    url = server + "/my/echo"
    r = RequestApi(url, name="second", count=2)
    assert r == {"stat":"ok", "name":"second", "count":2}

    url = server + "/my/echo?name=third"
    r = RequestApi(url, count=3)
    assert r == {"stat":"ok", "name":"third", "count":3}

    r = RequestApi(server + "/my/swap", x=1, y=2)
    assert r == {"stat":"ok", "x":2, "y":1}

    r = RequestApi(server + "/my/throw", code="first")
    assert r == {"stat":"first"}

    r = RequestApi(server + "/my/throw", code="second", m="go")
    assert r == {"stat":"second", "m":"go"}

    r = RequestApi(server + "/my/div", x=6, y=3)
    assert r == {"stat":"ok", "value":2}

    r = RequestApi(server + "/my/div", x=1, y=0)
    # assert r == {"stat":"InternalServerError"}

    r = RequestApi(server + "/my/stat", stat="true")
    assert r == {"stat":"true"}

    r = RequestApiObj(server + "/my/nostat", None)
    assert r == {}

    # JSON Api with Cookies
    headers = {"Cookie":"name=mo; value=5"}
    response = RequestRaw(server + "/my/cookie", headers=headers)
    assert response[0] == 200
    headers = response[1]
    assert headers["Set-Cookie"] == "name=x-mo; path=/, value=6; path=/"
    r = json.loads(response[2])
    assert r == {"stat":"ok", "name":"x-mo", "value":6}
    
    r = RequestRaw(server + "/my/sayhello?name=Yuki")
    assert r[0] == 200 and r[2] == "Hello, Yuki!"

    r = RequestRaw(server + "/my/savehello?name=Tayo")
    assert r[0] == 200 and r[2] == "Hello, Tayo!"

    # Json Api Request
    response = RequestRaw(server + "/my/GetHost")
    assert r[0] == 200
    r = json.loads(response[2])
    assert r == {"data":"10.211.55.8:5000","stat":"ok"}

    response = RequestRaw(server + "/my/PrintUrl?name=wenliangjun")
    assert response[0] == 200 
    assert response[2] == "wenliangjun! access 10.211.55.8:5000"
    
    print "ok"    
