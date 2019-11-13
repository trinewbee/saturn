
# Nano.Ext.Persist Namespace

## Bin-log Store

提供了面向少量数据的轻量级持久化方案。使用该方案的所有数据均运行在内存中，在应用正常退出，或者调用导出方法时，将全量数据持久化到最新的 Map 文件。同时，任何修改操作都需要写入一条 bin-log。当应用启动时，如果上一次是正常关闭，将载入上次关闭时的 Map 文件（全量数据）。如果是非正常关闭，将载入最后一次正常关闭时的 Map 文件，和后续的 bin-log 文件。使用此方式，来实现数据的持久化。

Bin-log Store功能包含如下类：
- BinlogAccept: 接受 bin-log 数据并写入日志
- BinlogLoader: 用于读取 bin-log
- BinlogAccess: 用户需要实现的回调接口
- BinlogStore: 主类

简单的使用步骤如下。
1. 首先实现 BinlogAccess 接口，用于实现 map 文件的保存和解析，以及 bin-log 文件的解析
2. 创建 BinlogStore 实例，传入实现 BinlogAccess 接口的对象
3. 使用 Open 方法读取数据，在空库上也可以执行。此时，BinlogAccess 的 LoadMap 和 LoadLog 都可能被调用
4. 从 BinlogStore 中获取 BinlogAccept，在修改数据时写入日志（通过 BinlogAccept）
5. 在正常关闭前使用 Close 方法写入数据

使用 BinlogStore 的持久化方案，其 map 和 bin-log 的生成和解析，是由用户自行开发（实现 BinlogAccess 接口）。使用该方案的优势是轻量级、运行效率高。

## BOM Store

BOM Store 在 Bin-log Store 的基础上，增加了序列化的能力。这样，用户不需要再自行编写 map 和 bin-log 的生成和读取逻辑。不过，由于数据读写利用反射机制完成，对运行效率有一定影响。

BOM Store 功能包含如下类：
- BomSaver
- BomLoader

使用 BOM Store，可以自动序列化自定义的数据结构，和常见的容器。但是，需要在定义数据时遵循如下的方案。
- 需要持久化的类定义上需要添加 BomNode 属性，其中需要持久化的字段上也需要添加 BomField 属性。

关于Object Tracker：可以只使用 BomSaver 和 BomLoader（用于实现 BinlogAccess），在这种场景下需要自行维护 bin-log 的写入和解析。BomStore 则带了 Object Tracker 功能，只需要在对象创建、写入调用对应的方法，就可以完成 bin-log 的写入和解析。

---
[Home](../index)
