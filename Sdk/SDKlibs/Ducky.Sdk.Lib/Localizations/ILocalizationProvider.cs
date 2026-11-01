using System.Collections.Generic;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Localizations;

/// <summary>
/// A source of translations owned by one mod (or sub-mod). The SDK asks for one language at a time and caches the
/// result per source, so <see cref="Load"/> is called only for languages that are actually needed.
/// </summary>
public interface ILocalizationProvider
{
    /// <summary>The mod that owns these texts. Lookups can be scoped to it with <c>L.Scope(modId)</c>.</summary>
    ModId ModId { get; }

    /// <summary>The languages this provider supplies. Codes are normalized by the SDK (see <c>LanguageCodes</c>).</summary>
    IReadOnlyCollection<string> Languages { get; }

    /// <summary>The key/value pairs of one language. Keys are compared ignoring case; an empty value counts as absent.</summary>
    IReadOnlyDictionary<string, string> Load(string language);
}
