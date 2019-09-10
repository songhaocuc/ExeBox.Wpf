# ExeBox.Wpf

## 编译
如果要利用 Constura.Fody编译为单独的exe文件，请用VS2019打开。

## xml配置
```xml
<run 
    cmd="DataSvr_Master" 
    dir="./vc141x64debugs/" 
    param="data_svr.exe ../data/cfg/network/data_svr_master.txt"
    enabled="1" 
    priority="0" 
    tag="1" />
```
`run` : 一项`run`表示一项任务，对应一个单独的进程。  
`cmd` : 表示该项任务的名称。主要用于标识该项任务，在界面中显示。  
`dir` : 表示任务的运行路径。可使用绝对路径或者相对路径，相对路径是对于xml配置文件的相对路径。   
`param` : 运行参数。  
`enabled` : 默认情况下是否选中。`0` 表示不选中。  
`priority` : 任务的优先级。优先级高的任务后执行、先关闭（依赖优先级低的任务）。  
`tag` : exebox中的设置，用途不明。