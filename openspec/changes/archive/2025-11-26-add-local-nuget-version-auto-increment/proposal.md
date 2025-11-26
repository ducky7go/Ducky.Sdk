# Change: Add Local NuGet Version Auto-Increment Management

## Why
当前本地开发时手动管理NuGet包版本号容易出错，且缓存问题导致测试时不一定能使用到最新版本。需要建立自动版本递增机制，确保每次本地打包都使用唯一版本号，避免NuGet缓存问题。

## What Changes
- 在根目录创建 `nuget.props` 文件存储本地打包版本号
- 修改 `packToLocal.sh` 脚本支持自动版本递增
- 更新所有 Samples 项目引用 `nuget.props` 获取版本号
- 版本格式：`0.0.000000-dev`，六位数字部分自动递增

## Impact
- **Affected specs**: mod-build (添加本地开发包版本管理需求)
- **Affected code**:
  - `scripts/packToLocal.sh` (主要修改)
  - `Samples/**/*.csproj` (添加 props 引用)
  - `nuget.props` (新文件)