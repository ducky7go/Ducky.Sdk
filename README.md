# Ducky.Sdk

**中文 | [English](README_en.md)**

一个用于为"Escape from Duckov"游戏开发 Mod 的综合性 .NET SDK。

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download)
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

- .NET SDK 9.0 或更高版本
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

## 高级功能

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
</PropertyGroup>

<ItemGroup>
  <ProjectReference Include="../MyMod.Common/MyMod.Common.csproj" />
</ItemGroup>
```

### 自动生成资源

SDK 会自动生成：

1. **info.ini** - 基本 Mod 元数据（名称、显示名称、描述）
2. **preview.png** - 基于 Mod 名称的 256x256 identicon
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
    ├── preview.png        # Mod 图标
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
│   │   │   └── scripts/*.csx     # 自动化脚本
│   │   └── Ducky.Sdk.Lib/        # 共享库（以源码分发）
│   ├── Ducky.Sdk.Analyser/       # Roslyn 源生成器
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
| `DeployMod` | `true` | 启用自动部署到游戏 |
| `EnableILRepack` | `true` | 将程序集合并到单个 DLL |
| `IncludeHarmony` | `false` | 包含 Harmony 用于运行时补丁 |
| `AssetsDir` | `assets/` | 自定义资源目录路径（用于一般资源）|
| `LocalizationAssetsDir` | `$(AssetsDir)` | 本地化资源目录路径（用于 UpdateLocalesCsv target）|
| `ExcludeSdkLib` | `true` | 排除 SDK 源代码编译（用于入口项目）|
| `IsModLib` | `false` | 将项目标记为共享库 |

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

在项目根目录创建 `Local.props`，填写 Steam 安装路径：

```xml
<Project>
  <PropertyGroup>
    <SteamFolder>/path/to/steam/</SteamFolder>
  </PropertyGroup>
</Project>
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
- **ILRepack**：[dotnet-ilrepack](https://github.com/gluck/il-repack)

## 支持

- 🐛 [报告问题](https://github.com/ducky7go/Ducky.Sdk/issues)
- 💬 [讨论](https://github.com/ducky7go/Ducky.Sdk/discussions)

---

用 ❤️ 为 Escape from Duckov Mod 社区打造
