# Ducky.Sdk

**中文 | [English](README_en.md)**

一个用于为"Escape from Duckov"游戏开发 Mod 的综合性 .NET SDK。

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/download)
[![NuGet](https://img.shields.io/nuget/v/Ducky.Sdk.svg)](https://www.nuget.org/packages/Ducky.Sdk/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Ducky.Sdk.svg)](https://www.nuget.org/packages/Ducky.Sdk/)
[![GitHub Stars](https://img.shields.io/github/stars/ducky7go/Ducky.Sdk?style=social)](https://github.com/ducky7go/Ducky.Sdk/stargazers)
[![GitHub Issues](https://img.shields.io/github/issues/ducky7go/Ducky.Sdk)](https://github.com/ducky7go/Ducky.Sdk/issues)
[![GitHub Pull Requests](https://img.shields.io/github/issues-pr/ducky7go/Ducky.Sdk)](https://github.com/ducky7go/Ducky.Sdk/pulls)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/ducky7go/Ducky.Sdk/pulls)

## 功能特性

- 🚀 **自动化构建管道** - 构建时自动部署到游戏目录
- 🌍 **智能本地化** - 基于源生成器的本地化系统，支持 CSV/文件翻译
- 📦 **单 DLL 分发** - 通过 ILRepack 自动合并程序集，无冲突部署
- 🔧 **Harmony 集成** - 可选的运行时补丁支持，无缝依赖管理
- 🎨 **自动生成资源** - 自动生成 Mod 元数据和预览图
- 📝 **强类型开发** - 完整的 IntelliSense 支持和编译时验证
- 🔄 **源码分发** - SDK 以源代码形式分发，避免版本冲突

## 快速开始

### 前置要求

- .NET SDK 10.0 或更高版本（构建运行器需要 .NET 10 运行时，SDK 10 自带；无需安装 `dotnet-script` 或 `ilrepack`）
- 已安装"Escape from Duckov"游戏
- Steam 安装目录（用于自动部署）

### 安装

1. **在项目根目录创建 `Local.props`**（git 忽略）：

```xml
<Project>
  <PropertyGroup>
    <SteamFolder>/path/to/your/steam/</SteamFolder>
  </PropertyGroup>
</Project>
```

2. **配置你的 Mod 项目：**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <ModName>MyAwesomeMod</ModName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Ducky.Sdk" Version="*">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

### CI/CD 环境配置

在持续集成环境（如 GitHub Actions、Azure Pipelines）中，无需安装 Steam 即可构建 Mod。通过环境变量直接指定关键路径：

```yaml
# GitHub Actions 示例
env:
  ManagedDirectory: /opt/game-assemblies/Managed/
  ModsDirectory: /tmp/mod-output/
```

或通过 MSBuild 参数传递：

```bash
dotnet build -p:ManagedDirectory=/path/to/managed/ -p:ModsDirectory=/path/to/mods/
```

**CI 构建优势：**
- 无需 Steam 安装
- 无需游戏文件
- 灵活的输出路径配置
- 自动跳过 `SteamFolder` 验证（检测 `CI` 环境变量）

### 你的第一个 Mod

创建 `ModBehaviour.cs` 文件：

```csharp
using Ducky.Sdk.Logging;
using Ducky.Sdk.ModBehaviours;

namespace MyAwesomeMod;

public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        Log.Info("我的超棒 Mod 已启用！");
        // 在这里编写初始化代码
    }

    protected override void ModDisabled()
    {
        Log.Info("我的超棒 Mod 已禁用！");
        // 在这里编写清理代码
    }
}
```

构建并运行：

```bash
dotnet build
# 你的 Mod 会自动部署到游戏目录！
```

## 包发布渠道

Ducky.Sdk 通过 NuGet.org 发布稳定版本：

### NuGet.org (稳定版)
- **主要发布渠道** - 面向生产环境使用
- **官方稳定版本** - 经过完整测试的正式版本
- **访问地址**: [https://www.nuget.org/packages/Ducky.Sdk/](https://www.nuget.org/packages/Ducky.Sdk/)

### 推荐使用方式
- **生产环境**: 使用 NuGet.org 稳定版本
- **体验测试**: 在项目中指定预发布版本：`<PackageReference Include="Ducky.Sdk" Version="*" />`

## 核心概念

### ModBehaviour 模式

每个 Mod 都继承 `ModBehaviourBase`，它提供：

- **生命周期钩子**：`ModEnabled()` 和 `ModDisabled()`
- **自动初始化**：日志记录、本地化和 buff 注册
- **Mod 身份**：访问 Mod 名称、ID 和 Steam 创意工坊元数据

```csharp
public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        // 当 Mod 被加载时调用
        // 初始化系统、注册事件、应用补丁
    }

    protected override void ModDisabled()
    {
        // 当 Mod 被卸载时调用
        // 清理资源、移除补丁、注销事件
    }
}
```

### 本地化系统

SDK 使用独特的双类模式实现类型安全的本地化：

#### 1. 定义键（LK.cs）

```csharp
using Ducky.Sdk.Attributes;

[LanguageSupport("en", "zh", "fr")]
public static class LK
{
    public static class UI
    {
        public const string Welcome = "欢迎使用我的 Mod！";
        public const string Settings = "设置";
        
        [TranslateFile("md")]
        public const string Documentation = "文档";
    }
}
```

#### 2. 使用生成的属性（自动生成的 L.cs）

```csharp
using Ducky.Sdk.Localizations;

// SDK 会生成一个匹配的 L 类及其属性
Log.Info(L.UI.Welcome);        // 返回本地化字符串
Log.Info(L.UI.Documentation);  // 返回 Documentation.md 的内容
```

#### 3. 管理翻译

翻译存储在 `assets/Locales/`：

```
assets/
  Locales/
    en.csv         # 英文翻译
    zh.csv         # 中文翻译
    en/
      Documentation.md  # 英文长文本内容
    zh/
      Documentation.md  # 中文长文本内容
```

CSV 格式：

```csv
Key,Value
欢迎使用我的 Mod！,欢迎使用我的 Mod！
设置,设置
文档,Documentation.md
```

#### 4. 多目录本地化支持

通过 `LocalizationAssetsDir` 属性，您可以指定多个本地化资源目录：

```xml
<PropertyGroup>
  <LocalizationAssetsDir>assets/Locales;shared-locales;external-translations</LocalizationAssetsDir>
</PropertyGroup>
```

当指定多个目录时：
- 每个目录都应包含相同结构的 CSV 文件
- `UpdateLocalesCsv` target 会为所有目录生成和验证翻译文件
- 其他资源文件仍使用 `AssetsDir` 指定的目录

#### 5. 运行时 API、文件标记、语言回退与校验

完整说明见 [本地化文档](docs/localization.md)；从自研本地化方案迁移见 [DOD 迁移指南](docs/localization-dod-migration.md)。

- 运行时：`L.Get`、`L.TryGet`、`L.Dynamic`（格式化键模板，如 `"worldeffect.{0}.name"`）；`L.RegisterLocales(modId, dir)` 注册子 Mod 的 `Locales` 目录，`L.Scope(modId)` 只查询该 Mod 的来源。
- 动态键：在 `LK` 上声明 `[KeyPattern("worldeffect.{code}.{name|summary|description}", Group = "WorldEffect")]`，生成 `L.WorldEffect.Name(code)` 等强类型访问器。
- 文件引用：CSV 值以 `@file:` 开头时才表示文件（如 `@file:Welcome.md`），其他值一律按文本处理。
- 语言代码统一为小写并使用 `-`（`zh-Hant` → `zh-hant`）；默认回退链为 `zh-hant → zh → en`，可用 `L.ConfigureFallback` 修改。
- 键不区分大小写；缺失文本返回 `[key]`，`L.TryGet` 则不返回占位文本。
- 构建校验：比较各语言与英文基线。`LocalizationValidation`（`Off` / `Warn` / `Error`，默认 `Warn`）控制缺失键和占位符不一致；`LocalizationExtraKeys`（`Ignore` / `Warn` / `Error`，默认 `Warn`）控制多余键。缺失项写入 `missing.csv`。
- 翻译命令：`dotnet tools/net10.0/Ducky.Sdk.Build.dll locales set | sync | validate ...`。

### 配置管理

存储和检索 Mod 设置：

```csharp
using Ducky.Sdk.Options;

// 每个 Mod 的配置（隔离存储）
ModOptions.ForThis.Set("volume", 0.8);
var volume = ModOptions.ForThis.Get("volume", 1.0);

// 共享配置（跨所有 Mod）
ModOptions.ForAllMods.Set("globalSetting", "value");
```

### 日志记录

使用 LibLog 进行结构化日志记录：

```csharp
using Ducky.Sdk.Logging;

Log.Info("玩家加入：{PlayerName}", playerName);
Log.Warn("生命值过低：{Health}", health);
Log.Error(exception, "加载资源失败：{ResourceId}", resourceId);
```

## 共享运行时 API

### ModInfo：当前 Mod 的身份

```csharp
using Ducky.Sdk.Utils;

string name = ModInfo.Name;              // Mod 名称
string id = ModInfo.Id;                  // "steam.<数字>" 或 "local.<文件夹名>"
string? folder = ModInfo.Folder;         // Mod 所在目录的完整路径，未知时为 null
string? assets = ModInfo.AssetsPath;     // Folder 下的 assets 路径，Folder 为 null 时为 null
bool core = ModInfo.IsHotReloadCore;     // 是否为热重载 Core（<ModName>.Core.<时间戳>）
```

热重载 Core 没有文件路径，`Folder` 会取同一 Mod 已加载的 shim 程序集所在目录。
注意：默认构建流程把 `AssetsDir` 的**内容**复制到 Mod 目录根部，不会创建 `assets` 子目录；`AssetsPath` 按规范返回 `Folder/assets`，使用前请确认目录确实存在。

### MainThread：从任意线程回到 Unity 主线程

```csharp
using Ducky.Sdk.Threading;

MainThread.Post(() => DoSomethingOnMainThread());          // 排队，之后的帧在主线程执行

await MainThread.InvokeAsync(() => GameObject.Find("x"),   // 返回 Task<T>，可取结果或异常
                             cancellationToken,
                             TimeSpan.FromSeconds(2));     // 超时参数可选
```

- 工作**从不内联执行**，即使在主线程调用也一样；`Post` 不阻塞调用方。
- 超时前工作未开始：任务以 `TimeoutException` 失败，工作永远不会执行。工作已开始：任务返回工作自身的结果或异常。
- 取消在工作开始前生效：任务被取消，工作不执行。
- `ModBehaviourBase` 在启用时自动启动主线程调度器，并在最后一个启用的子类被禁用时关闭它（待处理的 `InvokeAsync` 被取消，`Post` 被丢弃）。
- 不继承 `ModBehaviourBase` 的 Mod 必须在主线程上先调用 `MainThread.EnsureStarted()`，否则投递的工作会一直等待。

### Subscriptions：随禁用自动释放的事件订阅

```csharp
using Ducky.Sdk.GameEvents;

protected override void ModEnabled()
{
    Subscriptions.Add(LevelEvents.OnInitialized(() => OnLevelReady()));
    Subscriptions.Add(SceneEvents.OnPhaseChanged((previous, next) => OnPhase(next)));
    Subscriptions.Add(() => SomeOtherEvent -= handler);     // 任意 Action 也可以
}
```

- 在 `ModDisabled()` 之后按加入的逆序释放；`ModDisabled()` 抛出异常时也会释放，原异常照常抛出。热重载时同样释放旧 Core 的订阅。
- 禁用期间加入的项会被立即释放，不会保留到下一次启用。
- 单个订阅释放失败只记录日志，不影响其余订阅。
- `ScenePhases.Of(sceneName)` 把场景名分为 `Base`（以 `Base` 开头）、`Raid`（以 `Level` 开头）和 `Other`，区分大小写。

**清理清单**：经 `Subscriptions` 添加的事件处理器不需要在 `ModDisabled` 里 `-=`。下列内容仍需要在 `ModDisabled` 里显式清理：Harmony 补丁、`GameObject`、Buff 注册、UI。

### 分析器诊断 DUCKY006 / DUCKY007

| ID | 含义 | 处理方式 |
| --- | --- | --- |
| `DUCKY006` | `Task.Run` 或 `UniTask.RunOnThreadPool` 的委托中使用了 Unity API | 先 `await UniTask.SwitchToMainThread()`，或改用 `MainThread.InvokeAsync` |
| `DUCKY007` | 静态构造函数或静态字段/属性初始化器中访问了 Unity 对象 | 改为在主线程上惰性创建的实例成员 |

两者都是默认启用的警告（不会导致构建失败），生成的代码不分析。已知限制：不跟踪辅助方法和方法组中的 Unity 调用。`Debug`、`Mathf` 和结构体（如 `Vector3`）不会被标记；`Debug.Log*` 由 `DUCKY001`–`DUCKY005` 处理。

### 子弹数 API

`ItemSetting_Gun` 上提供 `TryGetBulletCount(out int)` 与 `SetBulletCount(int)`，读写游戏内部的 `bulletCount` 成员（私有实例属性）。找不到该成员时只记录一次日志，`SetBulletCount` 不做任何事并返回原对象，可以链式调用。

### 游戏机制参考

[docs/game-mechanics.md](docs/game-mechanics.md) 记录游戏的实际行为（Buff 抗性、敌人出生时机、`Health.Hurt` 伤害顺序、爆炸、天气与时钟、翻滚与耐力、开箱，以及第二批的治疗与进食、AI 控制与隐身、经济、手雷与地雷、挡弹墙、发现玩家、烧伤与眩晕、超重与翻滚、拾取、声音和爆炸目标），并附每条结论的证据等级，同时说明基于它们实现的辅助类（`BuffImmunity`、`StatBuff`、`PlayerCombat`、`HurtRules`、`RestoreRules`、`CharacterRestore`、`Economy`、`Grenades`、`BulletBlockers`、`EnemyNoticed`、`LootEvents`、`AiSounds`、`CharacterControlRules`、`CharacterVisibility`、`MouseAimControl`、`Explosions`、`LethalDamageGuard`、`EnemyReady` 等）。基于 Harmony 的辅助类需要把 `IncludeHarmony` 设为 true。开发期热重载见 [docs/hot-reload.md](docs/hot-reload.md)。

## 高级功能

### MessageHub 跨 Mod 通信

SDK 集成了 MessageHub 功能，允许 mod 之间进行通信，无需安装额外的 host mod。

#### 自动 Host 功能

```csharp
public class MyModBehaviour : ModBehaviourBase
{
    // 默认启用，可以选择禁用
    protected override bool EnableMessageHubHost { get; set; } = true;

    protected override void ModEnabled()
    {
        // 检查此 mod 是否是 host
        if (IsMessageHubHost)
        {
            Log.Info("此 Mod 是 MessageHub Host!");
        }

        // 注册为消息接收方
        var proxy = ModHttpV1Proxy.CreateFromSingleton();
        proxy.RegisterClient("MyMod", async (fromMod, contentType, body) =>
        {
            Log.Info($"收到来自 {fromMod} 的消息: {body}");
            // 处理消息...
        });
    }
}
```

#### 发送消息到其他 Mod

```csharp
var proxy = ModHttpV1Proxy.CreateFromSingleton();
await proxy.Notify("MyMod", "TargetModName", "custom_event", "Hello World!");
```

#### 特性
- ✅ **无需额外安装** - 自动集成在 SDK 中
- ✅ **自动 Host 检测** - 第一个启动的 mod 成为 host
- ✅ **向后兼容** - 与现有代码完全兼容
- ✅ **生命周期管理** - Host 会持续运行，即使创建它的 mod 已禁用

### Harmony 运行时补丁

启用运行时方法补丁以实现高级 Mod：

```xml
<PropertyGroup>
  <ModName>MyAwesomeMod</ModName>
  <IncludeHarmony>true</IncludeHarmony>
</PropertyGroup>
```

使用 Harmony 补丁：

```csharp
using HarmonyLib;

public class ModBehaviour : ModBehaviourBase
{
    private Harmony _harmony;
    
    protected override void ModEnabled()
    {
        _harmony = new Harmony("com.myname.mymod");
        _harmony.PatchAll();
    }
    
    protected override void ModDisabled()
    {
        _harmony?.UnpatchAll();
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.TakeDamage))]
public static class Player_TakeDamage_Patch
{
    static void Prefix(Player __instance, ref float damage)
    {
        Log.Info("玩家受到伤害：{Damage}", damage);
        damage *= 0.5f; // 伤害减少 50%
    }
}
```

### 程序集合并

默认情况下，SDK 会将所有依赖项合并到单个 DLL：

```xml
<PropertyGroup>
  <!-- 默认值：true（单 DLL 分发）-->
  <EnableILRepack>true</EnableILRepack>
  
  <!-- 禁用合并（依赖项复制到 Dependency/ 文件夹）-->
  <EnableILRepack>false</EnableILRepack>
</PropertyGroup>
```

优点：
- ✅ 单文件分发
- ✅ Mod 之间无版本冲突
- ✅ 内部化依赖项（无命名空间污染）
- ✅ 更小的部署占用空间

### 多项目 Mod

将复杂的 Mod 组织成多个项目：

**共享库项目**（`MyMod.Common.csproj`）：

```xml
<PropertyGroup>
  <IsModLib>true</IsModLib>
  <AssetsDir>$(SolutionDir)/MyMod/assets</AssetsDir>
</PropertyGroup>
```

**入口项目**（`MyMod.csproj`）：

```xml
<PropertyGroup>
  <ModName>MyMod</ModName>
  <ExcludeSdkLib>true</ExcludeSdkLib>
</PropertyGroup>

<ItemGroup>
  <ProjectReference Include="../MyMod.Common/MyMod.Common.csproj" />
</ItemGroup>
```

#### IsModLib 行为差异

**共享库项目**（`IsModLib=true`）会跳过以下自动化任务：
- ❌ 预览图片（`preview.png`）生成
- ❌ Mod 元数据（`info.ini`）生成
- ❌ 自动部署到游戏目录
- ❌ ILRepack 程序集合并
- ❌ 依赖文件复制

**保留的功能**：
- ✅ 本地化处理（字符串提取和 CSV 更新）
- ✅ 编译和程序集生成

**最终发布 Mod 项目**（`IsModLib=false` 或未设置）会执行所有自动化任务。

#### 项目属性说明

- **`IsModLib=true`**: 标记项目为共享库，跳过非必要的自动化任务
- **`ExcludeSdkLib=true`**: 排除 SDK 源代码编译（用于入口项目，避免重复编译）

### 自动生成资源

SDK 会自动生成：

1. **info.ini** - 基本 Mod 元数据（名称、显示名称、描述）
2. **preview.png** - 256x256 预览图：按 Mod 名称的 SHA-256 哈希从 10 张内置背景中选一张，并在其上绘制项目名缩写（如 `Ducky.EntranceMod` → `DEM`）。已存在的 `preview.png` 不会被覆盖；库项目（`IsModLib`）不生成。背景由 ComfyUI 一次性生成并随 SDK 提交，维护者可运行 `scripts/generate_preview_backgrounds.sh` 重新生成；普通 Mod 构建不调用 ComfyUI。
3. **publishedFileId** - Steam 创意工坊 ID 同步

开发期间禁用自动部署：

```xml
<PropertyGroup>
  <DeployMod>false</DeployMod>
</PropertyGroup>
```

## 项目结构

```
MyMod/
├── MyMod.csproj           # 主 Mod 项目
├── ModBehaviour.cs        # Mod 入口点
├── LK.cs                  # 本地化键
├── Local.props            # Git 忽略的本地配置
└── assets/
    ├── info.ini           # Mod 元数据
    ├── preview.png        # Mod 预览图（背景 + 缩写，自动生成）
    ├── description.md     # 详细描述
    └── Locales/
        ├── en.csv         # 英文翻译
        ├── zh.csv         # 中文翻译
        └── ...
```

## SDK 开发

### 本地构建 SDK

1. **打包到本地源：**

```bash
./scripts/packToLocal.sh --version 0.0.1
```

2. **使用新 SDK 重建示例：**

```bash
./scripts/rebuild_samples.sh
```

3. **获取游戏依赖：**

```bash
./scripts/fetch_build_dependency.sh
```

### 测试更改

`Samples/` 目录包含集成测试项目：

- **Ducky.SingleProject** - 单项目 Mod 模板
- **Ducky.EntranceMod** - 带共享库的多项目 Mod
- **Ducky.TryHarmony** - Harmony 补丁示例

运行 `./scripts/rebuild_samples.sh` 验证端到端 SDK 工作流程。

### 仓库结构

```
Ducky.Sdk/
├── Sdk/                           # SDK 开发工作区
│   ├── SDKlibs/
│   │   ├── Ducky.Sdk/            # 核心 NuGet 包
│   │   │   ├── Ducky.Sdk.nuspec  # 包清单
│   │   │   ├── Ducky.Sdk.props   # MSBuild 属性
│   │   │   ├── Ducky.Sdk.targets # 构建目标
│   │   │   └── Ducky.Sdk.Orchestration.targets # 调用构建运行器
│   │   └── Ducky.Sdk.Lib/        # 共享库（以源码分发）
│   ├── Ducky.Sdk.Analyser/       # Roslyn 源生成器
│   ├── Ducky.Sdk.Build/          # 构建运行器（随包发布到 tools/net10.0/）
│   └── Tests/                     # 单元测试
├── Samples/                       # 示例 Mod 项目
│   ├── Ducky.SingleProject/
│   ├── Ducky.EntranceMod/
│   └── Ducky.TryHarmony/
├── scripts/                       # 构建自动化
│   ├── packToLocal.sh            # 打包 SDK 到本地源
│   ├── rebuild_samples.sh        # 使用新 SDK 重建示例
│   └── fetch_build_dependency.sh # 下载游戏程序集
└── duckylocal/                   # 本地 NuGet 源
```

## 配置参考

### MSBuild 属性

| 属性 | 默认值 | 描述 |
|------|--------|------|
| `ModName` | (必需) | Mod 标识符和输出 DLL 名称 |
| `SteamFolder` | - | Steam 安装路径 |
| `DuckovFolder` | 计算得出 | 游戏目录（从 SteamFolder 自动计算）|
| `ManagedDirectory` | 计算得出 | 游戏托管程序集目录（可显式设置用于 CI 环境）|
| `ModsDirectory` | 计算得出 | Mod 部署目标目录（可显式设置用于 CI 环境）|
| `DeployMod` | `true` | 启用自动部署到游戏 |
| `EnableILRepack` | `true` | 将程序集合并到单个 DLL |
| `IncludeHarmony` | `false` | 包含 Harmony 用于运行时补丁 |
| `AssetsDir` | `assets/` | 自定义资源目录路径（用于一般资源）|
| `LocalizationAssetsDir` | `$(AssetsDir)` | 本地化资源目录路径（用于 UpdateLocalesCsv target）|
| `ExcludeSdkLib` | `true` | 排除 SDK 源代码编译（用于入口项目）|
| `IsModLib` | `false` | 将项目标记为共享库（跳过图片生成、部署、ILRepack 等自动化任务，仅保留本地化处理）|

#### AssetsDir vs LocalizationAssetsDir

`AssetsDir` 和 `LocalizationAssetsDir` 有不同的用途：

- **`AssetsDir`**: 用于一般资源（如 `info.ini`、`preview.png` 等），期望为单一路径
- **`LocalizationAssetsDir`**: 专用于本地化资源，支持多个目录（用分号分隔）

**使用场景：**

```xml
<!-- 使用默认设置（两者相同） -->
<PropertyGroup>
  <AssetsDir>assets</AssetsDir>
  <!-- LocalizationAssetsDir 会自动使用 AssetsDir 的值 -->
</PropertyGroup>

<!-- 分别设置 -->
<PropertyGroup>
  <AssetsDir>assets</AssetsDir>
  <LocalizationAssetsDir>locales;shared-locales</LocalizationAssetsDir>
</PropertyGroup>
```

### 本地化属性

#### `[LanguageSupport(...)]`

指定支持的语言：

```csharp
[LanguageSupport("en", "zh", "fr", "de", "ja")]
public static class LK { ... }
```

使用 "all" 生成所有支持语言的文件：

```csharp
[LanguageSupport("all")]
public static class LK { ... }
```

使用 "all" 时支持的语言：de, en, es, fr, ja, ko, pt, ru, zh-hant, zh

混合使用 "all" 和额外语言：

```csharp
[LanguageSupport("all", "custom-lang")]
public static class LK { ... }
```

#### `[TranslateFile]` 或 `[TranslateFile("ext")]`

在外部文件中存储翻译：

```csharp
[TranslateFile]           // 使用 .txt 扩展名
public const string Help = "帮助文本";

[TranslateFile("md")]     // 使用 .md 扩展名
public const string ReadMe = "说明内容";
```

## 故障排除

### "SteamDir property must be set"

**本地开发：** 在项目根目录创建 `Local.props`，填写 Steam 安装路径：

```xml
<Project>
  <PropertyGroup>
    <SteamFolder>/path/to/steam/</SteamFolder>
  </PropertyGroup>
</Project>
```

**CI 环境：** 设置 `ManagedDirectory` 和 `ModsDirectory` 环境变量：

```yaml
env:
  ManagedDirectory: /path/to/managed/
  ModsDirectory: /path/to/mods/
```

或通过 MSBuild 参数：

```bash
dotnet build -p:ManagedDirectory=/path/to/managed/ -p:ModsDirectory=/path/to/mods/
```

### NuGet 缓存过期

清除所有缓存并重建：

```bash
./scripts/rebuild_samples.sh --clear-all-caches
```

或手动清除：

```bash
dotnet nuget locals all --clear
rm -rf ~/.nuget/packages/ducky.sdk/
```

### 缺少游戏程序集

下载所需的游戏 DLL：

```bash
./scripts/fetch_build_dependency.sh
```

### Mod 未部署

1. 验证 `$(DuckovFolder)` 路径存在
2. 检查 `$(DeployMod)` 是否设置为 `false`
3. 确保对游戏目录有写入权限

### CSV 中缺少本地化键

SDK 会验证 CSV 文件包含所有键。运行：

```bash
dotnet build
```

检查构建输出中的验证错误。

## 示例

查看 `Samples/` 目录获取完整示例：

- **[Ducky.SingleProject](Samples/Ducky.SingleProject/)** - 最小单文件 Mod
- **[Ducky.EntranceMod](Samples/Ducky.EntranceMod/)** - 带本地化的多项目 Mod
- **[Ducky.TryHarmony](Samples/Ducky.TryHarmony/)** - 使用 Harmony 的运行时补丁

## 贡献

欢迎贡献！请：

1. Fork 仓库
2. 创建功能分支
3. 进行更改
4. 使用 `./scripts/rebuild_samples.sh` 测试
5. 提交 Pull Request

## 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件。

## 致谢

- **游戏**："Escape from Duckov" by TeamSoda
- **Harmony**：[Harmony Library](https://github.com/pardeike/Harmony)
- **ILRepack**：[ILRepack.Lib](https://github.com/gluck/il-repack)（随构建运行器在进程内调用）

## 支持

- 🐛 [报告问题](https://github.com/ducky7go/Ducky.Sdk/issues)
- 💬 [讨论](https://github.com/ducky7go/Ducky.Sdk/discussions)
- 🌐 [HagiCode](https://www.hagicode.com) - 了解同一团队制作的其他产品

---

用 ❤️ 为 Escape from Duckov Mod 社区打造
