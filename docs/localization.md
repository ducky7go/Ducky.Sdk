# Localization

This page describes how Ducky.Sdk reads translation files. It is updated section by section as the localization
work lands; the sections below cover the CSV format, canonical language codes and the single language list.

## CSV syntax

A translation file is an RFC 4180 CSV file with a `Key,Value` header:

```csv
Key,Value
greeting,"Hello, ""friend"""
menu.title,"Main
Menu"
```

- Quoted fields may contain commas, `""` (an escaped quote) and line breaks.
- A UTF-8 byte order mark is accepted, and both CRLF and LF line endings are read.
- A first row named `key` (any case) is the header and is skipped; blank rows are skipped. A later row may use `key` as a real key.
- Whitespace outside quotes is ignored; whitespace inside quotes is kept.
- The same reader and writer are used by the runtime, the build steps and the translation commands, so a file
  readable by one is readable by all.

Covered by tests in `CsvParserTests` (`CsvDocumentRulesTests`): escaped quote, multiline value, embedded comma, BOM,
CRLF and LF, header and blank rows, untrimmed value and write-then-read round trip.

## Canonical language codes

Every language code is normalized before it is used as a file name or a lookup key:

1. trim surrounding whitespace;
2. replace `_` with `-`;
3. lower-case the result;
4. apply the alias table.

| Written as | Canonical |
|---|---|
| `zh-Hant`, `zh_TW`, `ZH-HANT` | `zh-hant` |
| `zh-CN`, `zh-Hans`, `zh-SG` | `zh` |
| `zh-TW`, `zh-HK`, `zh-MO` | `zh-hant` |

So `Locales/zh-Hant.csv` is the `zh-hant` file. Names that are not language-code shaped (for example `missing.csv`)
are not languages.

Covered by `LanguageCodesTests`.

## The language list

The supported languages are defined once, in `LanguageCodes.AllLanguages`:

`de, en, es, fr, ja, ko, pt, ru, zh-hant, zh`

`[LanguageSupport("all")]`, the source generator and the build steps all read this list, so a change to it appears in
all of them. `[LanguageSupport("all", "zh_Hant")]` adds the normalized extra code after the list, without duplicates.

Covered by `LanguageCodesTests.AllLanguages_IsTheOneOrderedList` and
`LanguageCodesTests.LanguageSupportAll_ExpandsToTheSharedListAndNormalizesExtras`.

## Runtime lookup

Lookups use the current language and its fallback chain, and check the sources in registration order. A key is looked
up ignoring case.

- **Key comparison.** Keys match ignoring case, so `Menu.Title` in a file answers a lookup for `menu.title`. Two rows
  in one file that differ only by case: the later row wins and a warning is logged.
  Tests: `MixedCaseCsvKey_MatchesLookupInAnyCase`, `SameFileDuplicateKey_LaterRowWinsAndWarns`.
- **File markers.** A value is a file reference only when it starts with `@file:`. The file is read from the language
  folder `Locales/{lang}/`, and a file next to the CSV is accepted as well. Names that contain `/`, `\` or `..` are
  rejected with a warning (`rejected file reference`). Any other value is text, whatever it looks like.
  Tests: `FileMarker_ReadsTheReferencedFileFromTheLanguageFolder`, `PlainTextThatLooksLikeAFileName_StaysText`,
  `FileMarker_PathEscapeIsRejectedWithWarning`.
- **Legacy rows.** The exact value `{key}.md` or `{key}.txt` is still read as a file when that file exists in the
  language folder, with a deprecation warning. Rewrite these rows to `@file:{key}.{ext}`; `locales sync` and the build
  update step do this. Test: `LegacyExactRow_ReadsTheFileAndLogsDeprecation`.
- **Missing referenced file.** A marker whose file does not exist logs a warning that names the key, the language and
  the file. The entry is skipped for that language, so the fallback chain continues. Test:
  `MissingReferencedFile_WarnsAndFallsThroughToEnglish`.
- **Missing text.** When no language in the chain has text, `L.Get(key)` returns `[key]`, and `L.Get(key, fallback)`
  returns the fallback. `L.TryGet(key, out text)` returns false with an empty text and no placeholder. Tests:
  `UnknownKey_GivesBracketedKeyAndTryGetReportsAbsenceWithoutPlaceholder`, `Fallback_MissingKeyUsesTheFallbackOrABracketedKey`.
- **Language switch.** Switching languages publishes the new texts to the game and removes only the overrides the SDK
  set that the new language no longer supplies. An unchanged text is neither removed nor set again. The SDK never
  removes a key it did not set. Tests: `LanguageSwitch_RemovesOverridesOnlyTheOldLanguageHad`,
  `LanguageSwitch_UnchangedTextIsNotRemovedAndSetAgain`, `LanguageSwitch_NeverRemovesAKeyTheSdkDidNotSet`.

## Dynamic keys

`L.Dynamic(template, args...)` formats the template with invariant culture and looks the result up as `L.Get` does.

```csharp
var name = L.Dynamic("worldeffect.{0}.name", "FROST");
var nameOrDefault = L.DynamicOr("worldeffect.{0}.name", "Unknown", code);
if (L.TryDynamic("worldeffect.{0}.name", out var text, code)) { /* ... */ }
```

`L.DynamicOr` is the fallback form. It has its own name because a second `params` overload would make
`L.Dynamic("…", "FROST")` ambiguous. A bad template or a missing argument does not throw: it logs a warning and gives
`[template]` (or the fallback). Tests: `EffectCodeKey_ReturnsTheTextOfTheFormattedKey`,
`UnknownDynamicKey_GivesTheBracketedKeyOrTheFallback`, `CodeCase_DoesNotMatter`,
`MissingArgument_LogsAndGivesTheTemplateResultWithoutThrowing`, `TryDynamic_ReportsAbsenceWithoutAPlaceholder`.

## Sources

A mod has one default source: its own `Locales` folder. Other folders or custom providers are registered under a
`ModId`, and the returned handle removes the source (and the game texts it owns) when disposed.

```csharp
var handle = L.RegisterLocales(new ModId("sub.one"), Path.Combine(folder, "Locales"));
var scoped = L.Scope(new ModId("sub.one")).Get("title");
handle.Dispose(); // on unload or hot reload
```

- **Default source.** The `Locales` folder next to the assembly, under the mod's identity (`Helper.GetModId()`), or the
  assembly name when that is unavailable. Test: `DefaultSource_IsTheModLocalesFolderUnderTheDefaultModId`.
- **Registering.** A sub-mod folder or a custom `ILocalizationProvider` becomes available at once, and the current
  language is applied to it. A missing folder logs a warning and registers an empty source; `L.Reindex` picks the
  folder up later. Registering the same folder under the same `ModId` again is a no-op. Tests:
  `SubModDirectory_ItsTextsAreAvailableAndTheCurrentLanguageIsApplied`, `CustomProvider_IsAvailableLikeADirectorySource`,
  `MissingDirectory_LogsWarningRegistersEmptyAndCanBePickedUpByReindex`, `RegisteringTheSameDirectoryTwice_IsIdempotent`.
- **Scoping.** `L.Scope(modId)` sees only that `ModId`'s sources. An unscoped `L.Get` looks at the default source first,
  then the others in registration order. Chain languages come before sources: a `zh` text in a later source beats an
  `en` text in an earlier one. Tests: `SameKeyInTwoSources_ScopedLookupReturnsEachSourcesOwnText`,
  `UnscopedLookup_DefaultSourceWinsThenTheEarliestRegistered`.
- **Raw and prefixed game keys.** By default a source publishes its keys raw, which is what the game's own lookups
  expect. `KeyPrefix` adds a prefix (for example `dod.`) and is opt-in. Test: `PrefixedPublishing_PublishesTheKeyWithThePrefix`.
- **Ownership and collisions.** When two sources publish the same game key, the source that supplies the winning text
  in the chain owns it, earliest registered first, and one warning per pair is logged. Test:
  `Collision_OwnerTextIsPublishedAndOneWarningIsLogged`.
- **Unloading.** Disposing a handle removes the overrides that source owned and nothing else. A key the removed source
  shadowed comes back with the other source's text. Tests: `UnloadOneOfTwo_RemovesOnlyItsOverrides`,
  `ShadowedKey_ReturnsWithTheOtherSourcesTextWhenTheOwnerIsUnloaded`.
- **Release.** `ReleaseOverrideTexts` (hot reload) removes every override the SDK set, for every source. Test:
  `ReleaseOverrideTexts_RemovesEverySourceOverride`.
- **Language applies to every source.** Test: `LanguageSwitch_AppliesToEverySource`.

## Language index and fallback

When a folder is registered, the SDK enumerates it once and records which files belong to each language. Loading a
language opens only those files.

- `{lang}.csv` at the top level, and every `.csv` under `{lang}/`, are language files. Names that are not language codes
  (for example `missing.csv`, `notes.csv`) are ignored. Tests: `LanguageSetOfASource_IsEnFrAndZhHantFromCanonicalNames`,
  `FilesThatAreNotLanguages_AreIgnoredIncludingTheReportFile`.
- Loading a language does not enumerate the folder again. `L.Reindex(modId?)` picks up new files. Tests:
  `IndexBuiltOnRegistration_LoadingALanguageDoesNotEnumerateAgain`, `ReindexFindsANewFileWithoutRestarting`.
- **File order.** Within one language, `{lang}.csv` files come first, then the files under `{lang}/` in ordinal order.
  A later file wins for the same key. For two names with the same code (`zh-Hant.csv` and `zh-hant.csv`), the names are
  read in ordinal order and the later one wins. Tests: `TwoFilesDefiningOneKey_LaterFileInTheDeterministicOrderWins`,
  `SameCodeFromTwoNames_ZhHantFileOrderIsDeterministic`.
- **Default chain.** The language, then each shorter form (drop the last `-` part), then `en`, with no repeats. `zh-hant`
  gives `zh-hant, zh, en`; `pt-br` gives `pt-br, pt, en`; `en` gives `en`. Tests: `DefaultChain_IsTheLanguageShorterFormsThenEnglish`.
- **Configured chain.** `L.ConfigureFallback(language, codes...)` replaces the default. The chain is the language, the
  codes, then `en`. Codes that are not language codes, and repeated codes, are dropped with a warning. Tests:
  `CustomChain_ReplacesTheDefaultAndEndsWithEnglish`, `RepeatedCode_IsDroppedWithAWarning`,
  `ConfiguredChain_KoreanMissingTextFallsBackToJapanese`.
- **Lookup follows the chain.** The first non-empty text along the chain wins, and an empty value counts as absent.
  Tests: `ZhHantUser_ReceivesZhTextBeforeEnglish`, `EmptyValue_FallsThroughToTheNextLanguage`.
- **Partial translations reach the game.** Keys a language lacks are published with the text of the first language in
  its chain that has them. Test: `PartialTranslation_MissingKeysArePublishedFromTheChain`.
- **Lazy loading.** A language is loaded when the chain first needs it, and then cached per source. Test:
  `LazyLoad_OnlyTheLanguagesInTheChainAreLoaded`.

## Key patterns

Keys built from data (one key per world-effect code, for example) can be declared with `[KeyPattern]` on the `LK` class:

```csharp
[LanguageSupport("all")]
[KeyPattern("worldeffect.{code}.{name|summary|description}", Group = "WorldEffect")]
public static class LK { /* static keys */ }

// generated:
L.WorldEffect.Name(code)        // = L.Dynamic("worldeffect.{0}.name", code)
L.WorldEffect.Summary(code)
L.WorldEffect.Description(code)
```

- `{identifier}` is a runtime parameter; `{a|b|c}` is a choice list. A pattern has at most one choice list.
- A parameter in the last segment may contain dots; any other parameter matches one segment.
- Matching ignores case.
- An accessor is generated for each choice, named after it in PascalCase. A pattern without a choice list gets no
  accessor, but `L.Dynamic` still works with it.
- `Group` names the accessor class. When it is not set, the class is the first literal segment with its first letter
  upper-cased, so `worldeffect` gives `L.Worldeffect`. Set `Group = "WorldEffect"` for `L.WorldEffect`.
- An invalid pattern (unclosed brace, empty choice, two choice lists, empty pattern) reports `DUCKYL001` as an error,
  and no accessor is generated for it.
- The pattern is written to `lkeys.json` in a `patterns` array, which is omitted when there are none. Patterns do not
  add rows to the CSV files.

Tests: `KeyPatternSyntaxTests` (`{ident}`, `{a|b|c}`, last-segment rule, two choice lists, empty pattern),
`LocalizationGeneratorTests.KeyPattern_EmitsOneTypedAccessorPerChoiceOverDynamic`,
`LocalizationGeneratorTests.InvalidKeyPattern_ReportsDucky001AndEmitsNoAccessor`,
`LocalizationGeneratorTests.NoPattern_EmitsNoDynamicAccessorsAndNoPatternsKey`,
`ExtractLocalizationKeysLibTests.Execute_CarriesKeyPatternsIntoLkeysJson`.

## Build-time CSV updates

The build keeps `Locales/*.csv` in step with the `LK` keys.

- Rows whose key is a static key, or matches a `[KeyPattern]`, are kept. Any other row is obsolete and is removed.
  Key matching ignores case. Test: `Execute_KeepsRowsMatchedByAKeyPatternAndStillRemovesObsoleteRows`.
- A `[TranslateFile]` key is written as `@file:{key}.{ext}`, and the translation file is created in the language folder
  when it is missing. Old bare rows are rewritten to the marker. Test:
  `Execute_WritesFileReferenceKeysAsStandaloneFilesWithDefaultLanguages`.
- Rows are read and written with the same CSV rules as the runtime, so a multiline or quoted value survives a rewrite
  exactly. Test: `Execute_RewriteKeepsMultilineAndQuotedValuesExactly`.
- The existing golden output for `Ducky.TryHarmony` is unchanged. Test:
  `Execute_GeneratesLocaleCsvsAndHashMatchingBaselineGolden`.

## Build-time validation

Each build compares every language with the English baseline, after the CSV update step. The baseline is the static key
list of `lkeys.json` when that file exists, otherwise the English CSV. Validation works from the files alone, so it also
fits mods whose keys are built at runtime.

| Property | Values | Default | Applies to |
|---|---|---|---|
| `LocalizationValidation` | `Off`, `Warn`, `Error` | `Warn` | missing keys (absent or empty) and placeholder mismatches |
| `LocalizationExtraKeys` | `Ignore`, `Warn`, `Error` | `Warn` | keys a language has but the baseline does not |

```xml
<PropertyGroup>
  <LocalizationValidation>Error</LocalizationValidation>
  <LocalizationExtraKeys>Ignore</LocalizationExtraKeys>
</PropertyGroup>
```

- `Warn` prints `warning: [LocalizationValidation] ...` lines and the build succeeds. `Error` prints `error: ...` and
  fails the build. `Off` skips the check and writes no report.
- An unknown value gives the default and a warning.
- A key matched by a `[KeyPattern]` is never reported as extra.
- Placeholders are compared as sets of numbers, so `{0}` and `{1}` may appear in any order. `{{` and `}}` are not
  placeholders. Both comparisons ignore case for keys.
- The check never edits a language file. When something is missing, it writes `missing.csv` into the assets folder,
  next to `Locales` (not inside it), with the columns `Language,Key,English`, sorted by language then key. When nothing is
  missing, an old `missing.csv` is removed.
- A missing English baseline (no `lkeys.json` and no `en.csv`) gives a `BaselineMissing` finding and compares nothing.

Tests: `ValidateLocalizationLibTests` (`DirectoryWithoutLkeys_ComparesTheOtherLanguageWithEnglish`,
`NoEnglishFile_ReportsTheBaselineAndComparesNothing`, `AbsentKey_IsReportedAsMissingForThatLanguage`,
`EmptyValue_IsReportedAsMissing`, `DifferentCasing_CountsAsPresent`, `ObsoleteKey_IsReportedAsExtraOfThatLanguage`,
`PatternKey_IsNeverReportedAsExtra`, `MissingPlaceholder_IsReported`, `ReorderedPlaceholders_AreNotAMismatch`,
`EscapedBraces_AreNotPlaceholders`, `MissingReport_IsWrittenToAssetsNotLocales`, `AllTranslated_WritesNoReportAndRemovesAnOldOne`,
`DefaultSeverity_ReportsWarningsAndExitsZero`, `ErrorMode_FailsTheBuildWithTheKeyAndLanguage`,
`ExtraKeys_StayASeparateSeverity`, `Off_SkipsValidationAndWritesNoReport`, `SeverityChange_TakesEffectOnTheNextRun`).

## Translation commands

The build runner also has three commands for maintaining language files. They use the same CSV rules as the runtime,
so commas, quotes and line breaks in values are kept. A file is rewritten only when its content changes, and writes go
through a temp file.

```sh
dotnet tools/net10.0/Ducky.Sdk.Build.dll locales set <Locales dir> <lang> <key> <value>
dotnet tools/net10.0/Ducky.Sdk.Build.dll locales set <Locales dir> <key> --lang <lang>=<value> --lang <lang>=<value>
dotnet tools/net10.0/Ducky.Sdk.Build.dll locales sync <Locales dir> [--fill english|empty] [--prune] [--dry-run]
dotnet tools/net10.0/Ducky.Sdk.Build.dll locales validate <Locales dir>... [--validation Off|Warn|Error] [--extra-keys Ignore|Warn|Error]
```

- `set` creates the key or the language file when needed, keeps the file sorted by key, and creates a new language file
  with a `Key,Value` header. The multi form sets one key in several languages in one call.
- `sync` gives every language file every key of `en.csv`. By default a missing key gets the English text; `--fill empty`
  leaves it empty so that validation still reports it. Existing translations are never overwritten. Keys the English file
  lacks are kept unless `--prune` is given, and a key matched by a pattern in `../lkeys.json` is never pruned. `--dry-run`
  lists the changes and writes nothing. `@file:` rows are kept as written.
- `validate` runs the same check as the build, for each folder given, and exits with 1 when a finding is an error.
  It writes `missing.csv` into the folder above each Locales folder.
- Bad arguments print the usage and exit with 2. The build usage (`<build-context.json> --phase pre|post`) is unchanged.

Tests: `LocaleToolTests.Set_ValueWithCommaAndQuote_IsQuotedAndReadBackUnchanged` (`set`),
`SetMultiForm_SetsOneKeyInSeveralLanguagesInOneCall` (multi-form `set`),
`Sync_FillsMissingKeysFromEnglish_AndKeepsExistingTranslations` (`sync`),
`Sync_EmptyFill_LeavesNewKeysEmptySoValidationStillSeesThem` (`sync --fill empty`),
`Sync_Prune_RemovesKeysEnglishLacksUnlessAPatternMatches` (`sync --prune`),
`Sync_DryRun_ChangesNoFile` (`sync --dry-run`),
`Validate_SeveralSubModFolders_ChecksEachAndExitsZeroWithoutErrors` (`validate` with two folders),
`Validate_ErrorSeverity_ExitsOneWhenAFindingIsAnError` (`validate --validation Error`),
`RoundTrip_SetThenSyncKeepsCommasQuotesAndLineBreaks` and `RoundTrip_SyncOfAnAlreadySyncedFileIsByteIdentical` (round trip),
`NoCommand_PrintsUsageAndExitsTwo` and `SetWithoutAKey_PrintsUsageAndExitsTwo` (usage and exit 2).

## Not in this version

- **Other file formats.** JSON, resx and po are a long-term evaluation. They are not implemented. The CSV format stays
  the only one, and the shared reader and writer are the place a second format would plug in.
- **Plural forms.** Not implemented. A translation is one text per key.
- **Translation context and translation comments.** Not implemented. There is no per-key note for translators.
