using System.Reflection;
using System.Text;
using Duckov;
using Duckov.Buffs;
using Duckov.Buildings;
using Duckov.Crops;
using Duckov.Modding;
using Duckov.UI;
using Duckov.Utilities;
using FOW;
using ItemStatsSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Ducky.Sdk.Lib.Tests;

public class CodeGenTests
{
    [Test]
    public void GenerateGetSetForFields()
    {
        var types = new[]
        {
            typeof(GameplayDataSettings.TagsData),
            typeof(GameplayDataSettings.PrefabsData),
            typeof(UIPrefabsReference),
            typeof(GameplayDataSettings.ItemAssetsData),
            typeof(GameplayDataSettings.StringListsData),
            typeof(GameplayDataSettings.LayersData),
            typeof(GameplayDataSettings.SceneManagementData),
            typeof(GameplayDataSettings.SceneManagementData),
            typeof(GameplayDataSettings.BuffsData),
            typeof(GameplayDataSettings.QuestsData),
            typeof(GameplayDataSettings.EconomyData),
            typeof(GameplayDataSettings.UIStyleData),
            typeof(InputActionAsset),
            typeof(BuildingDataCollection),
            typeof(CustomFaceData),
            typeof(CraftingFormulaCollection),
            typeof(DecomposeDatabase),
            typeof(StatInfoDatabase),
            typeof(StockShopDatabase),
            typeof(GameplayDataSettings.LootingData),
            typeof(CropDatabase),
            typeof(GameplayDataSettings.SpritesData),
            typeof(GameplayDataSettings.CharacterRandomPresets),

            typeof(Buff),
            typeof(ItemSetting_Gun),
            typeof(ItemAgent_Gun),
            typeof(ItemAgent_MeleeWeapon),
            typeof(ItemAgent_Kazoo),
            typeof(ItemAgent),
            typeof(ItemAgentHolder),
            typeof(FogOfWarPass),
            typeof(LevelManager),
            typeof(SceneLoader),
            typeof(SceneManager),
            typeof(Health),
            typeof(ModManager),
            typeof(SteamManager),
            typeof(Effect),
            typeof(Item),
            typeof(SkillBase),
            typeof(Skill_Grenade),
            typeof(EXPManager),
            typeof(DamageInfo),
            typeof(CharacterBuffManager),
            typeof(CharacterMainControl),
            typeof(CharacterItemControl),
            typeof(CharacterSkillKeeper),
            typeof(ExplosionManager)
        };
        var outDir = Path.GetFullPath("../../../../../SDKlibs/Ducky.Sdk.Lib/GameApis");
        const bool forceOverwrite = false; // Set to true to regenerate existing files

        Directory.CreateDirectory(outDir);

        foreach (var type in types)
        {
            var className = type.Name;
            var fileName = $"{className}Extensions.cs";
            var filePath = Path.Combine(outDir, fileName);
            if (File.Exists(filePath) && !forceOverwrite)
            {
                Console.WriteLine($"Skipped (exists): {filePath}");
                continue;
            }

            var sb = new StringBuilder();

            // 生成文件头
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Runtime.CompilerServices;");

            // 收集需要额外添加的 using（考虑 List 和泛型参数的命名空间）
            var extraNamespaces = new HashSet<string>();
            // 扫描字段与属性类型以获取可能需要的命名空间
            var memberTypes = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                             BindingFlags.Static)
                .Select(f => f.FieldType)
                .Concat(type
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                   BindingFlags.Static)
                    .Select(p => p.PropertyType))
                .Where(t => t != null)
                .Distinct()
                .ToList();

            foreach (var mt in memberTypes)
            {
                if (mt.IsArray)
                {
                    var elem = mt.GetElementType();
                    if (!string.IsNullOrEmpty(elem?.Namespace)) extraNamespaces.Add(elem.Namespace);
                }

                if (mt.IsGenericType)
                {
                    // 对泛型参数尝试引入它们的命名空间
                    foreach (var ga in mt.GetGenericArguments())
                    {
                        if (!string.IsNullOrEmpty(ga.Namespace))
                            extraNamespaces.Add(ga.Namespace);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(mt.Namespace))
                        extraNamespaces.Add(mt.Namespace);
                }
            }

            extraNamespaces.Add("System.Collections.Generic");
            extraNamespaces.Add("System.Collections.ObjectModel");
            extraNamespaces.Add("UnityEngine.Events");

            // 只在命名空间非空时添加 using（始终确保 type 自身的命名空间被引入，避免重复）
            if (!string.IsNullOrEmpty(type.Namespace))
            {
                sb.AppendLine($"using {type.Namespace};");
                extraNamespaces.Remove(type.Namespace);
            }

            // 添加额外的 using（按排序确保稳定输出）
            foreach (var nsName in extraNamespaces.OrderBy(n => n))
            {
                sb.AppendLine($"using {nsName};");
            }

            // 如果有静态字段，需要引入 FieldExtensions
            var hasStaticFields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
                .Any(f => f.GetCustomAttribute<SerializeField>() != null || f.IsPublic);

            if (hasStaticFields)
            {
                sb.AppendLine("using Ducky.Sdk.GameApis;");
            }

            sb.AppendLine();
            sb.AppendLine("namespace Ducky.Sdk.GameApis;");
            sb.AppendLine();
            sb.AppendLine("// ReSharper disable once InconsistentNaming");
            sb.AppendLine($"public static class {className}Extensions");
            sb.AppendLine("{");

            // 获取所有属性（用于检查是否已有对应的属性）
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                                BindingFlags.Static)
                .Select(p => p.Name)
                .ToHashSet();

            // 获取所有实例字段（SerializeField 或 public）
            var instanceFields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                .Where(f => !f.IsStatic && (f.GetCustomAttribute<SerializeField>() != null || f.IsPublic))
                .OrderBy(f => f.Name)
                .ToList();

            // 获取所有静态字段
            var staticFields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
                .Where(f => f.GetCustomAttribute<SerializeField>() != null || f.IsPublic)
                .OrderBy(f => f.Name)
                .ToList();

            // 生成实例字段的 Get/Set 扩展方法
            if (instanceFields.Any())
            {
                sb.AppendLine("    // Instance field accessors");
                foreach (var field in instanceFields)
                {
                    var fieldName = field.Name;
                    var fieldType = field.FieldType;
                    var methodNameSuffix = ToPascalCase(fieldName);
                    var paramType = GetFriendlyTypeName(fieldType);
                    var useObject = ShouldUseObjectType(fieldType);

                    // 检查是否已存在对应的属性（PascalCase 名称）
                    var hasProperty = properties.Contains(methodNameSuffix);

                    // 如果使用 object 类型，添加注释说明
                    if (useObject)
                    {
                        paramType = "object";
                        sb.AppendLine($"    // Field '{fieldName}' uses object type (actual type: {fieldType.Name})");
                    }

                    // 如果已有属性，只生成 Setter，跳过 Getter
                    if (!hasProperty)
                    {
                        // 生成 Getter 方法
                        sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
                        if (useObject)
                        {
                            sb.AppendLine(
                                $"    public static {paramType} Get{methodNameSuffix}(this {className} target) =>");
                            sb.AppendLine($"        target.GetField<{className}>(\"{fieldName}\");");
                        }
                        else
                        {
                            sb.AppendLine(
                                $"    public static {paramType} Get{methodNameSuffix}(this {className} target) =>");
                            sb.AppendLine($"        target.GetField<{className}, {paramType}>(\"{fieldName}\");");
                        }

                        sb.AppendLine();
                    }

                    // 始终生成 Setter 方法 (Fluent API - leverages SetField fluent return)
                    sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
                    sb.AppendLine(
                        $"    public static {className} Set{methodNameSuffix}(this {className} target, {paramType} value) =>");
                    sb.AppendLine($"        target.SetField(\"{fieldName}\", value);");
                    sb.AppendLine();
                }
            }

            // 生成静态字段的 Get/Set 方法
            if (staticFields.Any())
            {
                sb.AppendLine("    // Static field accessors");
                foreach (var field in staticFields)
                {
                    var fieldName = field.Name;
                    var fieldType = field.FieldType;
                    var methodNameSuffix = ToPascalCase(fieldName);
                    var paramType = GetFriendlyTypeName(fieldType);
                    var useObject = ShouldUseObjectType(fieldType);

                    // 如果使用 object 类型，添加注释说明
                    if (useObject)
                    {
                        paramType = "object";
                        sb.AppendLine(
                            $"    // Static field '{fieldName}' uses object type (actual type: {fieldType.Name})");
                    }

                    // 生成静态 Getter 方法
                    sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
                    if (useObject)
                    {
                        sb.AppendLine($"    public static {paramType} GetStatic{methodNameSuffix}() =>");
                        sb.AppendLine($"        FieldExtensions.GetStaticField(typeof({className}), \"{fieldName}\");");
                    }
                    else
                    {
                        sb.AppendLine($"    public static {paramType} GetStatic{methodNameSuffix}() =>");
                        sb.AppendLine(
                            $"        FieldExtensions.GetStaticField<{className}, {paramType}>(\"{fieldName}\");");
                    }

                    sb.AppendLine();

                    // 生成静态 Setter 方法 (Fluent API - returns Type for potential chaining)
                    sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
                    if (useObject)
                    {
                        sb.AppendLine($"    public static Type SetStatic{methodNameSuffix}({paramType} value)");
                        sb.AppendLine("    {");
                        sb.AppendLine(
                            $"        FieldExtensions.SetStaticField(typeof({className}), \"{fieldName}\", value);");
                        sb.AppendLine($"        return typeof({className});");
                        sb.AppendLine("    }");
                    }
                    else
                    {
                        sb.AppendLine($"    public static Type SetStatic{methodNameSuffix}({paramType} value)");
                        sb.AppendLine("    {");
                        sb.AppendLine(
                            $"        FieldExtensions.SetStaticField<{className}, {paramType}>(\"{fieldName}\", value);");
                        sb.AppendLine($"        return typeof({className});");
                        sb.AppendLine("    }");
                    }

                    sb.AppendLine();
                }
            }

            sb.AppendLine("}");

            // 写入文件
            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"Generated: {filePath}");
        }
    }

    private static string ToPascalCase(string name)
    {
        // 去掉下划线，转换为 PascalCase
        if (string.IsNullOrEmpty(name)) return name;

        // 处理下划线开头的情况
        name = name.TrimStart('_');

        // 分割下划线
        var parts = name.Split('_');
        var sb = new StringBuilder();

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            sb.Append(char.ToUpper(part[0]));
            if (part.Length > 1)
                sb.Append(part.Substring(1));
        }

        return sb.ToString();
    }

    private static string GetFriendlyTypeName(Type type)
    {
        // Handle arrays
        if (type.IsArray)
        {
            var elem = type.GetElementType();
            return $"{GetFriendlyTypeName(elem)}[]";
        }

        // Common built-in types
        if (type == typeof(int)) return "int";
        if (type == typeof(float)) return "float";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(string)) return "string";
        if (type == typeof(void)) return "void";

        // Nullable<T> -> T?
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var arg = type.GetGenericArguments()[0];
            return $"{GetFriendlyTypeName(arg)}?";
        }

        // Generic types (general handling)
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();

            // Get base name without `1 suffix
            var name = genericDef.Name;
            var tick = name.IndexOf('`');
            if (tick >= 0) name = name.Substring(0, tick);

            var args = string.Join(", ", genericArgs.Select(a => GetFriendlyTypeName(a)));
            return $"{name}<{args}>";
        }

        // Nested types: format DeclaringType.FriendlyName
        if (type.IsNested)
        {
            var declaring = type.DeclaringType;
            var declaringName = declaring != null ? GetFriendlyTypeName(declaring) : declaring?.Name;
            return $"{declaringName}.{type.Name}";
        }

        // Unity/game types or internal types: fallback to object when not visible
        if (type.Namespace != null &&
            (type.Namespace.StartsWith("UnityEngine") ||
             type.Namespace.StartsWith("Duckov") ||
             type.Namespace.StartsWith("ItemStatsSystem")))
        {
            if (type.IsPublic || type.IsNestedPublic)
            {
                return type.Name;
            }

            return "object";
        }

        // Fallback to simple name
        return type.Name;
    }

    private static bool ShouldUseObjectType(Type type)
    {
        return false;
    }
}
