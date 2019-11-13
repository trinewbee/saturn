# JSON Method Returns Marshal - Designs

Puff Server 2.2 版本的 API 方法数据返回格式，因多个版本迭代造成风格不统一。因此在新版本 3.0 中，重新整理返回规则。同时增加对 value tuple 的支持。

该调整将导致与旧有的使用方式不兼容，并且提高了 Puff Server 的最低运行版本：
- .Net Framework 4.7.2
- .Net Core 2.2

## IceApi 属性

IceApiAttribute 用于修饰 API 方法。在 2.2 版本中，有如下属性与返回格式有关：
- IceApiFlag Flags // Json, Http
- string Ret // 返回对象的 key 表
- string Cookie // 需要写入 cookie 的 key 表
- bool CustomRet // 如果为 true，直接序列化函数返回值

并且，有多种返回方式将被支持：
- 普通值和对象（返回单个值）
- Tuple
- ValueTuple (C# 7)
- JsonNode 和 DObject

我们将 IceApiFlag 更名为 IceApiStyle，包含如下取值：
- Json: 默认值。按照 JSON 格式封送传入和传出的数据。
- Http: 原始 HTTP API 方式，仅在 Web Server 中生效。
- JsonIn: 输入参数处理与 Json 模式相同，输出为自定义。

## IceApiFlag.Json 样式

该选项为默认值。在该选项下，会将函数的返回值封送为 JSON 格式，并且增加一个默认的 stat 方法。

该样式保留了 Ret 和 Cookie 属性，去掉了 CustomRet 属性。

### 无返回

推荐做法是将函数返回值定义为 void。
```
[IceApi()]
void Ping()
```

### 通过复合对象返回

当 IceApi.Ret 属性为空字符串时，转换器按照复合对象返回的方式处理。支持的复合对象类型如下：
- JsonNode, DObject
- ValueTuple (C# 7)
- IDictionary 实现类
- 普通类和匿名类

JsonNode 和 DObject 是等价类型。该模式下要求返回的 JSON 对象必须是字典类型。
```
[IceApi()]
JsonNode GetUserInfo()

[IceApi()]
DObject GetUserInfo()
```

ValueTuple 仅适用于函数返回值（因为只有此时才能通过反射获取名字）。
```
[IceApi()]
(long id, string nick) GetUserInfo(string token)
```

实现了 IDictionary 接口的类（典型的是 Dictionary<K,V>）也可以直接作为返回。不论字典中的 Key 是什么类型，都会调用 ToString 方法转为字符串。
```
[IceApi()]
Dictionary<string, object> GetUserInfo(string token)
```

对于普通类和匿名类，转换器按照遍历所有 public 的字段和可读属性的方式，映射成字典。
```
[IceApi()]
UserInfo GetUserInfo(string token)

[IceApi()]
object GetUserInfo(string token) // 返回一个匿名类实例
```

复合对象会解析成字典类型的 JSON 根对象。因此，可以通过该方式返回任意个结果对象数目，包括 0 个对象。

默认情况下，系统会增加自动的 stat 值。

### 通过 Ret 指定名字列表返回

该方式可以返回一到多个结果对象。当返回一个对象时，推荐使用本方案；当返回多个对象时，推荐使用复合对象。

如果返回一个结果对象，在 Ret 属性中指定名字，然后直接将对象返回即可。
```
[IceApi(Ret = "token")]
string Login(string name, string pwd)
```

如果返回多个结果对象，在 Ret 属性中指定名字列表，并通过下列三种对象之一返回。并且三种对象的
- Tuple, ValueTuple
- Array
- IList 实现类

```
[IceApi(Ret = "id,nick")]
Tuple<long, string> GetUserInfo(string token)

[IceApi(Ret = "id,nick")]
ValueTuple<long, string> GetUserInfo(string token)

[IceApi(Ret = "id,nick")]
(long, string) GetUserInfo(string token)

[IceApi(Ret = "id,nick")]
object[] GetUserInfo(string token)

[IceApi(Ret = "id,nick")]
List<object> GetUserInfo(string token)
```

注意：使用 Tuple / ValueTuple 时，不支持嵌套（因此，最大返回 7 个对象）。

### stat 值

IceApiStyle.Json 模式下会在返回中自动增加默认状态码 stat，如果函数正常返回，值为 ok。

要返回一个不同的 stat 值，有两种方式：
- 直接抛出 NutsException 异常。该方式不能返回 stat 之外的其他值。
- 显式增加一个 stat 返回值（名字与 Stat 属性值相同），该返回值会覆盖系统默认的 stat 值。

IceApiAttribute 新增了 Stat 属性表示默认状态码的名字，默认值为 stat。如果该属性设置为空字符串，则表示返回结果中不自动添加 stat。

另外，通过 IceServiceAttribute.SuccCode 属性，可以修改一个模块中所有 API 的成功状态码。

## IceApiFlag.JsonIn 样式

该模式下用户自定义返回的结果，包括如下几种返回类型。

- string: 将返回的字符串作为 body（UTF-8 编码）
- byte[]: 将返回的二进制数据作为 body
- Stream 派生类：将流中的数据作为 body
- JsonNode, DObject: 将返回的对象的 JSON 字符串作为 body
- IceApiResponse: 符合对象，可以返回 HTTP 状态码、数据和 Cookie 设置（非 Web 服务端会忽略数据之外的项目）
