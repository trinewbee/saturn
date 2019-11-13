# DObject

DObject 是一个类似 JSON 的动态类簇。用于在 C# 中进行弱类型数据的处理。DObject 不是基于 JsonNode 构造，但是可以导入导出 JsonNode。

## 创建值类型

方法一：使用类型转换
```
DObject o = 123; // int json node (long)
DObject o = 3.14; // number json node (double)
DObject o = true; // boolean json node
DObject o = "Hello"; // string json node
```

上述常规类型（long, double, string, bool）可以隐式转换为 DObject 类型，例如：
```
void f(DObject o) { ... }
f("Hello");
```

方法二：使用 DObject.New 函数。例如：`DObject.New("Hello")`。

## 创建空对象

方法一：var o = DObject.Null();  
方法二：var o = DObject.New(null);  
结果相当于 null json node。

## 创建列表对象

DObject.New 传入实现了 System.Collections.IList 接口的类，或传入数组时，会枚举并创建列表对象。DObject.DList 类也满足该要求。
···
o = DObject.New(new DObject.DList { "Hello", 123, 3.14, true, null });
o = DObject.New(new object[] { "Hello", 123, 3.14, true, null });
o = DObject.New(new List<object> { "Hello", 123, 3.14, true, null });
···

特殊的，Array 和 DList 本身可以隐式转换成 DObject，所以也可以使用如下方式创建。
```
DObject o = new object[] { "Hello", 123, 3.14, true, null };
DObject o = new DObject.DList { "Hello", 123, 3.14, true, null };
```
该方法实质上会调用 DObject.New。

DObject.New 会递归处理列表中的每一个项目。例如：
```
DObject o = new DObject.DList
{
    new DObject.DList { 1, 2 },
    new [] { 3, 4 }
};
```

## 创建字典对象

DObject.New 传入实现了 System.Collections.IDictionary 接口的类，或传入数组时，会枚举并创建字典对象。DObject.DMap 类也满足该要求。
```
o = DObject.New(new DObject.DMap { { "red", 1 }, { "orange", "closed" } });
o = DObject.New(new Dictionary<string, object> { { "red", 1 }, { "orange", "closed" } });
```

另外，DObject.New 也可以处理常规的类（枚举 public 的字段和属性）和匿名类。
```
o = DObject.New(new { red = 1, orange = "closed" });
o = DObject.New(new Test { id = 1, name = "my" });
```

与 DList 类似，DMap 也可以隐式转换为 DObject。该过程实质上也调用了 DObject.New 方法，因此会递归处理每个值。
```
DObject o = new DObject.DMap { { "red", 1 }, { "orange", "closed" } };

DObject o = (DObject)new DObject.DMap {
    { "stat", "ok" },
    { "user", new { name = "Louis" } }
};
```

## 读取简单值

DObject 重载了大量和普通数值的隐式转换函数。因此大部分情况下，可以当成普通变量使用。

整型：
```
DObject o = 123;
long x = o;
int y = (int)o;
if (o > 100) { ... }
```
注意，DObject 和 JsonNode 类似，整型的默认类型是 long，转换成其他整型需要显式类型转换。

浮点型与整型使用方法类似，默认类型是 double。

字符串：
```
DObject o = "Hello";
string s = o;
if (o == "Hello") { ... }
```

布尔型：
```
DObject o = true;
bool f = o;
var f2 = x > y && !o;
if (o) { ... }
if (!o) { ... }
```

Null：
一定要用 `o.IsNull()` 来判断一个对象是不是 null 对象，而不要使用 `o == null`。

## 列表操作

DObject 提供了 Count 方法和只读索引来进行简单的列表读取。例如：
```
var count = o.Count();
var item = o[0];
```

对于复杂的操作（包括枚举），可以使用 List 方法返回的 DList 对象。
```
foreach (var item in o.List())
  ...
```

另外，DList 提供了额外的 Add 方法，接受 object 对象，并且用 New 方法转换成 DObject 对象添加到列表：`o.List().Add("test")`

## 字典操作

字典对象同样有 Count 方法和只读索引。
```
var count = o.Count();
string name = o["name"];
```

特殊的，DObject 在动态对象下，可以直接使用 key 作为属性成员来读取字典中的值。
```
dynamic o = ...;
if (o.stat == "ok")
{
    dynamic user = o.user;
    Console.WriteLine($"name={user.name}, age={user.age}");
}
```

更多的操作（包括枚举），则要通过 Map 方法返回的 DMap 对象。
```
foreach (var key in o.Map().Keys)
  ...
```

同样的，DMap 也添加了额外的 Add 方法，接受 object 对象，转换后加入到字典：`o.Map().Add("name", "Cindy")`。

## JSON 互操作

DObject 可以和 JsonNode 相互导入导出。

导入：
```
JsonNode jnode = ...;
var o = DObject.New(jnode);
var o = DObject.ImportJson(jnode);

string jstr = ...;
var o = DObject.ImportJson(jstr);
```

导出：
```
DObject o = ...;
var jnode = o.ToJson();
var jnode = DObject.ExportJson(o);
var jstr = DObject.ExportJsonStr(o);
```

为了便利，DList 和 DMap 也提供了 ToJson 方法：
```
var jnode = new DObject.DList { 1, 2, 3 }.ToJson();
var jnode = new DObject.DMap { { "name", "Mongo" }, { "age", 7 } }.ToJson();
```
这样写比直接使用 Nano.Json 库要更加简洁。

## Transform 方法

DObject 提供了 Transform 方法将一个可枚举序列变换成所需的列表 DObject 对象。
```
public static DObject Transform<T>(IEnumerable<T> e, Func<T, DObject> tr, Predicate<T> where = null)

public static DObject Transform(System.Collections.IEnumerable e, Func<object, DObject> tr, Predicate<object> where = null)
```

示例：`o = DObject.Transform(users, x => x.name, x => x.stat == 0)`。如果不需要筛选元素，可以省略 where 参数。

## TransformMap 方法

TransformMap 方法用于将一个 `IDictionary<string, T>` 实例变换成字典 DObject 对象。
```
public static DObject TransformMap<T>(IDictionary<string, T> map, Func<T, DObject> tr, Predicate<T> where = null)
```

示例：
```
o = DObject.TransformMap(userMap, x => DObject.New(new { name = x.name, age = x.age }), x => x.stat == 0)
```
如果不需要筛选元素，可以省略 where 参数。

## 其他

GetNodeType 方法可以获取等价的 JsonNodeType。并且，还提供了 IsInt 等系列方法判断 DObject 是否为某种类型。

---
[index](index)
