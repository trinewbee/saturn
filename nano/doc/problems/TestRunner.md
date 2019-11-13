# TestRunner

## 2018/7/7 无法加载类型
在加载组件 Nuts.Script 的单元测试时，TestRunner 抛出异常：无法加载一个或多个请求的类型。

经过调试，发现是加载了 Nuts.Script 之后，在 GetTypes 函数触发的异常。异常信息里面显示无法加载 Nuts.CodeModel.dll。它是 Nuts.Script 依赖的一个工程。

根据 
https://stackoverflow.com/questions/20605312/could-not-load-file-or-assembly-error-on-gettypes-method
的信息，将 LoadFile 改成 LoadFrom，在 TestRunner 工程中正常运行。

然而，将 TestRunner.exe 复制到发布目录 shared/cs/pc，用该目录启动还是抛出同样的异常，而在其他目录就不会。这是因为发布目录中有 Nuts.CodeModel.dll 的发布版本组件，而该组件没有更新。虽然指定了 Nuts.Script.dll 的路径，但是在加载依赖项时，还是优先加载 exe 所在目录的同名组件了，结果导致加载了不是最新版本的 Nuts.CodeModel.dll。

最稳妥的解决方案是将 TestRunner.exe 复制到要调试的工程目录。以保证所依赖的 dll 全部是最新版本。
