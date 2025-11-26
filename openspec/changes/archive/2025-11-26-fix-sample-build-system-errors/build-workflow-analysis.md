# Ducky SDK 构建流程分析

## 当前问题流程图

```mermaid
graph TD
    A[开始构建] --> B[项目编译]
    B --> C[验证脚本执行失败]
    C --> D[UpdateLocalizationCsvCSX]
    D --> E{查找 lkeys.json}
    E -->|文件不存在| F[构建失败 ❌]
    E -->|文件存在| G[ExtractLocalizationKeysCSX - 运行太晚]
    G --> H[生成预览失败]

    style C fill:#ff6b6b,color:#000000
    style F fill:#ff6b6b,color:#000000
    style H fill:#ff6b6b,color:#000000
```

## 正确的预期流程图

```mermaid
graph TD
    A[开始构建] --> B[项目编译]
    B --> C[LK.cs 编译到程序集]
    C --> D[ExtractLocalizationKeysCSX]
    D --> E[从程序集提取语言键]
    E --> F[生成 lkeys.json]
    F --> G[UpdateLocalizationCsvCSX]
    G --> H{使用 lkeys.json}
    H -->|成功| I[更新 CSV 文件]
    I --> J[CopyLocalizationAssetsCSX]
    J --> K[复制到正确位置]
    K --> L[生成预览]
    L --> M[构建成功 ✅]

    style D fill:#51cf66,color:#000000
    style F fill:#51cf66,color:#000000
    style I fill:#51cf66,color:#000000
    style M fill:#51cf66,color:#000000
```

## 关键问题详细分析

### 1. 构建目标执行顺序问题

**当前状态 (错误):**
```xml
<!-- 在 Orchestration.targets 中 -->
<Target Name="ProcessLocalization">
    <CallTarget Targets="UpdateLocalizationCsvCSX" />  <!-- ❌ 先运行 -->
    <CallTarget Targets="ExtractLocalizationKeysCSX" />  <!-- ❌ 后运行 -->
</Target>
```

**正确状态:**
```xml
<Target Name="ProcessLocalization">
    <CallTarget Targets="ExtractLocalizationKeysCSX" />  <!-- ✅ 先运行 -->
    <CallTarget Targets="UpdateLocalizationCsvCSX" />  <!-- ✅ 后运行 -->
</Target>
```

### 2. 脚本参数传递问题

**问题参数流:**
```mermaid
graph LR
    A[MSBuild 属性] --> B[PrepareScriptArguments 目标]
    B --> C[_ScriptArgs 字符串]
    C --> D[CSX 脚本执行]

    E[LocalizationAssetsDir] --> F[参数为空/错误]
    G[AssetsDir] --> H[路径解析问题]

    style F fill:#ff6b6b,color:#000000
    style H fill:#ff6b6b,color:#000000
```

**正确参数流:**
```mermaid
graph LR
    A[MSBuild 属性] --> B[PrepareScriptArguments 目标]
    B --> C[正确解析参数]
    C --> D[_ScriptArgs 字符串]
    D --> E[CSX 脚本接收]
    E --> F[BuildContext 解析]
    F --> G[成功执行]

    style C fill:#51cf66,color:#000000
    style G fill:#51cf66,color:#000000
```

### 3. 验证脚本上下文问题

**手动执行 vs MSBuild 执行差异:**

| 方面 | 手动执行 | MSBuild 执行 | 问题 |
|------|----------|--------------|------|
| 工作目录 | 项目根目录 | 可能不同目录 | 路径解析失败 |
| 参数传递 | 直接指定 | 通过 MSBuild 属性 | 参数丢失/错误 |
| 环境变量 | 用户环境 | 构建环境 | 依赖缺失 |
| 错误处理 | 可见输出 | 可能被截断 | 调试困难 |

## 修复方案流程图

```mermaid
graph TD
    A[开始修复] --> B[分析构建目标依赖]
    B --> C[修复执行顺序]
    C --> D[修复参数传递]
    D --> E[修复验证脚本]
    E --> F[清理目录结构]
    F --> G[测试修复效果]
    G --> H{构建成功?}
    H -->|是| I[归档更改 ✅]
    H -->|否| J[继续调试]
    J --> B

    style C fill:#74c0fc,color:#000000
    style D fill:#74c0fc,color:#000000
    style E fill:#74c0fc,color:#000000
    style I fill:#51cf66,color:#000000
```

## LK.cs 到 lkeys.json 转换流程

```mermaid
graph TD
    A[LK.cs 源文件] --> B[LanguageSupport 属性解析]
    B --> C[const 字符串提取]
    C --> D[程序集编译]
    D --> E[ExtractLocalizationKeysCSX 执行]
    E --> F[反射分析程序集]
    F --> G[提取语言代码 "en", "zh"]
    G --> H[提取键值 "NiceWelcomeMessage" 等]
    H --> I[生成 lkeys.json]
    I --> J[JSON 结构:]
    J --> K["{
  namespace: 'Ducky.EntranceMod.Common'
  supportedLanguages: ['en', 'zh']
  keys: ['ducky_entrancemod.common.ui.nicewelcomemessage']
}"]

    style E fill:#51cf66,color:#000000
    style I fill:#51cf66,color:#000000
```