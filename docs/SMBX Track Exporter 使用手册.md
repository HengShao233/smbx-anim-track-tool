# SMBX Track Exporter 使用手册

> Godot 动画 → SMBX 38A TeaScript 轨道数据导出插件
>
> 插件路径：`gd_project/addons/smbx_track_exporter/`
> 作者：xiaodou ｜版本：1.0

---

## 目录

- [1. 概述](#1-概述)
- [2. 工作原理](#2-工作原理)
- [3. 安装与启用](#3-安装与启用)
- [4. 界面说明](#4-界面说明)
- [5. 使用流程](#5-使用流程)
- [6. 参数详解](#6-参数详解)
- [7. 导出产物说明](#7-导出产物说明)
- [8. 常见问题 (FAQ)](#8-常见问题-faq)
- [9. 使用限制](#9-使用限制)
- [10. 附：配置存储位置](#10-附配置存储位置)

---

## 1. 概述

`SMBX Track Exporter` 是一个运行在 Godot 编辑器中的 C# 插件，用于把 Godot `AnimationPlayer` 中的动画轨道（关键帧数据）编码、压缩为 SMBX 38A 引擎可直接读取的 **`.smt` (TeaScript)** 脚本，从而将 Godot 编辑动画的能力桥接到 SMBX 38A 游戏中。

主要应用场景：

- 在 Godot 中使用可视化动画编辑器制作复杂的数值轨道（位置、颜色、旋转、数值等）。
- 一键导出为 SMBX 38A 中可直接 `call exescript(...)` 加载的 TeaScript 字符串常量。
- 在 SMBX 游戏内通过运行时 `AnimTrack.smt` 解析数据并驱动对象运动。

---

## 2. 工作原理

### 2.1 总体流程

```
┌──────────────────┐  读取  ┌──────────────────┐   编码   ┌────────────────┐
│  AnimationPlayer │ ─────▶ │  TrackSettings / │ ─────────▶ │  ASCII 字符串  │
│   (.tscn 场景)   │        │    Value4d       │            │   (64 字符集)  │
└──────────────────┘        └──────────────────┘            └────────┬───────┘
                                                                      │ 包装
                                                                      ▼
                                               ┌──────────────────────────────┐
                                               │ .smt 脚本 (Dim + Export Script)│
                                               └──────────────────────────────┘
```

1. **选中 AnimationPlayer**：用户在 Godot 场景树中选中一个 `AnimationPlayer`，插件读取它的全部动画。
2. **配置每条轨道**：为每条动画轨道指定 SMBX 侧的 `索引 (idx)`、`乘数 (Multiplier)`、`模板 (Template)`。
3. **关键帧归一化**：遍历轨道关键帧，把 `float / Vector2 / Vector3 / Vector4 / Color / Quaternion / Rect2 / Plane` 等类型归约成统一的 4 维向量 `Value4d`，并依据全局极值计算缩放系数 `ValueScale (Mi/Mo/Ai/Ao)`，把浮点数据压进 14 bit 的定点表示区间。
4. **帧号 + 插值编码**：每个关键帧压成一个 16 位整数 `关键帧设置`（高 2 位标志 = `11`，中间 2 位 = 插值模式，低 12 位 = 帧号 1~4096）。
5. **ASCII 化**：所有 `ushort` 经过 `Encoder` 用 64 字符集（`0-9 A-Z a-z # $`）编码成每值 3 字节的 ASCII 字符串，便于嵌入 TeaScript 字符串字面量。
6. **打包为 .smt**：为每个动画生成 `Dim TRACK_DATA__<动画名> As String = "..."` 以及一个 `AnimTrackLoad_<前缀><动画名>` 脚本入口。

### 2.2 数值格式（摘要）

与 `docs/smbx 轨道动画器草案.md` 对齐，一个 16 位值的高两位为类型标志位：

| 类型标志 | 含义 | 取值区间 |
|---|---|---|
| `00` | 整数 | −8191 ~ 8192 |
| `01` | 小数（分母固定 8191） | (−1, 0) ∪ (0, 1) |
| `10` | 特殊值（当前值 / NaN） | — |
| `11` | 关键帧设置（插值 2 bit + 帧号 12 bit） | 帧号 1~4096 |

### 2.3 轨道数据结构

每条轨道由 **8 个 ushort 的头部** + **每个关键帧 (1 + N) 个 ushort** 组成：

```
Header: [总帧数][FPS][关键帧数][Mi][Mo][Ai][Ao][维度]
Body : (keySetting, v_x [, v_y [, v_z [, v_w]]]) × 关键帧数
```

轨道真实值还原公式：

```
value = ((raw * Mi + Ai) * Mo + Ao)
```

### 2.4 代码文件结构

| 文件 | 作用 |
|---|---|
| `plugin.cfg` | Godot 插件描述 |
| `TrackExporterPlugin.cs` | `EditorPlugin` 入口，向编辑器注册 Dock |
| `TrackExporterDock.cs` | 主 Dock UI（选 AnimationPlayer / 动画 / 轨道列表 / 导出按钮） |
| `TrackExporterDock_DataParse.cs` | 数据模型 `TrackSettings`、`Value4d`、`Encoder`、`ValueScale` |
| `TrackExporterDock_Export.cs` | 动画遍历、关键帧归一化、打包 `.smt` 文件 |
| `TrackExporterConfig.cs` / `TrackExportConfig.cs` | JSON 配置持久化（保存在 `user://`） |

---

## 3. 安装与启用

本仓库的 `gd_project` 已经内置了该插件，正常情况下只需启用即可。

1. 用 Godot 4（Mono / C# 版本）打开 `gd_project/project.godot`。
2. 首次打开时需要先构建 C# 项目：菜单栏 **Build** → **Build Project**。
3. 进入 **Project → Project Settings → Plugins**。
4. 找到 `SMBX Track Exporter`，将 `Status` 置为 `Enabled`。
5. 启用后编辑器会在侧边出现一个新的 Dock 页签：**SMBX Track Exporter**。

> 如果看不到 Dock，请确认 C# 已编译成功，并尝试重启编辑器。

---

## 4. 界面说明

启用插件后，Dock 面板自上而下的控件如下：

| 区域 | 控件 | 说明 |
|---|---|---|
| 标题 | `SMBX 轨道动画导出器` | — |
| 动画名前缀 | `LineEdit` | 生成脚本入口 `AnimTrackLoad_<前缀><动画名>` 中的前缀，按 Enter 保存 |
| AnimationPlayer 选择 | 按钮 `使用当前选中的 AnimationPlayer` + Label | 从场景树选中目标节点后点击绑定 |
| 动画 | `OptionButton` + 按钮 `刷新轨道` | 选择当前要查看/导出的动画 |
| 配置模板 | `LineEdit` | 为该动画名指定一个 **配置模板名**，供多动画间共享轨道设置 |
| 轨道列表 | `Tree`（5 列） | 轨道路径 / 类型 / 索引 / 乘数 / 模板 |
| 导出路径 | `LineEdit` + 按钮 `浏览` | 选择 `.smt` 保存位置 |
| 导出按钮 | `导出 .smt` | 批量导出该 `AnimationPlayer` 下的所有动画 |
| 状态栏 | `Label` | 显示提示、错误、导出结果 |

轨道列表列说明：

| 列 | 是否可编辑 | 说明 |
|---|---|---|
| 轨道 | 否 | `节点名(完整路径)`，由动画数据自动生成 |
| 类型 | 否 | Godot 轨道类型（只有 `Value` 类型会真实导出，其它记为空轨道） |
| 索引 (idx) | ✅ | SMBX 侧的轨道索引，**必须 ≥ 0，且 < 63**，未设置或 < 0 的轨道会被跳过 |
| 乘数 (Multiplier) | ✅ | 在导出前对每帧值乘一个系数，支持表达式，详见 §6.2 |
| 模板 (Template) | ✅ | 指定要取向量的哪些分量，详见 §6.3 |

---

## 5. 使用流程

下面给出一次完整的「编辑 → 导出 → 在 SMBX 中使用」的操作流程。

### 步骤 1：在 Godot 中搭建动画

1. 在场景中添加若干节点（任意类型，有可动画属性即可，例如 `Node2D`、`Sprite2D`）。
2. 添加一个 `AnimationPlayer`，新建动画（如 `Walk`）。
3. 在动画编辑器里为节点添加 **Value 轨道**（例如 `position:x`、`position:y` 或 `modulate` 等）。
4. 添加关键帧。

> **注意**：导出时 **FPS 被硬编码为 60**，动画总时长上限约 `4095 / 60 ≈ 68.25 秒`。

### 步骤 2：打开并绑定 Dock

1. 切到 **SMBX Track Exporter** 面板。
2. 在场景树里点选要导出的 `AnimationPlayer`。
3. 点击 **「使用当前选中的 AnimationPlayer」**，右侧 Label 会显示节点名，同时 **动画** 下拉框会被填充。

### 步骤 3：配置每条轨道

对需要导出的每条轨道设置：

- **索引 (idx)**：填入 `0 ~ 62` 的整数。多个动画共享 idx 时会被当作"同一条轨道"。
- **乘数 (Multiplier)**：默认为空（即 1）。若填 `0.5` / `2` / `pi` / `1/60` / `inv_pi` 等会按 §6.2 规则解析。
- **模板 (Template)**：根据轨道值的类型选择要导出的分量，如 `xy`、`xyz`、`rgba`；留空则直接使用原值。

> 在轨道列表任意单元格完成编辑并按 Enter / 点击其它位置后，配置会自动写入 `user://smbx_track_exporter_config.json`。

#### 配置模板（多动画复用）

在 **配置模板** 输入框中给当前动画命名一个模板：

- 留空：每个动画有独立的轨道配置。
- 填入一个共享名（例如 `Humanoid`）：**所有** 具有相同模板名的动画会共用同一份轨道设置。
- 修改模板名时轨道列表会实时刷新。

这对于同一套骨骼上多段动画（`Walk` / `Run` / `Idle`）非常有用——你只需在其中一个动画里配好 idx/乘数，其他动画自动继承。

### 步骤 4：设置导出参数

- **动画名前缀** (`_animPrefix`)：会拼在生成的 `AnimTrackLoad_` 函数名中，例如前缀为 `Knight`，动画名为 `Walk`，则生成 `AnimTrackLoad_KnightWalk()`。前缀是 **按 AnimationPlayer 节点名** 分别保存的。
- **导出路径**：点击 **浏览**，选择目标 `.smt` 文件。路径会被记录，下次打开仍保留。

### 步骤 5：导出

点击 **「导出 .smt」**：

- 插件会遍历 **该 AnimationPlayer 下的所有动画**，每个动画独立编码成一行 `Dim TRACK_DATA__<动画名> As String = "..."`。
- 为每个动画生成一个 `Export Script AnimTrackLoad_<前缀><动画名>(Return Integer)` 入口。
- 底部状态栏会显示：`👌导出完成: Walk,Run,Idle` 这样的动画清单。

### 步骤 6：在 SMBX 38A 中使用

假设导出文件名为 `Anim.smt`：

```vbscript
call exescript(Libs)       ' 运算支撑库
call exescript(AnimTrack)  ' 动画解码与采样库（项目提供的运行时）
call exescript(Anim)       ' 刚刚导出的数据脚本

' 加载某个动画到运行时
Call AnimTrackLoad_Walk()

' 在 Do 循环中采样（以 60fps 为基准）
Dim t As Double = timestamp / 60
Call AnimTrack_CalcValue(1, t)        ' idx = 1 的轨道
Dim x As Double = AnimTrack_GetX()
Dim y As Double = AnimTrack_GetY()
```

可以参考 `tea_scripts/example/Main.smt` 的完整调用示例。

---

## 6. 参数详解

### 6.1 索引 idx

- 类型：`int`
- 合法范围：`0 ~ 62`（超过 62 会被夹成 62 并在状态栏告警；`< 0` 的轨道直接被跳过不导出）。
- 同一动画内 **idx 不应重复**（重复时后者覆盖前者）。
- 导出时按 idx 升序排列，中间缺口会被填充为 "空轨道"（长度为 0 的占位），从而保证 `AnimTrack_CalcValue(idx, t)` 在 SMBX 侧按 idx 直接索引不会错位。

### 6.2 乘数 Multiplier

用于导出前对原始值整体缩放，支持以下写法（大小写不敏感，空白会被 trim）：

| 写法 | 含义 |
|---|---|
| 空 / 空白 | 1 |
| `2`, `0.5`, `-3.14` | 普通浮点 |
| `1/60` | 除法，解析为 `1 ÷ 60` |
| `pi`, `2pi`, `0.5pi` | π 及其倍数（π ≈ 3.14159...） |
| `e`, `3e` | 自然常数 e |
| `inv_pi`, `inv_2pi`, `inv_3` | `inv_` 前缀表示取倒数，如 `inv_pi = 1/π` |

计算逻辑定义在 `TrackSettings.Multiplier` 的 setter 中，解析失败时回退为 1。

### 6.3 模板 Template

用于从原始值中挑选/映射分量到输出向量（最多 4 维），只识别字母字符，其它字符（空格、下划线）会被忽略。合法字母：

| 字母 | 映射到源分量 | 等价字母 |
|---|---|---|
| `x` / `r` | 分量 0 | — |
| `y` / `g` | 分量 1 | — |
| `z` / `b` | 分量 2 | — |
| `w` / `a` | 分量 3 | — |

示例：

- 空：按轨道类型默认使用全部维度。
- `xy`：只取 `x, y`（常用于 `Vector3` 只导出平面分量）。
- `xxxx`：所有输出位都用 `x`（常用于标量轨道扩维）。
- `rgb`：`Color` 只导出 RGB，忽略 Alpha。
- 如果模板里写了超出源维度的索引（例如源是 `Vector2` 却写了 `z`），该位会被回退为 `x`。

### 6.4 支持的 Godot 轨道值类型

`Value4d.Parse` 支持：`bool / int / float / Vector2(I) / Vector3(I) / Vector4(I) / Rect2(I) / Plane / Quaternion / Color`。其它类型（如 `Transform2D`、`NodePath`、字符串）将产生 `Dimension = 0`，轨道会被跳过。

### 6.5 插值类型映射

| Godot | 导出值 (2 bit) | 含义 |
|---|---|---|
| `Nearest` | 0 | 常量 / 最近邻 |
| `Linear` | 1 | 线性 |
| `LinearAngle` / `CubicAngle` | 2 | 三角（角度插值） |
| `Cubic` | 3 | 三次 |

---

## 7. 导出产物说明

一个导出的 `.smt` 结构大致如下：

```vbscript
' SMBX Track Script (generated by Godot)
' Animation: <AnimationPlayer 名>
' Paste this script into your TeaScript loader

' Animation: Walk
Dim TRACK_DATA__Walk As String = "....ASCII..."
' Animation: Run
Dim TRACK_DATA__Run As String = "....ASCII..."

Export Script AnimTrackLoad_<前缀>Walk(Return Integer)
    If TRACK_DATA__Walk = "" Then
        Return 0
    End If
    If AscW(Mid(TRACK_DATA__Walk, 1, 1)) <> 0 Then
        TRACK_DATA__Walk = AnimTrack_Internal_Decode(TRACK_DATA__Walk)
    End If
    Call AnimTrack_Internal_PushSource(TRACK_DATA__Walk)
End Script

Export Script AnimTrackLoad_<前缀>Run(Return Integer)
    ...
End Script
```

其中：

- **字符串编码字符集**：`0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#$`（共 64 字符），每个 `ushort` 占 3 个字符。
- **首字节**：为 `Encoder.Encode(tracks.Count)`，表示轨道数量。
- **随后 3×轨道数**：每条轨道的起始偏移（从 1 开始），空轨道偏移为 0。
- **剩余内容**：按 idx 升序排列的轨道数据。

解码过程由 `AnimTrack.smt`（仓库 `tea_scripts/AnimTrack.smt`）在 SMBX 侧完成。首次 `AnimTrackLoad_xxx()` 调用会触发一次内部 decode + push source，后续可以通过 `AnimTrack_CalcValue(idx, t)` / `AnimTrack_GetX/Y/Z/W` 读取采样结果。

---

## 8. 常见问题 (FAQ)

**Q1：导出后状态栏显示「🙅‍导出失败：请检查轨道设置」。**
A：请确认至少有一条轨道满足以下条件：类型为 `Value`、`idx ≥ 0`、关键帧数 > 0、值为支持的数值类型。

**Q2：状态栏提示「⚠ 轨道数不允许超过 63 条」。**
A：SMBX 侧轨道 idx 上限是 62（最多 63 条），请把 idx 改小或移除多余轨道。

**Q3：数据在 SMBX 侧解出来不对 / 精度差很多。**
A：检查：
- 轨道值范围是否被 `Multiplier` 放大到 16 位能表达的尺度。若原始值非常小（< 1），可能会落入定点小数区间；若非常大（> 8192），会被 `ValueScale` 动态压缩，但精度会下降。
- 控制台是否打印了 `可能存在的误差: ... != ...`，这是 `Value4d.Normalize` 检测到的舍入误差。

**Q4：多个动画共享相同的骨骼/轨道结构，要给每个动画都配置 idx 吗？**
A：不需要。在 **配置模板** 里填一个相同的名字（例如 `Humanoid`），所有使用该模板的动画共用轨道配置。

**Q5：配置保存在哪里？删掉怎么办？**
A：见 §10。删掉配置文件不会影响工程，只会丢失轨道 idx / 乘数 / 模板的设置。

**Q6：FPS 可以改吗？**
A：当前版本硬编码为 60（见 `TrackExporterDock_Export.cs` 的 `const int fps = 60`）。如需修改需要改源码并重新编译。

---

## 9. 使用限制

- **FPS 固定为 60**，动画长度上限约 `68.25s`（`4095` 帧）。
- **单动画轨道数 ≤ 63**（idx ∈ `[0, 62]`）。
- **关键帧位置分辨率**：1 帧 = 1/60 s，亚帧精度会被四舍五入。
- **只导出 Value 类型轨道**（`Bezier`、`Method` 等轨道会作为空轨道占位）。
- **数值范围**：单分量理论表示范围约 `[-8191, 8192]`（整数）或 `(-1, 1)`（小数定点），更大的数值需要依赖 `ValueScale` 的 `Mi/Mo/Ai/Ao` 缩放，精度随范围增大而下降。

---

## 10. 附：配置存储位置

- 文件路径：`user://smbx_track_exporter_config.json`
- 在 Windows 下通常位于：
  `%APPDATA%\Godot\app_userdata\<项目名>\smbx_track_exporter_config.json`
- 内容结构（简化）：

```json
{
  "animations": {
    "@prefix@<Player节点名>": "Knight",
    "@src_settings@<动画名>": { "@template@": "<模板名>" },
    "@template@<模板名>": {
      "@track_settings@<轨道路径>": {
        "idx": 1,
        "multiplier": "1/60",
        "template": "xy"
      }
    },
    "@export_path@": "res://dist/Anim.smt"
  }
}
```

其中的特殊前缀约定：

| 前缀 | 含义 |
|---|---|
| `@prefix@<PlayerName>` | 该 AnimationPlayer 的脚本函数名前缀 |
| `@src_settings@<AnimName>` | 动画 → 模板名的映射 |
| `@template@<TemplateName>` | 模板下的 **所有轨道设置** 容器 |
| `@track_settings@<TrackPath>` | 单条轨道的 `idx/multiplier/template` |
| `@export_path@` | 上次选择的导出文件路径 |

---

> 更多底层数据结构细节可阅读 `docs/smbx 轨道动画器草案.md`；运行时解码脚本参见 `tea_scripts/AnimTrack.smt`；完整使用示例参见 `tea_scripts/example/Main.smt`。
