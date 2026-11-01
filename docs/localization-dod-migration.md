# Migrating Newbe.Dod localization to Ducky.Sdk

This guide describes how the Newbe.Dod (DOD) mod can move its localization from its own copy of the CSV and override
code to the SDK. It is documentation only: this change does not edit DOD. Each sub-mod can move on its own, and each
step can be rolled back.

The rules and APIs referred to here are described in [`localization.md`](./localization.md).

## Why

DOD keeps its own copy of the localization logic (`WorldEffectLocalizationManager`). The copy has drifted from the
SDK's behavior:

| Topic | DOD today | SDK after migration |
|---|---|---|
| Language code | `zh-Hant` in code, `zh-hant` in the SDK | `zh-Hant` accepted and normalized to `zh-hant` |
| Key comparison | case-sensitive | case-insensitive (ordinal) |
| Missing text | `[Missing...]` | `[key]` |
| CSV reader | `ParseCsvLine` does not handle every escaped `"` | one reader (`CsvDocument`) for runtime and build |
| Fallback | English only | `zh-hant -> zh -> en` |
| Validation | none | `LocalizationValidation` (build) and `locales validate` |

Case-insensitive keys are the one behavior change that can affect existing lookups. Run `locales validate` first (see
step 8) to find keys that differ only by case.

## 1. Required SDK APIs

The migration needs the SDK version that includes this change, and these members:

- `L.RegisterLocales(ModId, string, LocalizationSourceOptions?)` returns an `IDisposable` handle.
- `L.Scope(ModId).Get`, `.TryGet` and `.Dynamic` / `.DynamicOr` / `.TryDynamic`.
- `L.Dynamic`, `L.DynamicOr`, `L.TryDynamic`, `L.TryGet`, `L.Reindex`, `L.ConfigureFallback`.
- `[KeyPattern(string, Group = ...)]` on the `LK` class.
- `LocalizationSourceOptions` (`KeyPrefix`, `PublishToGame`).
- `ModId` (`Ducky.Sdk.Utils`).
- The build properties `LocalizationValidation` and `LocalizationExtraKeys`.
- The `locales` commands of `tools/net10.0/Ducky.Sdk.Build.dll`.

## 2. Register each sub-mod's Locales folder

Every sub-mod with a `Locales` folder registers it under its own `ModId`, and disposes the handle on unload or hot reload:

```csharp
// in the sub-mod's enable path
var modId = new ModId("newbe.dod.worldeffect");
_localesHandle = L.RegisterLocales(modId, Path.Combine(ModFolder.Get(), "Locales"));

// in the disable / hot reload path
_localesHandle?.Dispose();
_localesHandle = null;
```

`ModFolder.Get()` is the SDK helper that DOD already uses for `Locales`. The first `L.RegisterLocales` call also applies
the current language to that source, so no separate `SetLanguage` call is needed. Disposing the handle removes the game
texts that source owns and nothing else.

Tests that cover this: `SubModDirectory_ItsTextsAreAvailableAndTheCurrentLanguageIsApplied`,
`UnloadOneOfTwo_RemovesOnlyItsOverrides`, `ReleaseOverrideTexts_RemovesEverySourceOverride`.

## 3. Replace the EffectLocalization lookups

DOD reads one `EffectLocalization` per code, built from `worldeffect.{CODE}.{name|summary|description}` keys. Declare the
key family once and use the generated accessors:

```csharp
[LanguageSupport("all")]
[KeyPattern("worldeffect.{code}.{name|summary|description}", Group = "WorldEffect")]
public static class LK { /* the existing static keys */ }

// replacing EffectLocalization lookups:
var name = L.WorldEffect.Name(code);
var summary = L.WorldEffect.Summary(code);
var description = L.WorldEffect.Description(code);
```

Or, without a declared pattern, call `L.Dynamic("worldeffect.{0}.name", code)`. Both give the same text, because the
accessors are wrappers over `L.Dynamic`.

Scope the lookups to the sub-mod's `ModId` with `L.Scope(modId)` when two sub-mods can use the same key.

Tests that cover this: `KeyPattern_EmitsOneTypedAccessorPerChoiceOverDynamic`,
`EffectCodeKey_ReturnsTheTextOfTheFormattedKey`, `ScopedDynamic_SeesOnlyItsOwnSource`.

## 4. Scoping and raw game keys

- Use one `ModId` per sub-mod.
- Keep the game keys raw. DOD's buff UI reads `worldeffect.*` by the raw key, so `KeyPrefix` stays unset. Set a
  `KeyPrefix` only when two sub-mods publish the same key, and then only on the sub-mod that must not collide.

Tests that cover this: `PrefixedPublishing_PublishesTheKeyWithThePrefix`,
`Collision_OwnerTextIsPublishedAndOneWarningIsLogged`.

## 5. Behavior differences

- **Case-insensitive keys.** A DOD lookup that is case-sensitive today becomes case-insensitive. If two keys differ only
  by case, `locales validate` reports it, and one of the two is lost. Rename one of them first.
- **`[key]` instead of `[Missing...]`.** Code that tests for the `[Missing` prefix must test for `[` and the key, or use
  `L.TryGet`.
- **`zh-Hant`.** The code is accepted as an alias of `zh-hant`, so `zh-Hant.csv` keeps working. It does not have to be
  renamed.
- **Fallback chain.** A `zh-hant` player gets `zh` text before `en`. `L.ConfigureFallback` changes this for a language.
- **File references.** A CSV value is a file only with the `@file:` marker. DOD's rows are text, so there is nothing to
  change. Legacy `{key}.md` rows still work and log a deprecation notice.

Tests that cover this: `MixedCaseCsvKey_MatchesLookupInAnyCase`, `UnknownKey_GivesBracketedKeyAndTryGetReportsAbsenceWithoutPlaceholder`,
`ZhHantUser_ReceivesZhTextBeforeEnglish`, `MixedCaseLanguageFileName_ResolvesToTheCanonicalCode`,
`LegacyExactRow_ReadsTheFileAndLogsDeprecation`.

## 6. Remove the DOD copy

Once the sub-mod uses the SDK, delete the copy in `NewMods/Newbe.Dod.Common/WorldEffectLocalizationManager.cs`:

- the CSV reading (`LoadLocalizations`, `ParseCsv`, `ParseCsvLine`);
- the language-code mapping (the `SystemLanguage` switch that returns `zh-Hant`);
- the `LocalizationManager.SetOverrideText` calls in `ApplyCurrentLanguage`;
- the static flag `NeedLoadLocalizations` and the code that reads it;
- the `EffectLocalization` class, when every caller has moved to the accessors.

Keep the class name only if other DOD code still refers to it while the migration is in progress.

## 7. The effect toggle texts

`NewMods/Newbe.Dod.Common/Logic/EffectToggleCatalog.cs` holds the toggle labels as literals. Move them into a `Locales`
source with keys under a pattern such as `effecttoggle.{code}.{label|hint}`, and read them with `L.Dynamic` or
`L.DynamicOr` (for the default). Keep `EffectToggleCatalog`'s public shape so its tests do not change:
`Tests/Newbe.Dod.Tests/EffectToggleCatalogTests.cs`.

## 8. Replace upsert.sh and check the translations

`NewMods/Newbe.Dod/Locales/upsert.sh` is an awk script. It splits rows on commas and breaks on quoted values. Replace
its uses with the SDK commands:

```sh
dotnet tools/net10.0/Ducky.Sdk.Build.dll locales set <Locales dir> <lang> <key> <value>
dotnet tools/net10.0/Ducky.Sdk.Build.dll locales sync <Locales dir>
dotnet tools/net10.0/Ducky.Sdk.Build.dll locales validate <Locales dir>
```

Run `locales validate` before the first build with `LocalizationValidation=Error`, so the case and placeholder problems
are fixed first. Then leave the build default, `Warn`, once the sub-mod is clean.

Tests that cover these commands: `Set_ValueWithCommaAndQuote_IsQuotedAndReadBackUnchanged`,
`Sync_FillsMissingKeysFromEnglish_AndKeepsExistingTranslations`, `Validate_ErrorSeverity_ExitsOneWhenAFindingIsAnError`,
`RoundTrip_SetThenSyncKeepsCommasQuotesAndLineBreaks`.

## 9. Per-sub-mod checklist and rollback

Move one sub-mod at a time. For each sub-mod:

- [ ] Register its `Locales` folder under its own `ModId`, and dispose the handle on unload.
- [ ] Declare `[KeyPattern]` or switch its lookups to `L.Dynamic` / `L.Scope(modId)`.
- [ ] Check keys for case collisions, and check placeholders, with `locales validate`.
- [ ] Replace `upsert.sh` uses with `locales set` and `locales sync`.
- [ ] Build in Debug and in Release with the default severity, and check that the warnings are only the missing translations you expect.
- [ ] Test a language switch and a hot reload in the game.

Rollback: each sub-mod's change is independent. To roll one back, restore the sub-mod's previous `Locales` code and
`WorldEffectLocalizationManager` usage from git, and keep the `Locales` CSV files as they are. The CSV files stay
readable by the old code, apart from the `@file:` rows, which the legacy rule still reads (see step 5).
