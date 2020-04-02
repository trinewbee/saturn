# Nano.Forms.Miu Namespace

## Summary

Nano.Forms.Miu 提供了构造简单窗体应用的支持，用于开发基于 WinForms，UI 要求不高的应用程序。

## 基础窗体

实现一个最简单的单窗体应用，需要完成如下工作：
- 实现 MiuApplication 接口，用于管理整个应用
- 实现 MiuView，每个不同的页面使用不同的实现
- 创建 MiuForm 实例并作为主窗体

可以使用 MiuViewHost 的 SwitchView 将界面切换成新的 View。其中，dispose 参数用于指示本 view 是直接清除还是保留在堆栈中，默认是保留在堆栈 (false)。

对于一个主 View 创建的临时性的新 View (例如，填写一些内容并返回)，主 View 应该保存在堆栈中，当新 View 完成任务后，调用 PopView 方法退回到主 View。注意，此时主 View 的 InitUI 方法会被再次调用，亦即，一个 View 在 Dispose 之前可能会初始化多次。
