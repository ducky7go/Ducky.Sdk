using System;
using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json;

namespace Ducky.Sdk.Utils;

/// <summary>
/// Strongly-typed wrapper for mod identifier strings.
/// Provides type safety to prevent mixing mod IDs with other string values.
/// </summary>
[JsonConverter(typeof(ModIdJsonConverter))]
[TypeConverter(typeof(ModIdTypeConverter))]
public readonly record struct ModId
{
    /// <summary>
    /// The underlying mod identifier string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModId"/> struct.
    /// </summary>
    /// <param name="value">The mod identifier string.</param>
    /// <exception cref="ArgumentException">Thrown when value is null or whitespace.</exception>
    public ModId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Mod ID cannot be null or whitespace.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Implicitly converts a string to a <see cref="ModId"/>.
    /// </summary>
    public static implicit operator ModId(string value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="ModId"/> to a string.
    /// </summary>
    public static implicit operator string(ModId modId) => modId.Value;

    /// <summary>
    /// Returns the string representation of this mod ID.
    /// </summary>
    public override string ToString() => Value;

    /// <summary>
    /// Determines whether this mod ID is empty or whitespace.
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    /// <summary>
    /// Creates a <see cref="ModId"/> from a string, or returns a default empty value if the string is invalid.
    /// </summary>
    public static ModId FromStringOrDefault(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? default : new ModId(value);
    }
}

/// <summary>
/// JSON converter for <see cref="ModId"/> that serializes it as a plain string.
/// Supports both value serialization and dictionary key serialization.
/// </summary>
internal sealed class ModIdJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ModId);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is ModId modId)
        {
            // When used as dictionary key, write the raw string value
            // When used as regular value, also write as string
            writer.WriteValue(modId.Value);
        }
        else
        {
            writer.WriteNull();
        }
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return default(ModId);
        }

        if (reader.TokenType == JsonToken.String)
        {
            var value = reader.Value?.ToString();
            return string.IsNullOrWhiteSpace(value) ? default(ModId) : new ModId(value);
        }

        // Handle property name tokens (used when ModId is a dictionary key)
        if (reader.TokenType == JsonToken.PropertyName)
        {
            var value = reader.Value?.ToString();
            return string.IsNullOrWhiteSpace(value) ? default(ModId) : new ModId(value);
        }

        throw new JsonSerializationException($"Unexpected token type {reader.TokenType} when parsing ModId.");
    }
}

/// <summary>
/// Type converter for <see cref="ModId"/> that enables it to be used as dictionary keys.
/// </summary>
internal sealed class ModIdTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            return string.IsNullOrWhiteSpace(str) ? default(ModId) : new ModId(str);
        }

        return base.ConvertFrom(context, culture, value);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value,
        Type destinationType)
    {
        if (destinationType == typeof(string) && value is ModId modId)
        {
            return modId.Value;
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
