# 动画轨道工具 For SMBX 38A

这是一个为 SMBX 38A 设计的动画轨道导出工具。

旨在通过 Godot 的动画器工具构建动画序列，再将其中的轨道导出成适合 SMBX 38A 运行的程序脚本 (TeaScript)。关于 TeaScript 的详细内容，可以参见在线文档：[Category:TeaScript.vbs - Moondust Wiki](https://wohlsoft.ru/pgewiki/Category:TeaScript.vbs)

关于 SMBX 侧动画轨道的结构设计以及接口设计，可以参见本地文档 `./smbx 轨道动画器草案.md` 。



## 项目结构

`./gd_engine` 目录下是已经编译好的 Godot 引擎运行时，通常不要去动它。

`./gd_project` 目录下是专用于产出动画序列的 Godot 工程，需要在此工程中新建动画并导出。

该工具主要的工作流程为: 

1. 在 gd_project 项目中搭建好动画，并标定好每个节点对应的轨道索引（idx）。
2. 通过 gd_project 提供的 editor 工具导出动画为 .smt 脚本。
3. 将脚本放入 SMBX 项目中使用。

## 导出的 .smt 脚本

.smt 脚本的相关介绍可以参见：[XiaoDouXd/smbx38a-teascript-vscode-support: VSC语言插件编写学习，以SMBX Teascript为例](https://github.com/XiaoDouXd/smbx38a-teascript-vscode-support) 原则上一个 .smt 对应一个游戏中的 TeaScript 脚本。

对于轨道的数据块，会使用这个工具将其转译成 ascii 字符串并在脚本中以字面量形式呈现：(./utils/binary-ascii/) [XiaoDouXd/binary-ascii: 把一些数据编码成 ascii 字符串](https://github.com/XiaoDouXd/binary-ascii)