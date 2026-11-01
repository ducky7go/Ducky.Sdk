using System.ComponentModel;
using System.Collections.Generic;
using Ducky.Sdk.Utils;
using Newtonsoft.Json;

namespace Ducky.Sdk.Lib.Tests;

public class ModIdTests
{
    private sealed class Holder
    {
        public ModId Id { get; set; }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_RejectsNullOrWhitespace(string? value) =>
        Should.Throw<ArgumentException>(() => new ModId(value!));

    [Test]
    public void ImplicitConversions_GoBothWays()
    {
        ModId id = "steam.123";
        string text = id;

        text.ShouldBe("steam.123");
        id.Value.ShouldBe("steam.123");
    }

    [Test]
    public void IsEmpty_TrueForDefaultAndWhitespaceFalseForValue()
    {
        default(ModId).IsEmpty.ShouldBeTrue();
        new ModId("local.a").IsEmpty.ShouldBeFalse();
    }

    [Test]
    public void FromStringOrDefault_ReturnsDefaultForBlankInput()
    {
        ModId.FromStringOrDefault(null).IsEmpty.ShouldBeTrue();
        ModId.FromStringOrDefault("  ").IsEmpty.ShouldBeTrue();
        ModId.FromStringOrDefault("local.a").Value.ShouldBe("local.a");
    }

    [Test]
    public void JsonValue_SerializesAsPlainString_AndBack()
    {
        var json = JsonConvert.SerializeObject(new Holder { Id = new ModId("steam.42") });

        json.ShouldBe("{\"Id\":\"steam.42\"}");
        JsonConvert.DeserializeObject<Holder>(json)!.Id.Value.ShouldBe("steam.42");
    }

    [Test]
    public void JsonDictionaryKey_RoundTrips()
    {
        var map = new Dictionary<ModId, int> { [new ModId("local.a")] = 3 };

        var json = JsonConvert.SerializeObject(map);
        var back = JsonConvert.DeserializeObject<Dictionary<ModId, int>>(json)!;

        json.ShouldBe("{\"local.a\":3}");
        back[new ModId("local.a")].ShouldBe(3);
    }

    [Test]
    public void TypeConverter_ConvertsFromAndToString()
    {
        var converter = TypeDescriptor.GetConverter(typeof(ModId));

        converter.CanConvertFrom(typeof(string)).ShouldBeTrue();
        converter.CanConvertTo(typeof(string)).ShouldBeTrue();
        ((ModId)converter.ConvertFrom("local.b")!).Value.ShouldBe("local.b");
        converter.ConvertTo(new ModId("local.c"), typeof(string)).ShouldBe("local.c");
    }
}
