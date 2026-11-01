using Ducky.Sdk.Localizations;

namespace Ducky.Sdk.Lib.Tests;

public class CsvParserTests
{
    [Test]
    public void ParseCsvLine_QuotedCommaStaysInOneField()
    {
        CsvParser.ParseCsvLine("a,\"b,c\",d").ShouldBe(new[] { "a", "b,c", "d" });
    }

    [Test]
    public void ParseCsvLine_EscapedQuoteBecomesOneQuote()
    {
        CsvParser.ParseCsvLine("\"say \"\"hi\"\"\",x").ShouldBe(new[] { "say \"hi\"", "x" });
    }

    [Test]
    public void ParseCsvLine_EmptyFieldsAreKept()
    {
        CsvParser.ParseCsvLine("a,,c,").ShouldBe(new[] { "a", "", "c", "" });
    }

    [Test]
    public void ParseCsvLine_EmptyLineIsOneEmptyField()
    {
        CsvParser.ParseCsvLine("").ShouldBe(new[] { "" });
    }

    [Test]
    public void ParseCsvToDictionary_ReadsKeyValuePairs()
    {
        var map = CsvParser.ParseCsvToDictionary("key,value\nhello,Hello\nbye,Bye\n");

        map.Count.ShouldBe(2);
        map["hello"].ShouldBe("Hello");
        map["bye"].ShouldBe("Bye");
    }

    [Test]
    public void ParseCsvToDictionary_SkipsTheHeaderRowCaseInsensitively()
    {
        var map = CsvParser.ParseCsvToDictionary("KEY,Value\nx,1");

        map.ContainsKey("KEY").ShouldBeFalse();
        map["x"].ShouldBe("1");
    }

    [Test]
    public void ParseCsvToDictionary_QuotedValueWithComma()
    {
        var map = CsvParser.ParseCsvToDictionary("k,\"one, two\"");

        map["k"].ShouldBe("one, two");
    }

    [Test]
    public void ParseCsvToDictionary_MalformedRowsWithOneColumnAreSkipped()
    {
        var map = CsvParser.ParseCsvToDictionary("lonely\n\n   \nok,1");

        map.Count.ShouldBe(1);
        map["ok"].ShouldBe("1");
    }

    [Test]
    public void ParseCsvToDictionary_LaterDuplicateKeyWins()
    {
        var map = CsvParser.ParseCsvToDictionary("k,first\nk,second");

        map["k"].ShouldBe("second");
    }

    [Test]
    public void ParseCsvToDictionary_NullOrEmptyContentGivesEmptyMap()
    {
        CsvParser.ParseCsvToDictionary(null!).Count.ShouldBe(0);
        CsvParser.ParseCsvToDictionary("").Count.ShouldBe(0);
    }

    [Test]
    public void ParseCsvToDictionary_TrimsKeysAndValues()
    {
        var map = CsvParser.ParseCsvToDictionary("  k  ,  v  ");

        map["k"].ShouldBe("v");
    }
}

public class CsvDocumentRulesTests
{
    [Test]
    public void EscapedQuote_IsReadAsOneQuote()
    {
        CsvParser.ParseCsvToDictionary("greeting,\"He said \"\"hi\"\"\"")["greeting"].ShouldBe("He said \"hi\"");
    }

    [Test]
    public void Multiline_QuotedValueKeepsItsLineBreaks()
    {
        var map = CsvParser.ParseCsvToDictionary("k,\"line one\nline two\"\nafter,ok");

        map["k"].ShouldBe("line one\nline two");
        map["after"].ShouldBe("ok");
    }

    [Test]
    public void EmbeddedComma_InsideQuotesStaysInValue()
    {
        CsvParser.ParseCsvToDictionary("k,\"a, b, c\"")["k"].ShouldBe("a, b, c");
    }

    [Test]
    public void Bom_IsIgnoredBeforeTheHeader()
    {
        var map = CsvParser.ParseCsvToDictionary("﻿Key,Value\nk,v");

        map.Count.ShouldBe(1);
        map["k"].ShouldBe("v");
    }

    [Test]
    public void CrlfAndLf_BothSplitRows()
    {
        var map = CsvParser.ParseCsvToDictionary("a,1\r\nb,2\nc,3\r\n");

        map["a"].ShouldBe("1");
        map["b"].ShouldBe("2");
        map["c"].ShouldBe("3");
    }

    [Test]
    public void HeaderAndBlankRows_AreSkipped()
    {
        var map = CsvParser.ParseCsvToDictionary("Key,Value\n\n\nx,1\n\n");

        map.Count.ShouldBe(1);
        map["x"].ShouldBe("1");
    }

    [Test]
    public void UntrimmedValueInsideQuotes_KeepsItsSpaces()
    {
        CsvParser.ParseCsvToDictionary("k,\"  spaced  \"")["k"].ShouldBe("  spaced  ");
    }

    [Test]
    public void WriteThenRead_RoundTripsCommasQuotesAndLineBreaks()
    {
        var text = CsvDocument.Serialize(new[]
        {
            new KeyValuePair<string, string>("a", "Say \"hi\", then leave"),
            new KeyValuePair<string, string>("b", "first\nsecond"),
            new KeyValuePair<string, string>("c", "  padded  "),
        });

        var map = CsvParser.ParseCsvToDictionary(text);

        map["a"].ShouldBe("Say \"hi\", then leave");
        map["b"].ShouldBe("first\nsecond");
        map["c"].ShouldBe("  padded  ");
    }

    [Test]
    public void Duplicate_ReportsOnceAndLaterWins()
    {
        var messages = new System.Collections.Generic.List<string>();
        var map = CsvDocument.ToDictionary("Menu.Title,first\nmenu.title,second", messages.Add);

        map["menu.title"].ShouldBe("second");
        messages.Count.ShouldBe(1);
    }
}
