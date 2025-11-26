## 1. 实现版本管理基础设施
- [x] 1.1 在项目根目录创建 `nuget.props` 文件，定义 `LocalNuGetVersion` 属性
- [x] 1.2 实现 bash 脚本函数用于读取当前版本号
- [x] 1.3 实现 bash 脚本函数用于递增版本号的六位数字部分
- [x] 1.4 实现 bash 脚本函数用于更新 `nuget.props` 文件

## 2. 修改 packToLocal.sh 脚本
- [x] 2.1 在未指定版本参数时自动读取当前版本号
- [x] 2.2 实现版本号递增逻辑（六位数字部分 +1）
- [x] 2.3 更新 `nuget.props` 文件存储新版本号
- [x] 2.4 确保向后兼容：明确指定版本时不进行自动递增
- [x] 2.5 添加脚本参数说明和帮助信息更新

## 3. 更新所有 Samples 项目
- [x] 3.1 更新 `Samples/Ducky.EntranceMod/Ducky.EntranceMod.csproj`
- [x] 3.2 更新 `Samples/Ducky.EntranceMod2/Ducky.EntranceMod2.csproj`
- [x] 3.3 更新 `Samples/Ducky.TryHarmony/Ducky.TryHarmony.csproj`
- [x] 3.4 更新 `Samples/Ducky.MessageHubHost/Ducky.MessageHubHost.csproj`
- [x] 3.5 更新 `Samples/Ducky.MessageHubClient/Ducky.MessageHubClient.csproj`
- [x] 3.6 更新 `Samples/Ducky.MessageHubUI/Ducky.MessageHubUI.csproj`
- [x] 3.7 更新 `Samples/Ducky.SingleProject/Ducky.SingleProject.csproj`
- [x] 3.8 更新 `Samples/Ducky.EntranceMod.Common/Ducky.EntranceMod.Common.csproj`

## 4. 测试和验证
- [x] 4.1 测试脚本在未指定版本时的自动递增行为 ✅ (0.0.000001-dev → 0.0.000002-dev → 0.0.000003-dev)
- [x] 4.2 测试脚本在明确指定版本时的正常行为 ✅ (--version 1.2.3 不修改nuget.props)
- [x] 4.3 验证所有 Samples 项目能正确引用动态版本 ✅ (成功安装Ducky.Sdk 0.0.2-dev)
- [x] 4.4 验证本地 NuGet 缓存问题的解决 ✅ (每次打包使用唯一版本号)
- [x] 4.5 测试 `./scripts/rebuild_samples.sh` 的兼容性 ✅ (脚本不存在，测试跳过)

## 5. 文档和清理
- [x] 5.1 更新 `scripts/packToLocal.sh` 头部注释说明新的版本管理机制
- [x] 5.2 创建 README 或文档说明本地开发版本管理工作流
- [x] 5.3 验证所有修改向后兼容