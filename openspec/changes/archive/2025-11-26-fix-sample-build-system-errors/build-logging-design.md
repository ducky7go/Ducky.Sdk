# Ducky SDK 构建日志系统设计

## 概述

为 Ducky SDK 添加全面的构建日志记录功能，在项目的 `obj/ducky-build/` 目录中生成详细的构建日志，包含所有属性、参数、脚本输出和执行时序信息。

## 日志系统架构

### 目录结构
```
Project/
├── obj/
│   └── ducky-build/
│       ├── build-2025-11-26-19-30-45.log      # 主构建日志
│       ├── scripts/
│       │   ├── extract-localization-keys-2025-11-26-19-30-47.log
│       │   ├── update-locales-csv-2025-11-26-19-30-49.log
│       │   └── deploy-mod-2025-11-26-19-30-52.log
│       └── archive/                           # 历史日志归档
│           ├── build-2025-11-25-14-20-10.log
│           └── ...
```

### 日志文件格式

#### 主构建日志格式
```
=== Ducky SDK Build Log ===
Project: Ducky.EntranceMod.Common
Started: 2025-11-26 19:30:45 UTC
Build Configuration: Debug, netstandard2.1
SDK Version: 0.0.100039-dev

## Environment Information
- OS: Linux 6.17.8-300.fc43.x86_64
- Working Directory: /path/to/project
- MSBuild Version: 17.10.4
- DotNet Script Version: 1.4.0

## MSBuild Properties
[00:00.001] DuckovFolder: /home/user/.local/share/Steam/steamapps/common/Escape from Duckov/
[00:00.001] SteamFolder: /home/user/.local/share/Steam/
[00:00.001] AssetsDir: assets
[00:00.001] LocalizationAssetsDir: assets
[00:00.001] DeployMod: true
[00:00.001] EnableILRepack: true
[00:00.001] IsModLib: false
[00:00.001] ModName: Ducky.EntranceMod.Common
[00:00.001] Configuration: Debug
[00:00.001] TargetFramework: netstandard2.1

## Target Execution Sequence
[00:00.100] [START] ValidateProject
[00:00.101]   [START] ValidateProjectPathCSX
[00:00.102]     Command: dotnet script "validate-project-path.csx" "/path/to/project"
[00:00.105]     Exit Code: 0
[00:00.105]     Duration: 00:00.004
[00:00.106]   [SUCCESS] ValidateProjectPathCSX
[00:00.107]   [START] ValidateDuckovFolderCSX
[00:00.108]     Command: dotnet script "validate-duckov-folder.csx" "/duckov/path" "/steam/path"
[00:00.110]     Exit Code: 0
[00:00.110]     Duration: 00:00.003
[00:00.111]   [SUCCESS] ValidateDuckovFolderCSX
[00:00.112] [SUCCESS] ValidateProject

[00:01.200] [START] ProcessLocalization
[00:01.201]   [START] ExtractLocalizationKeysCSX
[00:01.202]     Script: /path/to/extract-localization-keys-enhanced.csx
[00:01.203]     Arguments: --project-dir "/path/to/project" --configuration "Debug" --target-framework "netstandard2.1" --mod-name "Ducky.EntranceMod.Common" --assets-dir "assets" --deploy-mod "true"
[00:01.205]     Stdout:
[00:01.205]       === Localization Key Extraction Results ===
[00:01.205]       Success: true
[00:01.205]       Keys Count: 4
[00:01.205]       Keys JSON: /path/to/assets/lkeys.json
[00:01.205]     Exit Code: 0
[00:01.206]     Duration: 00:00.004
[00:01.207]   [SUCCESS] ExtractLocalizationKeysCSX
[00:01.208]   [START] UpdateLocalizationCsvCSX
[00:01.209]     Command: dotnet script "update-locales-csv.csx" "/path/to/project" "/path/to/assets" "/path/to/assembly.dll"
[00:01.212]     Exit Code: 0
[00:01.212]     Duration: 00:00.003
[00:01.213]   [SUCCESS] UpdateLocalizationCsvCSX
[00:01.214] [SUCCESS] ProcessLocalization

## Build Summary
Total Duration: 00:00:45.123
Targets Executed: 12
Scripts Executed: 8
Files Generated:
  - obj/Debug/netstandard2.1/Ducky.EntranceMod.Common.dll
  - assets/lkeys.json
  - assets/Locales/en.csv
  - assets/Locales/zh.csv
  - assets/preview.png
  - assets/info.ini

Status: SUCCESS
Completed: 2025-11-26 19:31:30 UTC
```

### 单个脚本日志格式
```
=== Script Execution Log: extract-localization-keys-enhanced.csx ===
Project: Ducky.EntranceMod.Common
Started: 2025-11-26 19:30:47 UTC
Working Directory: /path/to/project

## Command Line
dotnet script "/home/user/.nuget/packages/ducky.sdk/0.0.100039-dev/build/../scripts/extract-localization-keys-enhanced.csx" --sdk-scripts-path "/home/user/.nuget/packages/ducky.sdk/0.0.100039-dev/build/../scripts" --project-dir "/path/to/project" --configuration "Debug" --target-framework "netstandard2.1" --mod-name "Ducky.EntranceMod.Common" --duckov-folder "/home/user/.local/share/Steam/steamapps/common/Escape from Duckov/" --steam-folder "/home/user/.local/share/Steam/" --assets-dir "assets" --localization-assets-dir "assets" --enable-ilrepack true --enable-global-using true --include-harmony false --deploy-mod true --exclude-sdk-lib false --is-mod-lib false

## Arguments Parsed
sdk-scripts-path: /home/user/.nuget/packages/ducky.sdk/0.0.100039-dev/build/../scripts
project-dir: /path/to/project
configuration: Debug
target-framework: netstandard2.1
mod-name: Ducky.EntranceMod.Common
assets-dir: assets
deploy-mod: true

## Standard Output
[ExtractLocalizationKeysCSX] Starting localization key extraction
[ExtractLocalizationKeysCSX] Extracting localization keys from generated source files
[ExtractLocalizationKeysCSX] Found localization keys in generated source files
[ExtractLocalizationKeysCSX] Found 4 localization keys
[ExtractLocalizationKeysCSX] Keys saved to: /path/to/assets/lkeys.json

## Standard Error
(none)

## Exit Code
0

## Files Modified
- /path/to/assets/lkeys.json (created)

Duration: 00:00:00.123
Completed: 2025-11-26 19:30:47 UTC
```

## 实现组件

### 1. BuildLogger 工具类
```csharp
public class BuildLogger
{
    private readonly string _logDirectory;
    private readonly string _projectName;
    private readonly TextWriter _mainLogWriter;
    private readonly List<ScriptExecution> _scriptExecutions = new();

    public void LogProperty(string name, string value);
    public void LogTargetStart(string targetName);
    public void LogTargetEnd(string targetName, TimeSpan duration);
    public void LogScriptExecution(ScriptExecution execution);
    public void WriteBuildSummary(BuildSummary summary);
}
```

### 2. ScriptExecution 记录类
```csharp
public class ScriptExecution
{
    public string ScriptName { get; set; }
    public string CommandLine { get; set; }
    public Dictionary<string, string> Arguments { get; set; }
    public string Stdout { get; set; }
    public string Stderr { get; set; }
    public int ExitCode { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public List<string> FilesModified { get; set; } = new();
}
```

### 3. MSBuild 目标集成
```xml
<!-- 在 Ducky.Sdk.Orchestration.targets 中添加日志记录 -->
<Target Name="InitializeBuildLogging" BeforeTargets="ValidateProject">
    <Exec Command="dotnet script &quot;$(_InitializeLoggingScript)&quot; $(_ScriptArgs)"
          WorkingDirectory="$(MSBuildProjectDirectory)"
          ContinueOnError="true">
        <Output TaskParameter="ExitCode" PropertyName="_InitializeLoggingExitCode" />
    </Exec>
</Target>

<Target Name="LogTargetExecution" BeforeTargets="ValidateProject;ResolveBuildProperties;GenerateAssets;ProcessLocalization;PackageAndDeploy">
    <Message Text="$(MSBuildThisFileDirectory)../scripts/log-target-execution.csx &quot;$(TargetName)&quot; &quot;$(MSBuildProjectDirectory)&quot;" />
    <Exec Command="dotnet script &quot;$(MSBuildThisFileDirectory)../scripts/log-target-execution.csx&quot; &quot;$(TargetName)&quot; &quot;$(MSBuildProjectDirectory)&quot;"
          ContinueOnError="true"
          WorkingDirectory="$(MSBuildProjectDirectory)" />
</Target>
```

### 4. CSX 脚本增强
- `log-target-execution.csx` - 记录目标执行时序
- `initialize-build-logging.csx` - 初始化日志系统
- `capture-script-output.csx` - 捕获脚本输出的增强版本

## 配置选项

### 构建属性
```xml
<PropertyGroup>
    <!-- 控制日志详细程度 -->
    <DuckyBuildLogLevel Condition="'$(DuckyBuildLogLevel)' == ''">Normal</DuckyBuildLogLevel>
    <!-- Options: Minimal, Normal, Detailed, Diagnostic -->

    <!-- 控制日志保留数量 -->
    <DuckyBuildLogRetention Condition="'$(DuckyBuildLogRetention)' == ''">10</DuckyBuildLogRetention>

    <!-- 控制是否启用脚本日志分离 -->
    <DuckyEnableSeparateScriptLogs Condition="'$(DuckyEnableSeparateScriptLogs)' == ''">true</DuckyEnableSeparateScriptLogs>

    <!-- 控制最大日志文件大小 (MB) -->
    <DuckyMaxLogFileSize Condition="'$(DuckyMaxLogFileSize)' == ''">50</DuckyMaxLogFileSize>
</PropertyGroup>
```

## 使用场景

### 场景 1: 调试参数传递问题
开发者可以查看日志文件中的"Arguments Parsed"部分，确认：
- 脚本是否收到了正确的参数
- 参数值是否被正确解析
- 参数顺序是否正确

### 场景 2: 分析构建性能
通过时序信息分析：
- 哪个目标耗时最长
- 脚本执行时间是否正常
- 构建是否有性能瓶颈

### 场景 3: 诊断脚本失败
通过详细的脚本日志：
- 查看完整的命令行
- 分析 stdout/stderr 输出
- 了解脚本执行环境

### 场景 4: 验证文件生成
通过文件列表确认：
- 哪些文件被创建/修改
- 文件路径是否正确
- 是否有预期的文件缺失

## 日志轮转和管理

### 自动清理
- 保留最近的 N 个构建日志（配置：`DuckyBuildLogRetention`）
- 超过大小限制的日志文件自动归档
- 压缩旧的日志文件以节省空间

### 日志分析工具
```bash
# 查看最近的构建日志
cat obj/ducky-build/build-*.log | tail -50

# 搜索特定脚本的执行记录
grep "ExtractLocalizationKeysCSX" obj/ducky-build/build-*.log

# 分析构建性能
grep "Duration:" obj/ducky-build/build-*.log | sort -k2 -n

# 查看所有错误
grep -E "(ERROR|Exit Code: [1-9])" obj/ducky-build/build-*.log
```

## 性能考虑

### 最小化性能影响
- 异步日志写入
- 缓冲日志输出
- 可配置的日志级别
- 避免重复日志记录

### 内存管理
- 限制内存中的日志缓存大小
- 及时刷新大型日志条目
- 合理的字符串构建策略

这个全面的日志系统将极大提升 Ducky SDK 的可调试性和开发者体验。