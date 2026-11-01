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
- The header row (`key`, any case) and blank rows are skipped.
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
