using System.Reflection;
using System.Text;
using Duckov.Buffs;
using Duckov.Utilities;
using UnityEngine;

namespace Ducky.Sdk.Lib.Tests;

public class CodeGenTests
{
    // [Test]
    public void GenerateGetSetForFields()
    {
        var types = new[]
        {
            typeof(Buff),
            typeof(GameplayDataSettings.BuffsData),
            typeof(ItemSetting_Gun),
            typeof(ItemAgent_Gun)
        };
        var outDir = Path.GetFullPath("../../../../../SDKlibs/Ducky.Sdk.Lib/GameApis");
        const bool forceOverwrite = true; // Set to true to regenerate existing files

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

            // 只在命名空间非空时添加 using
            if (!string.IsNullOrEmpty(type.Namespace))
            {
                sb.AppendLine($"using {type.Namespace};");
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
        // 处理常见类型
        if (type == typeof(int)) return "int";
        if (type == typeof(float)) return "float";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(string)) return "string";
        if (type == typeof(void)) return "void";

        // 处理泛型类型
        if (type.IsGenericType)
        {
            var genericType = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();

            if (genericType == typeof(List<>))
            {
                return $"List<{GetFriendlyTypeName(genericArgs[0])}>";
            }

            if (genericType == typeof(Dictionary<,>))
            {
                return $"Dictionary<{GetFriendlyTypeName(genericArgs[0])}, {GetFriendlyTypeName(genericArgs[1])}>";
            }
        }

        // 处理嵌套类型
        if (type.IsNested)
        {
            var declaringType = type.DeclaringType;
            return $"{declaringType?.Name}.{type.Name}";
        }

        // 处理 Unity/游戏类型，某些类型可能不公开，使用 object
        if (type.Namespace != null &&
            (type.Namespace.StartsWith("UnityEngine") ||
             type.Namespace.StartsWith("Duckov") ||
             type.Namespace.StartsWith("ItemStatsSystem")))
        {
            // 对于已知的公开类型，使用其名称
            if (type.IsPublic || type.IsNestedPublic)
            {
                return type.Name;
            }

            // 对于不公开的类型（如 Sprite, GameObject 等内部类型），使用 object
            return "object";
        }

        return type.Name;
    }

    private static bool ShouldUseObjectType(Type type)
    {
        // Unity 类型如 Sprite, GameObject 等在某些上下文中不可见，使用 object
        var typeName = type.Name;
        return typeName == "Sprite" ||
               typeName == "GameObject" ||
               typeName == "Transform" ||
               typeName == "Component" ||
               (!type.IsPublic && !type.IsNestedPublic);
    }
}
