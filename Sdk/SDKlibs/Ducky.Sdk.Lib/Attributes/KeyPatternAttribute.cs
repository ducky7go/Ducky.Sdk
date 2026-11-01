using System;

namespace Ducky.Sdk.Attributes;

/// <summary>
/// Declares a family of localization keys whose names are built at runtime, such as one key per world-effect code.
/// Put it on the <c>LK</c> class. In the pattern, <c>{identifier}</c> is a runtime parameter and <c>{a|b|c}</c> is a
/// choice list. The source generator emits a typed accessor under <c>L</c> for each choice, for example
/// <c>L.WorldEffect.Name(code)</c> for <c>worldeffect.{code}.{name|summary|description}</c>. Each accessor is a thin
/// wrapper over <c>L.Dynamic</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class KeyPatternAttribute : Attribute
{
    /// <summary>Creates the attribute for one pattern.</summary>
    /// <param name="pattern">The key pattern, for example <c>worldeffect.{code}.{name|summary|description}</c>.</param>
    public KeyPatternAttribute(string pattern)
    {
        Pattern = pattern ?? string.Empty;
    }

    /// <summary>The key pattern.</summary>
    public string Pattern { get; }

    /// <summary>
    /// The name of the generated accessor class under <c>L</c>. When not set, it is the first literal segment of the
    /// pattern with its first letter upper-cased (<c>worldeffect</c> gives <c>Worldeffect</c>), so set this to
    /// <c>WorldEffect</c> to get <c>L.WorldEffect</c>.
    /// </summary>
    public string? Group { get; set; }
}
