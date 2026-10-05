# Combolands 中文化模组

[繁體中文](README.md)｜[简体中文](README_zh_cn.md)

为 **Combolands** 提供繁体中文／简体中文界面的社区中文化模组。

安装后，你可以直接在游戏的 **Settings** 菜单切换：

- **English**
- **繁體中文**
- **简体中文**

模组还提供 **80%～140% 的文字缩放**，方便根据屏幕尺寸和阅读习惯调整界面文字。

> 当前版本：**v0.4.0**
> 适用平台：**Windows**  
> 需要预先安装：**BepInEx 5.x**

## 效果预览

### 简体中文

![Combolands 简体中文模组效果预览](screenshot/zh_cn.jpg)

### 繁体中文

![Combolands 繁体中文模组效果预览](screenshot/zh_tw.jpg)

## 功能

- 繁体中文界面翻译
- 繁体中文／简体中文模式均使用中文化主菜单 LOGO，English 保留原版 LOGO
- 简体中文即时转换
- 可随时切回原版英文
- 无需重启游戏即可切换语言
- 文字大小可调整为 80%～140%
- 语言与文字大小会自动记住
- 自动使用可用的中日韩系统字体，降低缺字／方框字问题
- 找不到翻译的文字会保留原本英文，不会显示空白内容

## 安装

### 1. 先安装 BepInEx

本模组需要 **BepInEx 5.x** 才能加载。

如果你的 Combolands 已经可以正常使用其他 BepInEx 模组，可以直接进行下一步。

如果尚未安装 BepInEx，请先从官方 Release 页面下载：

**[前往 BepInEx 官方下载页面](https://github.com/BepInEx/BepInEx/releases)**

Combolands 是 Windows x64 游戏，请选择 **BepInEx 5.x 的 Windows x64 压缩包**，文件名会类似：

~~~text
BepInEx_win_x64_5.x.x.x.zip
~~~

目前官方 BepInEx 5 最新稳定版为 **5.4.23.5**，对应文件为：

~~~text
BepInEx_win_x64_5.4.23.5.zip
~~~

下载后，将 BepInEx 压缩包内容直接解压到放有 `Combolands.exe` 的游戏根目录，再启动游戏一次，让 BepInEx 创建所需的文件夹与配置文件。

> 请使用 **BepInEx 5.x**。BepInEx 6 目前仍有预发行版本，而且 BepInEx 5 插件不能直接使用 BepInEx 6 预发行版加载。  
> 本中文化模组的下载包**不包含 BepInEx**。

### 2. 下载中文化模组

前往 GitHub Releases：

**[下载最新版本](../../releases/latest)**

下载：

~~~text
Localization-vX.Y.Z.zip
~~~

例如 v0.4.0 对应：

~~~text
Localization-v0.4.0.zip
~~~

普通玩家只需要下载 ZIP，不需要下载源代码。

### 3. 解压到游戏目录

将 ZIP **直接解压到 Combolands 游戏根目录**。

游戏根目录就是放有 `Combolands.exe` 的文件夹。

如果系统询问是否合并 `BepInEx` 文件夹，请选择合并。

安装完成后应该可以看到：

~~~text
Combolands/
├── Combolands.exe
└── BepInEx/
    └── plugins/
        └── Localization/
            ├── Localization.dll
            └── zh-Hant.json
~~~

请特别确认 `Localization.dll` 与 `zh-Hant.json` 在**同一个文件夹**。

### 4. 启动游戏

正常启动 Combolands。

第一次成功加载模组后，中文化设置会出现在游戏原本的 **Settings** 菜单中。

默认语言为 **繁体中文**。

## 使用方法

### 切换语言

进入：

**Settings → Language**

可以选择：

- `English`
- `繁體中文`
- `简体中文`

选择后点击游戏原本的 **Apply** 按钮。

语言会立即应用，不需要重新启动游戏。

如果点击 **Cancel**，本次尚未应用的语言变更会取消。

### 调整文字大小

在 Settings 中可以看到：

**文字缩放比例**

可调整范围：

~~~text
80% ～ 140%
~~~

每次调整 5%。

例如：

- 80%：较小文字，适合希望界面更紧凑的玩家
- 100%：默认大小
- 120%～140%：较大文字，适合高分辨率屏幕或希望提高可读性的玩家

调整后同样需要点击 **Apply**。

### 设置会自动保存

成功点击 Apply 后，模组会记住：

- 使用的语言
- 文字缩放比例

下次启动游戏时会自动使用上次应用的设置。

## 更新模组

更新前建议先关闭游戏。

1. 从 [Releases](../../releases) 下载新版 `Localization-vX.Y.Z.zip`
2. 再次解压到 Combolands 游戏根目录
3. 选择覆盖旧的 `Localization.dll` 与 `zh-Hant.json`
4. 启动游戏

原本选择的语言与文字大小通常会保留，不需要重新设置。

## 卸载模组

先关闭游戏，然后删除：

~~~text
BepInEx/plugins/Localization/
~~~

这样即可停止加载中文化模组。

如果也希望删除模组保存的语言／文字大小设置，可以另外删除：

~~~text
BepInEx/config/com.combolands.localization.cfg
~~~

卸载中文化模组不需要修改游戏存档。

## 常见问题

### 安装后游戏仍然是英文

先检查以下几点：

1. BepInEx 是否已正确安装并能正常加载模组
2. 文件是否位于：

~~~text
BepInEx/plugins/Localization/Localization.dll
BepInEx/plugins/Localization/zh-Hant.json
~~~

3. 是否不小心多解压了一层文件夹，例如：

~~~text
错误：
Combolands/Localization-v0.4.0/BepInEx/...

正确：
Combolands/BepInEx/...
~~~

4. 进入游戏的 **Settings**，确认是否出现 **Language / 语言** 选项
5. 选择中文后记得点击 **Apply**

如果 Settings 完全没有 Language 选项，通常表示模组没有成功加载。

### 出现方框字、缺字或中文字体异常

模组会尝试使用 Windows 上可用的中文字体。

繁体中文会优先使用例如：

- Microsoft JhengHei UI
- Microsoft JhengHei
- Noto Sans CJK TC / Noto Sans TC

简体中文会优先使用例如：

- Microsoft YaHei UI
- Microsoft YaHei
- Noto Sans CJK SC / Noto Sans SC

一般 Windows 10／11 安装通常已有可用字体。

如果使用精简版 Windows、Wine／Proton 或自行移除过系统字体，可能需要另外安装 CJK 字体。

### 为什么有少量文字仍然是英文？

模组遇到没有可用中文翻译的项目时，会保留游戏原本的英文，而不是显示空白或错误文字。

另外，游戏更新新增文字后，也可能暂时出现尚未翻译的新内容。

### 简体中文是独立翻译吗？

目前中文翻译以人工审校的**繁体中文内容**为主要来源。

选择简体中文时，模组会在 Windows 上即时将繁体中文转换为简体中文。因此简体版本可能在个别游戏术语上与专门人工编写的简体翻译有所差异。

### 更新游戏后模组失效怎么办？

Combolands 更新后，如果游戏内部界面或程序结构发生变化，中文化模组可能需要同步更新。

请先查看 [Releases](../../releases) 是否已有新版。如果最新版本仍无法使用，可以报告问题并附上：

- Combolands 游戏版本
- 中文化模组版本
- BepInEx 版本
- 发生问题的画面／操作方式
- 如方便，附上 `BepInEx/LogOutput.log`

### 游戏启动后看不到 Settings 的 Apply／Cancel，或设置页面显示异常

本模组会在原本 Settings 画面中加入语言与文字缩放控制。

如果遇到界面超出画面、无法滚动、按钮无法操作等问题，请报告：

- 屏幕分辨率
- Windows 显示缩放比例
- 游戏窗口／全屏设置
- 模组的文字缩放比例
- 问题画面的截图

这些信息可以帮助定位不同屏幕环境下的 UI 问题。

## 兼容性

当前模组版本：

~~~text
Localization v0.4.0
~~~

当前源代码基线对应的 Combolands build：

~~~text
6000.0.66f2-f95343c07fe6-687f1dad7dbd
~~~

游戏版本更新后不保证旧版模组仍能正常使用。

另外，成功编译模组并不代表所有 Windows 游戏环境都已经完成实机验证。如果遇到问题，请优先确认使用的是最新 Release。

## 问题反馈

如果你发现：

- 翻译错误
- 漏翻文字
- 简繁用词不自然
- 文字重叠或超出界面
- 缺字／方框字
- 语言切换异常
- Settings 无法正常操作

欢迎在 GitHub Issues 反馈。

反馈时如果能附上**原文、画面截图、出现位置与操作方式**，会更容易确认问题。

## 给开发者

该公开仓库只包含中文化模组源代码、翻译文件与发布流程；不包含或重新分发 Combolands、Unity、Assembly-CSharp、Sirenix／Odin 等游戏或第三方 binaries。

普通玩家不需要自行编译，请直接使用 [Releases](../../releases) 提供的 ZIP。

## 许可证与免责声明

该项目为社区制作的非官方中文化模组，与 Combolands 官方没有隶属关系。

本模组不包含 Combolands 游戏本体，也不包含 BepInEx。
