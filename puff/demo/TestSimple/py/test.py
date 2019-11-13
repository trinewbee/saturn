# coding:utf-8

from api import *

def TestBasic():
    print "TestBasic"

    r = Ping()
    assert r["stat"] == "ok"

    r = Hello("Mongo", 7)
    assert r["stat"] == "ok" and r["m"] == "My name is Mongo. I'm 7 yrs old."

    r = Info("Mongo", 7)
    assert r["stat"] == "ok" and r["name"] == "Mongo" and r["age"] == 7

    r = InfoV("Melon", 9)
    assert r == { "stat":"ok", "name":"Melon", "age":9 }

    r = InfoQ(r"%E5%91%B5", 7)
    assert r == { "stat":"ok", "name":u"呵", "age":7 }

    r = InfoCG("Mongo", 7)
    assert r["stat"] == "ok" and r["info"] == { "name":"Mongo", "age":7 }

    r = InfoDO("Mongo", 7)
    assert r["stat"] == "ok" and r["info"] == { "name":"Mongo", "age":7 }

    r = Throw("nani")
    assert r["stat"] == "nani"

    r = Stat()
    assert r == { "stat":"false", "value":100 }

    r = NoStat()
    assert r == { "value":100 }

    r = Raw()
    assert r == (200, "Haruhi")

if __name__ == '__main__':
    TestBasic()
    print "All done"
