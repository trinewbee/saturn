# Menu Builder (Nano.Forms)

CMenuBuilder 用于辅助构建菜单，CCtxMenuBuilder 用于辅助构建上下文菜单。两个类的使用方法类似。

CMenuBuilder 使用例子
```
var mb = new CMenuBuilder();
mb.Begin("&File");
mb.End();
mb.Begin("&Run");
mb.Add("&Run Applet", "RunApplet", Keys.F5, MenuItem_Click);
mb.End();
mb.SetForm(this);
```

CCtxMenuBuilder 使用例子
```
var ccmb = new CCtxMenuBuilder();
ccmb.Add("保存", "Save", onclick: MailListContext);
ccmb.Add("服务端删除", "RemoteDelete", onclick: MailListContext);
listMail.ContextMenuStrip = ccmb.Menu;
```
