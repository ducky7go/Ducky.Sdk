# Ducky SDK 完整构建流程分析

## 系统架构概览

Ducky SDK 使用**混合 MSBuild-CSX 架构**，从纯粹的 MSBuild XML 目标演进到更易维护的 C# 驱动系统。

### 核心组件
- **Ducky.Sdk.props** - 属性初始化
- **Ducky.Sdk.targets** - 主要编排
- **Ducky.Sdk.Validation.targets** - 路径验证
- **Ducky.Sdk.Orchestration.targets** - CSX 脚本编排

## 完整构建生命周期

```mermaid
graph TD
    A[构建开始] --> B[阶段 1: 预构建]
    B --> C[阶段 2: 验证]
    C --> D[阶段 3: 属性解析]
    D --> E[阶段 4: 资源生成]
    E --> F[阶段 5: 核心编译]
    F --> G[阶段 6: 本地化处理]
    G --> H[阶段 7: 打包]
    H --> I[阶段 8: 部署]
    I --> J[阶段 9: 后处理]
    J --> K[构建完成]

    style B fill:#e3f2fd,stroke:#2196f3
    style C fill:#ffebee,stroke:#f44336
    style F fill:#e8f5e8,stroke:#4caf50
    style G fill:#fff3e0,stroke:#ff9800
    style H fill:#f3e5f5,stroke:#9c27b0
    style I fill:#e0f2f1,stroke:#009688
```

## 详细阶段分析

### 阶段 1: 预构建 (Ducky.Sdk.props)

```mermaid
graph TD
    A[导入 Ducky.Sdk.props] --> B[设置默认属性]
    B --> C[全局 Using 注入]
    C --> D[游戏依赖检测]
    D --> E[编译器配置]

    B --> B1[EnableGlobalUsing: true]
    B --> B2[EnableILRepack: true]
    B --> B3[DeployMod: true]
    B --> B4[IsModLib: false]

    C --> C1[命名空间注入]
    C1 --> C2[SDK 命名空间]
    C1 --> C3[Unity 命名空间]
    C1 --> C4[第三方命名空间]

    style B fill:#e3f2fd,color:#0d47a1
    style C fill:#e3f2fd,color:#0d47a1
```

**关键任务**:
- 初始化所有 SDK 属性和默认值
- 注入全局 using 指令
- 设置编译器和调试属性
- 动态添加游戏程序集引用

### 阶段 2: 验证 (CSX 驱动)

```mermaid
graph TD
    A[验证开始] --> B[项目路径验证]
    B --> C[Duckov 文件夹验证]
    C --> D[特殊字符检查]
    D --> E[路径长度验证]
    E --> F[写权限检查]
    F --> G{验证通过?}
    G -->|是| H[验证完成 ✅]
    G -->|否| I[构建失败 ❌]

    B --> B1[validate-project-path.csx]
    C --> C1[validate-duckov-folder.csx]

    style B fill:#ffebee,color:#b71c1c
    style C fill:#ffebee,color:#b71c1c
    style I fill:#ffebee,color:#b71c1c
    style H fill:#e8f5e8,color:#1b5e20
```

**验证脚本**:
- `validate-project-path.csx` - 检查特殊字符 (#, &, ;, |, <, >, `)
- `validate-duckov-folder.csx` - 验证游戏安装路径和必需文件

### 阶段 3: 属性解析 (resolve-sdk-properties.csx)

```mermaid
graph TD
    A[属性解析开始] --> B[路径计算]
    B --> C[项目类型检测]
    C --> D[本地化配置]
    D --> E[游戏依赖检测]
    E --> F[属性缓存]
    F --> G[生成 MSBuild 属性]

    B --> B1[DuckovFolder 解析]
    B --> B2[AssetsDir 解析]
    B --> B3[ModsDirectory 计算]

    C --> C1[IsModLib 检测]
    C --> C2[ExcludeSdkLib 处理]

    F --> F1[缓存文件生成]
    F1 --> F2[性能优化]

    style B fill:#f3e5f5,color:#4a148c
    style G fill:#f3e5f5,color:#4a148c
```

### 阶段 4: 资源生成 (CSX)

```mermaid
graph TD
    A[资源生成开始] --> B[预览图生成]
    B --> C[info.ini 创建]
    C --> D[资源验证]
    D --> E[目录结构检查]
    E --> F[资源完成 ✅]

    B --> B1[generate-preview-enhanced.csx]
    C --> C1[ensure-info-ini-enhanced.csx]

    B1 --> B2[源图像检测]
    B1 --> B3[图像处理]
    B1 --> B4[预览图生成]

    C1 --> C2[ModName 同步]
    C1 --> C3[元数据验证]
    C1 --> C4[INI 格式化]

    style B fill:#fff3e0,color:#e65100
    style C fill:#fff3e0,color:#e65100
```

### 阶段 5: 核心编译

```mermaid
graph TD
    A[C# 编译开始] --> B[源代码编译]
    B --> C[源生成器执行]
    C --> D[L 类生成]
    D --> E[元数据生成]
    E --> F[程序集输出]

    C --> C1[DuckyLocalizationGenerator]
    C1 --> C2[LK 类扫描]
    C1 --> C3[强类型生成]
    C1 --> C4[元数据 JSON 创建]

    E --> E1[LanguageSupport 解析]
    E1 --> E2["all" 关键字展开]
    E2 --> E3["de, en, es, fr, ja, ko, pt, ru, zh-hant, zh"]

    style F fill:#e8f5e8,color:#1b5e20
```

### 阶段 6: 本地化处理 (关键阶段)

```mermaid
graph TD
    A[本地化处理开始] --> B[ExtractLocalizationKeysCSX]
    B --> C[键提取成功?]
    C -->|是| D[UpdateLocalizationCsvCSX]
    C -->|否| E[提取失败 ❌]
    D --> F[CopyLocalizationAssetsCSX]
    F --> G[本地化完成 ✅]

    B --> B1[编译程序集分析]
    B1 --> B2[反射提取 LanguageSupport]
    B1 --> B3[const 字符串提取]
    B1 --> B4[lkeys.json 生成]

    D --> D1[CSV 文件处理]
    D1 --> D2[多目录支持]
    D1 --> D3[本地化资源更新]

    F --> F1[AssetsDir 同步]
    F1 --> F2[分号分隔路径处理]
    F1 --> F3[资源验证]

    style B fill:#fff8e1,color:#f57c00
    style D fill:#fff8e1,color:#f57c00
    style G fill:#fff8e1,color:#f57c00
    style E fill:#ffebee,color:#c62828
```

**❌ 当前问题**: 在实际构建中，`UpdateLocalizationCsvCSX` 在 `ExtractLocalizationKeysCSX` 之前运行，导致找不到 `lkeys.json` 文件。

### 阶段 7: 打包

```mermaid
graph TD
    A[打包开始] --> B{EnableILRepack?}
    B -->|true| C[ILRepack 合并]
    B -->|false| D[依赖收集]
    C --> E[单文件输出]
    D --> F[多文件复制]
    E --> G[PDB 处理]
    F --> G
    G --> H[打包完成 ✅]

    C --> C1[ilrepack-assemblies.csx]
    D --> D1[copy-dependencies.csx]
    D1 --> D2[collect-from-mod.csx]

    style C fill:#f3e5f5,color:#6a1b9a
    style D fill:#f3e5f5,color:#6a1b9a
    style H fill:#f3e5f5,color:#6a1b9a
```

### 阶段 8: 部署

```mermaid
graph TD
    A[部署开始] --> B{DeployMod?}
    B -->|true| C[完整部署流程]
    B -->|false| D[仅清理模式]
    C --> E[目标目录清理]
    D --> E
    E --> F{DeployMod?}
    F -->|true| G[资源文件复制]
    F -->|false| H[跳过复制]
    G --> I[DLL 文件复制]
    H --> J[清理完成 ✅]
    I --> K[部署验证]
    K --> L[部署完成 ✅]

    C --> C1[IsModLib 检查]
    C1 --> C2[库项目排除]
    C1 --> C3[错误处理]

    D --> D1[仅执行清理逻辑]
    D1 --> D2[删除现有模组文件]
    D2 --> D3[不复制新文件]

    style C fill:#e0f2f1,color:#00695c
    style D fill:#fff3e0,color:#e65100
    style L fill:#e0f2f1,color:#00695c
    style J fill:#fff3e0,color:#e65100
```

### ❌ 当前问题：DeployMod=false 逻辑
```mermaid
graph TD
    A[DeployMod=false] --> B[ShouldSkipDeployment 返回 true]
    B --> C[完全跳过部署脚本执行]
    C --> D[游戏目录中保留旧文件]
    D --> E[可能产生冲突或过时文件]

    style B fill:#ffebee,color:#b71c1c
    style C fill:#ffebee,color:#b71c1c
    style E fill:#ffebee,color:#b71c1c
```

### ✅ 正确行为：清理模式
```mermaid
graph TD
    A[DeployMod=false] --> B[执行部署脚本但不复制新文件]
    B --> C[调用 CleanTargetDirectory]
    C --> D[删除游戏目录中的模组文件]
    D --> E[保持游戏目录清洁]
    E --> F[后续部署不会产生冲突]

    style C fill:#fff3e0,color:#e65100
    style D fill:#fff3e0,color:#e65100
    style F fill:#e8f5e8,color:#1b5e20
```

### 阶段 9: 后处理

```mermaid
graph TD
    A[后处理开始] --> B[清理临时文件]
    B --> C[验证输出]
    C --> D[生成报告]
    D --> E[缓存更新]
    E --> F[构建完成 ✅]

    B --> B1[临时文件删除]
    C --> C1[输出完整性检查]
    D --> D1[构建统计]
    E --> E1[增量构建准备]

    style F fill:#f1f8e9,color:#33691e
```

## 目标依赖关系图

```mermaid
graph TD
    A[Build] --> B[ValidateProject]
    B --> C[ValidateProjectPathCSX]
    B --> D[ValidateDuckovFolderCSX]

    A --> E[ResolveBuildProperties]
    E --> B

    A --> F[GenerateAssets]
    F --> E
    F --> G[GeneratePreviewCSX]
    F --> H[EnsureInfoIniCSX]

    A --> I[ProcessLocalization]
    I --> J[ExtractLocalizationKeysCSX]
    I --> K[UpdateLocalizationCsvCSX]
    I --> L[CopyLocalizationAssetsCSX]

    A --> M[PackageAndDeploy]
    M --> N[CollectFromModCSX]
    M --> O[CopyToDuckovCSX]
    M --> P[ILRepackAssembliesCSX]
    M --> Q[CopyDependenciesCSX]

    style J fill:#ff9800,color:#000
    style K fill:#ff9800,color:#000
```

## 参数传递系统

### 标准 BuildContext 参数格式
```bash
--sdk-scripts-path <path> \
--project-dir <path> \
--configuration <config> \
--target-framework <tfm> \
--mod-name <name> \
--duckov-folder <path> \
--steam-folder <path> \
--assets-dir <path> \
--localization-assets-dir <path> \
--enable-ilrepack <bool> \
--enable-global-using <bool> \
--include-harmony <bool> \
--deploy-mod <bool> \
--exclude-sdk-lib <bool> \
--is-mod-lib <bool>
```

## 性能优化特性

1. **缓存系统** - 属性和本地化键缓存
2. **条件执行** - 根据项目类型跳过不必要步骤
3. **增量构建** - 源生成器集成
4. **多目录支持** - 优化的资源复制
5. **详细日志控制** - minimal, normal, detailed, diagnostic

## 关键失败点

### 🔴 当前关键问题
- **本地化处理顺序错误** - `UpdateLocalizationCsvCSX` 在 `ExtractLocalizationKeysCSX` 前执行
- **参数传递问题** - `LocalizationAssetsDir` 和 `AssetsDir` 解析错误
- **验证脚本上下文** - MSBuild 环境与手动执行环境差异

### 🟡 潜在风险点
- ILRepack 合并冲突
- 游戏依赖检测失败
- 路径特殊字符问题
- 权限不足导致的写入失败

这个完整的构建流程分析为修复方案提供了清晰的技术路线图和实施指导。