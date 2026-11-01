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
