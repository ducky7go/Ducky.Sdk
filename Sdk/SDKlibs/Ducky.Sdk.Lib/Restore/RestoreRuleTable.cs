using System;
using System.Collections.Generic;
using Ducky.Sdk.Rules;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Restore;

/// <summary>
/// One rule registry per <see cref="RestoreKind"/>, with the callbacks that install and remove that kind's game patch.
/// The kinds are independent: the rules of one never touch another's registry or patch. Touches no Unity object, so the
/// behavior is tested with fake callbacks.
/// </summary>
internal sealed class RestoreRuleTable
{
    private static readonly RestoreKind[] Kinds = { RestoreKind.Health, RestoreKind.Energy, RestoreKind.Water };

    private readonly Dictionary<RestoreKind, RuleRegistry<float, RestoreContext>> _byKind = new();

    /// <param name="onFirst">Called for a kind when its first rule is added (its patch is installed here).</param>
    /// <param name="onLast">Called for a kind when its last rule is removed (its patch is removed here).</param>
    public RestoreRuleTable(Func<RestoreKind, Action> onFirst, Func<RestoreKind, Action> onLast)
    {
        foreach (var kind in Kinds)
        {
            _byKind[kind] = new RuleRegistry<float, RestoreContext>(onFirst(kind), onLast(kind));
        }
    }

    /// <summary>The registry of <paramref name="kind"/>.</summary>
    public RuleRegistry<float, RestoreContext> For(RestoreKind kind) => _byKind[kind];

    /// <summary>The patch identifier of a kind in one copy: "&lt;assembly&gt;.RestoreRules.&lt;kind&gt;.&lt;instance guid&gt;".</summary>
    public static string PatchIdFor(string assemblyName, RestoreKind kind, Guid instance) =>
        PatchIds.For(assemblyName, "RestoreRules." + kind, instance);
}
