using System;
using System.Collections.Generic;
using System.IO;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Options;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// 测试 ModOptions 中时间类型的处理
/// </summary>
[TestFixture]
public class ModOptionsTests
{
    private string _testDirectory = null!;
    private ModOptions _modOptions = null!;
    private IModOptionsStorage _storage = null!;

    [SetUp]
    public void SetUp()
    {
        Log.Current = new TestConsoleLogger();
        // 创建一个临时测试目录
        _testDirectory = Path.Combine(Path.GetTempPath(), "DuckySdkTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // 创建一个测试用的 ModOptions 实例，使用自定义路径
        var configPath = Path.Combine(_testDirectory, "testconfig.json");
        var folderPath = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(folderPath) && !Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        _storage = new InMemoryModOptionsStorage();
        _modOptions = new ModOptions(() => configPath, _storage);
    }

    [TearDown]
    public void TearDown()
    {
        // 清理测试目录
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Test]
    public void SaveAndLoadDateTime_ShouldConvertToUnixTimestamp()
    {
        // Arrange
        var key = "test_datetime";
        var originalDateTime = new DateTime(2023, 6, 15, 12, 30, 45, DateTimeKind.Utc);
        var expectedUnixTimestamp = ((DateTimeOffset)originalDateTime).ToUnixTimeSeconds();

        // Act
        var saveResult = _modOptions.SaveConfig(key, originalDateTime);
        var loadedDateTime = _modOptions.LoadConfig<DateTime>(key);

        // Assert
        saveResult.ShouldBeTrue();
        loadedDateTime.ShouldBe(originalDateTime);

        // 验证底层存储的是 long 类型
        var loadedUnixTimestamp = _modOptions.LoadConfig<long>(key);
        loadedUnixTimestamp.ShouldBe(expectedUnixTimestamp);
    }

    [Test]
    public void SaveAndLoadDateTimeOffset_ShouldConvertToUnixTimestamp()
    {
        // Arrange
        var key = "test_datetimeoffset";
        var originalDateTimeOffset = new DateTimeOffset(2023, 6, 15, 12, 30, 45, TimeSpan.FromHours(8));
        var expectedUnixTimestamp = originalDateTimeOffset.ToUnixTimeSeconds();

        // Act
        var saveResult = _modOptions.SaveConfig(key, originalDateTimeOffset);
        var loadedDateTimeOffset = _modOptions.LoadConfig<DateTimeOffset>(key);

        // Assert
        saveResult.ShouldBeTrue();
        loadedDateTimeOffset.ShouldBe(originalDateTimeOffset);

        // 验证底层存储的是 long 类型
        var loadedUnixTimestamp = _modOptions.LoadConfig<long>(key);
        loadedUnixTimestamp.ShouldBe(expectedUnixTimestamp);
    }

    [Test]
    public void SaveAndLoadNullableDateTime_WithNullValue_ShouldStoreAsZero()
    {
        // Arrange
        var key = "test_nullable_datetime_null";
        DateTime? originalDateTime = null;

        // Act
        var saveResult = _modOptions.SaveConfig(key, originalDateTime);
        var loadedDateTime = _modOptions.LoadConfig<DateTime?>(key);

        // Assert
        saveResult.ShouldBeTrue();
        loadedDateTime.ShouldBeNull();

        // 验证底层存储的是 0
        var loadedUnixTimestamp = _modOptions.LoadConfig<long>(key);
        loadedUnixTimestamp.ShouldBe(0);
    }

    [Test]
    public void SaveAndLoadNullableDateTimeOffset_WithNullValue_ShouldStoreAsZero()
    {
        // Arrange
        var key = "test_nullable_datetimeoffset_null";
        DateTimeOffset? originalDateTimeOffset = null;

        // Act
        var saveResult = _modOptions.SaveConfig(key, originalDateTimeOffset);
        var loadedDateTimeOffset = _modOptions.LoadConfig<DateTimeOffset?>(key);

        // Assert
        saveResult.ShouldBeTrue();
        loadedDateTimeOffset.ShouldBeNull();

        // 验证底层存储的是 0
        var loadedUnixTimestamp = _modOptions.LoadConfig<long>(key);
        loadedUnixTimestamp.ShouldBe(0);
    }

    [Test]
    public void SaveAndLoadNullableDateTime_WithValue_ShouldConvertToUnixTimestamp()
    {
        // Arrange
        var key = "test_nullable_datetime_value";
        var originalDateTime = new DateTime(2023, 6, 15, 12, 30, 45, DateTimeKind.Utc);
        DateTime? originalNullableDateTime = originalDateTime;
        var expectedUnixTimestamp = ((DateTimeOffset)originalDateTime).ToUnixTimeSeconds();

        // Act
        var saveResult = _modOptions.SaveConfig(key, originalNullableDateTime);
        var loadedDateTime = _modOptions.LoadConfig<DateTime?>(key);

        // Assert
        saveResult.ShouldBeTrue();
        loadedDateTime.ShouldBe(originalNullableDateTime);

        // 验证底层存储的是 long 类型
        var loadedUnixTimestamp = _modOptions.LoadConfig<long>(key);
        loadedUnixTimestamp.ShouldBe(expectedUnixTimestamp);
    }

    [Test]
    public void SaveAndLoadNullableDateTimeOffset_WithValue_ShouldConvertToUnixTimestamp()
    {
        // Arrange
        var key = "test_nullable_datetimeoffset_value";
        var originalDateTimeOffset = new DateTimeOffset(2023, 6, 15, 12, 30, 45, TimeSpan.FromHours(8));
        DateTimeOffset? originalNullableDateTimeOffset = originalDateTimeOffset;
        var expectedUnixTimestamp = originalDateTimeOffset.ToUnixTimeSeconds();

        // Act
        var saveResult = _modOptions.SaveConfig(key, originalNullableDateTimeOffset);
        var loadedDateTimeOffset = _modOptions.LoadConfig<DateTimeOffset?>(key);

        // Assert
        saveResult.ShouldBeTrue();
        loadedDateTimeOffset.ShouldBe(originalNullableDateTimeOffset);

        // 验证底层存储的是 long 类型
        var loadedUnixTimestamp = _modOptions.LoadConfig<long>(key);
        loadedUnixTimestamp.ShouldBe(expectedUnixTimestamp);
    }

    [Test]
    public void LoadDateTime_WithZeroValue_ShouldReturnMinValue()
    {
        // Arrange
        var key = "test_datetime_zero";
        _modOptions.SaveConfig(key, 0L);

        // Act
        var loadedDateTime = _modOptions.LoadConfig<DateTime>(key);

        // Assert
        loadedDateTime.ShouldBe(DateTime.MinValue);
    }

    [Test]
    public void LoadDateTimeOffset_WithZeroValue_ShouldReturnMinValue()
    {
        // Arrange
        var key = "test_datetimeoffset_zero";
        _modOptions.SaveConfig(key, 0L);

        // Act
        var loadedDateTimeOffset = _modOptions.LoadConfig<DateTimeOffset>(key);

        // Assert
        loadedDateTimeOffset.ShouldBe(DateTimeOffset.MinValue);
    }

    [Test]
    public void LoadNullableDateTime_WithZeroValue_ShouldReturnNull()
    {
        // Arrange
        var key = "test_nullable_datetime_zero";
        _modOptions.SaveConfig(key, 0L);

        // Act
        var loadedDateTime = _modOptions.LoadConfig<DateTime?>(key);

        // Assert
        loadedDateTime.ShouldBeNull();
    }

    [Test]
    public void LoadNullableDateTimeOffset_WithZeroValue_ShouldReturnNull()
    {
        // Arrange
        var key = "test_nullable_datetimeoffset_zero";
        _modOptions.SaveConfig(key, 0L);

        // Act
        var loadedDateTimeOffset = _modOptions.LoadConfig<DateTimeOffset?>(key);

        // Assert
        loadedDateTimeOffset.ShouldBeNull();
    }

    [Test]
    public void LoadDateTime_WithInvalidValue_ShouldReturnDefaultValue()
    {
        // Arrange
        var key = "test_datetime_invalid";
        var defaultValue = new DateTime(2020, 1, 1);
        _modOptions.SaveConfig(key, "invalid_timestamp");

        // Act
        var result = _modOptions.LoadConfig(key, defaultValue);

        // Assert
        result.ShouldBe(defaultValue);
    }

    [Test]
    public void LoadDateTimeOffset_WithInvalidValue_ShouldReturnDefaultValue()
    {
        // Arrange
        var key = "test_datetimeoffset_invalid";
        var defaultValue = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _modOptions.SaveConfig(key, "invalid_timestamp");

        // Act
        var result = _modOptions.LoadConfig(key, defaultValue);

        // Assert
        result.ShouldBe(defaultValue);
    }

    [Test]
    public void LoadNullableDateTime_WithInvalidValue_ShouldReturnDefaultValue()
    {
        // Arrange
        var key = "test_nullable_datetime_invalid";
        DateTime? defaultValue = new DateTime(2020, 1, 1);
        _modOptions.SaveConfig(key, "invalid_timestamp");

        // Act
        var result = _modOptions.LoadConfig(key, defaultValue);

        // Assert
        result.ShouldBe(defaultValue);
    }

    [Test]
    public void LoadNullableDateTimeOffset_WithInvalidValue_ShouldReturnDefaultValue()
    {
        // Arrange
        var key = "test_nullable_datetimeoffset_invalid";
        DateTimeOffset? defaultValue = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _modOptions.SaveConfig(key, "invalid_timestamp");

        // Act
        var result = _modOptions.LoadConfig(key, defaultValue);

        // Assert
        result.ShouldBe(defaultValue);
    }

    #region 内部方法测试

    [Test]
    public void IsDateTimeType_WithDateTime_ShouldReturnTrue()
    {
        // Act
        var result = Ducky.Sdk.Options.ModOptions.IsDateTimeType(typeof(DateTime));

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public void IsDateTimeType_WithDateTimeOffset_ShouldReturnTrue()
    {
        // Act
        var result = Ducky.Sdk.Options.ModOptions.IsDateTimeType(typeof(DateTimeOffset));

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public void IsDateTimeType_WithNullableDateTime_ShouldReturnTrue()
    {
        // Act
        var result = Ducky.Sdk.Options.ModOptions.IsDateTimeType(typeof(DateTime?));

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public void IsDateTimeType_WithNullableDateTimeOffset_ShouldReturnTrue()
    {
        // Act
        var result = Ducky.Sdk.Options.ModOptions.IsDateTimeType(typeof(DateTimeOffset?));

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public void IsDateTimeType_WithNonDateTimeType_ShouldReturnFalse()
    {
        // Act & Assert
        Ducky.Sdk.Options.ModOptions.IsDateTimeType(typeof(int)).ShouldBeFalse();
        Ducky.Sdk.Options.ModOptions.IsDateTimeType(typeof(string)).ShouldBeFalse();
        Ducky.Sdk.Options.ModOptions.IsDateTimeType(typeof(TimeSpan)).ShouldBeFalse();
        Ducky.Sdk.Options.ModOptions.IsDateTimeType(typeof(Guid)).ShouldBeFalse();
    }

    [Test]
    public void ConvertToUnixTimestamp_WithDateTime_ShouldReturnCorrectTimestamp()
    {
        // Arrange
        var dateTime = new DateTime(2023, 6, 15, 12, 30, 45, DateTimeKind.Utc);
        var expectedTimestamp = ((DateTimeOffset)dateTime).ToUnixTimeSeconds();

        // Act
        var result = Ducky.Sdk.Options.ModOptions.ConvertToUnixTimestamp(dateTime);

        // Assert
        result.ShouldBe(expectedTimestamp);
    }

    [Test]
    public void ConvertToUnixTimestamp_WithDateTimeOffset_ShouldReturnCorrectTimestamp()
    {
        // Arrange
        var dateTimeOffset = new DateTimeOffset(2023, 6, 15, 12, 30, 45, TimeSpan.FromHours(8));
        var expectedTimestamp = dateTimeOffset.ToUnixTimeSeconds();

        // Act
        var result = Ducky.Sdk.Options.ModOptions.ConvertToUnixTimestamp(dateTimeOffset);

        // Assert
        result.ShouldBe(expectedTimestamp);
    }

    [Test]
    public void ConvertToUnixTimestamp_WithNullValue_ShouldReturnZero()
    {
        // Act
        var result = Ducky.Sdk.Options.ModOptions.ConvertToUnixTimestamp((object)null!);

        // Assert
        result.ShouldBe(0);
    }

    [Test]
    public void ConvertFromUnixTimestamp_WithZeroToDateTime_ShouldReturnMinValue()
    {
        // Act
        var result = Ducky.Sdk.Options.ModOptions.ConvertFromUnixTimestamp(typeof(DateTime), 0);

        // Assert
        result.ShouldBe(DateTime.MinValue);
    }

    [Test]
    public void ConvertFromUnixTimestamp_WithZeroToDateTimeOffset_ShouldReturnMinValue()
    {
        // Act
        var result = Ducky.Sdk.Options.ModOptions.ConvertFromUnixTimestamp(typeof(DateTimeOffset), 0);

        // Assert
        result.ShouldBe(DateTimeOffset.MinValue);
    }

    [Test]
    public void ConvertFromUnixTimestamp_WithZeroToNullableDateTime_ShouldReturnNull()
    {
        // Act
        var result = Ducky.Sdk.Options.ModOptions.ConvertFromUnixTimestamp(typeof(DateTime?), 0);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void ConvertFromUnixTimestamp_WithZeroToNullableDateTimeOffset_ShouldReturnNull()
    {
        // Act
        var result = Ducky.Sdk.Options.ModOptions.ConvertFromUnixTimestamp(typeof(DateTimeOffset?), 0);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void ConvertFromUnixTimestamp_WithValidValueToDateTime_ShouldReturnCorrectDateTime()
    {
        // Arrange
        var timestamp = 1686832245L; // 2023-06-15 12:30:45 UTC
        var expectedDateTime = new DateTime(2023, 6, 15, 12, 30, 45, DateTimeKind.Utc);

        // Act
        var result = Ducky.Sdk.Options.ModOptions.ConvertFromUnixTimestamp(typeof(DateTime), timestamp);

        // Assert
        result.ShouldBe(expectedDateTime);
    }

    [Test]
    public void ConvertFromUnixTimestamp_WithValidValueToDateTimeOffset_ShouldReturnCorrectDateTimeOffset()
    {
        // Arrange
        var timestamp = 1686832245L; // 2023-06-15 12:30:45 UTC
        var expectedDateTimeOffset = new DateTimeOffset(2023, 6, 15, 12, 30, 45, TimeSpan.Zero);

        // Act
        var result = Ducky.Sdk.Options.ModOptions.ConvertFromUnixTimestamp(typeof(DateTimeOffset), timestamp);

        // Assert
        result.ShouldBe(expectedDateTimeOffset);
    }

    [Test]
    public void ConvertFromUnixTimestamp_WithValidValueToNullableDateTime_ShouldReturnCorrectDateTime()
    {
        // Arrange
        var timestamp = 1686832245L; // 2023-06-15 12:30:45 UTC
        var expectedDateTime = new DateTime(2023, 6, 15, 12, 30, 45, DateTimeKind.Utc);

        // Act
        var result = Ducky.Sdk.Options.ModOptions.ConvertFromUnixTimestamp(typeof(DateTime?), timestamp);

        // Assert
        result.ShouldBe(expectedDateTime);
    }

    [Test]
    public void ConvertFromUnixTimestamp_WithValidValueToNullableDateTimeOffset_ShouldReturnCorrectDateTimeOffset()
    {
        // Arrange
        var timestamp = 1686832245L; // 2023-06-15 12:30:45 UTC
        var expectedDateTimeOffset = new DateTimeOffset(2023, 6, 15, 12, 30, 45, TimeSpan.Zero);

        // Act
        var result = Ducky.Sdk.Options.ModOptions.ConvertFromUnixTimestamp(typeof(DateTimeOffset?), timestamp);

        // Assert
        result.ShouldBe(expectedDateTimeOffset);
    }

    [Test]
    public void SaveAndLoadListString_ShouldConvertToJson()
    {
        // Arrange
        var key = "test_list_string";
        var originalList = new List<string> { "apple", "banana", "cherry", "123", "测试中文" };

        // Act
        var saveResult = _modOptions.SaveConfig(key, originalList);
        var loadedList = _modOptions.LoadConfig<List<string>>(key);

        // Assert
        saveResult.ShouldBeTrue();
        loadedList.ShouldNotBeNull();
        loadedList.Count.ShouldBe(originalList.Count);

        for (int i = 0; i < originalList.Count; i++)
        {
            loadedList[i].ShouldBe(originalList[i]);
        }

        // 验证底层存储的是 JSON 字符串
        var storedJson = _modOptions.LoadConfig<string>(key);
        storedJson.ShouldNotBeNull();
        storedJson.ShouldStartWith("[");
        storedJson.ShouldEndWith("]");
    }

    [Test]
    public void SaveAndLoadEmptyListString_ShouldWork()
    {
        // Arrange
        var key = "test_empty_list_string";
        var originalList = new List<string>();

        // Act
        var saveResult = _modOptions.SaveConfig(key, originalList);
        var loadedList = _modOptions.LoadConfig<List<string>>(key);

        // Assert
        saveResult.ShouldBeTrue();
        loadedList.ShouldNotBeNull();
        loadedList.Count.ShouldBe(0);

        // 验证底层存储的是 "[]"
        var storedJson = _modOptions.LoadConfig<string>(key);
        storedJson.ShouldBe("[]");
    }

    [Test]
    public void SaveAndLoadListStringWithSpecialCharacters_ShouldWork()
    {
        // Arrange
        var key = "test_list_special_chars";
        var originalList = new List<string>
        {
            "normal string",
            "string with \"quotes\"",
            "string with 'single quotes'",
            "string with \n newline",
            "string with \t tab",
            "string with \\ backslash",
            "string with {braces}",
            "string with [brackets]",
            "Unicode: 你好世界 🌍",
            "emoji: 🍎🍌🍒"
        };

        // Act
        var saveResult = _modOptions.SaveConfig(key, originalList);
        var loadedList = _modOptions.LoadConfig<List<string>>(key);

        // Assert
        saveResult.ShouldBeTrue();
        loadedList.ShouldNotBeNull();
        loadedList.Count.ShouldBe(originalList.Count);

        for (int i = 0; i < originalList.Count; i++)
        {
            loadedList[i].ShouldBe(originalList[i]);
        }
    }

    [Test]
    public void SaveAndLoadArrayTypes_ShouldConvertToJson()
    {
        // Arrange
        var stringKey = "test_string_array";
        var intKey = "test_int_array";
        var originalStringArray = new[] { "one", "two", "three" };
        var originalIntArray = new[] { 1, 2, 3, 4, 5 };

        // Act
        var stringSaveResult = _modOptions.SaveConfig(stringKey, originalStringArray);
        var intSaveResult = _modOptions.SaveConfig(intKey, originalIntArray);
        var loadedStringArray = _modOptions.LoadConfig<string[]>(stringKey);
        var loadedIntArray = _modOptions.LoadConfig<int[]>(intKey);

        // Assert - String array
        stringSaveResult.ShouldBeTrue();
        loadedStringArray.ShouldNotBeNull();
        loadedStringArray.Length.ShouldBe(originalStringArray.Length);
        for (int i = 0; i < originalStringArray.Length; i++)
        {
            loadedStringArray[i].ShouldBe(originalStringArray[i]);
        }

        // Assert - Int array
        intSaveResult.ShouldBeTrue();
        loadedIntArray.ShouldNotBeNull();
        loadedIntArray.Length.ShouldBe(originalIntArray.Length);
        for (int i = 0; i < originalIntArray.Length; i++)
        {
            loadedIntArray[i].ShouldBe(originalIntArray[i]);
        }

        // 验证底层存储的是 JSON 字符串
        var storedStringJson = _modOptions.LoadConfig<string>(stringKey);
        var storedIntJson = _modOptions.LoadConfig<string>(intKey);
        storedStringJson.ShouldStartWith("[");
        storedStringJson.ShouldEndWith("]");
        storedIntJson.ShouldStartWith("[");
        storedIntJson.ShouldEndWith("]");
    }

    [Test]
    public void VerifyListStringStoredAsJsonString_ShouldPass()
    {
        // Arrange
        var key = "test_json_storage";
        var originalList = new List<string> { "verify", "json", "storage" };

        // Act
        var saveResult = _modOptions.SaveConfig(key, originalList);

        // 验证底层存储
        var storedRawData = _storage.Load<object>(key, _modOptions.GetConfigFilePath());
        var storedJson = _modOptions.LoadConfig<string>(key);
        var loadedList = _modOptions.LoadConfig<List<string>>(key);

        // Assert
        saveResult.ShouldBeTrue();

        // 底层应该存储为 JSON 字符串
        storedRawData.ShouldBeOfType<string>();
        storedJson.ShouldNotBeNull();
        storedJson.ShouldContain("verify");
        storedJson.ShouldContain("json");
        storedJson.ShouldContain("storage");

        // 能够正确反序列化
        loadedList.ShouldNotBeNull();
        loadedList.Count.ShouldBe(3);
        loadedList[0].ShouldBe("verify");
        loadedList[1].ShouldBe("json");
        loadedList[2].ShouldBe("storage");
    }

    [Test]
    public void SaveAndLoadDictionary_ShouldConvertToJson()
    {
        // Arrange
        var key = "test_dictionary";
        var originalDict = new Dictionary<string, int>
        {
            { "apple", 1 },
            { "banana", 2 },
            { "cherry", 3 }
        };

        // Act
        var saveResult = _modOptions.SaveConfig(key, originalDict);
        var loadedDict = _modOptions.LoadConfig<Dictionary<string, int>>(key);

        // Assert
        saveResult.ShouldBeTrue();
        loadedDict.ShouldNotBeNull();
        loadedDict.Count.ShouldBe(originalDict.Count);

        foreach (var kvp in originalDict)
        {
            loadedDict.ContainsKey(kvp.Key).ShouldBeTrue();
            loadedDict[kvp.Key].ShouldBe(kvp.Value);
        }

        // 验证底层存储的是 JSON 字符串
        var storedJson = _modOptions.LoadConfig<string>(key);
        storedJson.ShouldNotBeNull();
        storedJson.ShouldStartWith("{");
        storedJson.ShouldEndWith("}");
    }

    #endregion

    #region 默认值验证测试

    [Test]
    public void LoadConfig_WithMissingKeyAndSimpleTypeDefaultValue_ShouldValidateAndSave()
    {
        // Arrange
        var key = "test_missing_simple";
        var defaultValue = 42;

        // Act
        var result = _modOptions.LoadConfig<int>(key, defaultValue);

        // Assert
        result.ShouldBe(defaultValue);

        // 验证默认值已经被正确保存
        var savedValue = _modOptions.LoadConfig<int>(key);
        savedValue.ShouldBe(defaultValue);
    }

    [Test]
    public void LoadConfig_WithMissingKeyAndStringDefaultValue_ShouldValidateAndSave()
    {
        // Arrange
        var key = "test_missing_string";
        var defaultValue = "default string value";

        // Act
        var result = _modOptions.LoadConfig<string>(key, defaultValue);

        // Assert
        result.ShouldBe(defaultValue);

        // 验证默认值已经被正确保存
        var savedValue = _modOptions.LoadConfig<string>(key);
        savedValue.ShouldBe(defaultValue);
    }

    [Test]
    public void LoadConfig_WithMissingKeyAndBoolDefaultValue_ShouldValidateAndSave()
    {
        // Arrange
        var key = "test_missing_bool";
        var defaultValue = true;

        // Act
        var result = _modOptions.LoadConfig<bool>(key, defaultValue);

        // Assert
        result.ShouldBe(defaultValue);

        // 验证默认值已经被正确保存
        var savedValue = _modOptions.LoadConfig<bool>(key);
        savedValue.ShouldBe(defaultValue);
    }

    [Test]
    public void LoadConfig_WithMissingKeyAndDateTimeDefaultValue_ShouldConvertToUnixTimestamp()
    {
        // Arrange
        var key = "test_missing_datetime";
        var defaultValue = new DateTime(2023, 6, 15, 12, 30, 45, DateTimeKind.Utc);
        var expectedUnixTimestamp = ((DateTimeOffset)defaultValue).ToUnixTimeSeconds();

        // Act
        var result = _modOptions.LoadConfig<DateTime>(key, defaultValue);

        // Assert
        result.ShouldBe(defaultValue);

        // 验证底层存储的是 Unix 时间戳
        var savedUnixTimestamp = _modOptions.LoadConfig<long>(key);
        savedUnixTimestamp.ShouldBe(expectedUnixTimestamp);
    }

    [Test]
    public void LoadConfig_WithMissingKeyAndDateTimeOffsetDefaultValue_ShouldConvertToUnixTimestamp()
    {
        // Arrange
        var key = "test_missing_datetimeoffset";
        var defaultValue = new DateTimeOffset(2023, 6, 15, 12, 30, 45, TimeSpan.FromHours(8));
        var expectedUnixTimestamp = defaultValue.ToUnixTimeSeconds();

        // Act
        var result = _modOptions.LoadConfig<DateTimeOffset>(key, defaultValue);

        // Assert
        result.ShouldBe(defaultValue);

        // 验证底层存储的是 Unix 时间戳
        var savedUnixTimestamp = _modOptions.LoadConfig<long>(key);
        savedUnixTimestamp.ShouldBe(expectedUnixTimestamp);
    }

    [Test]
    public void LoadConfig_WithMissingKeyAndNullableDateTimeDefaultValue_ShouldConvertToUnixTimestamp()
    {
        // Arrange
        var key = "test_missing_nullable_datetime";
        DateTime? defaultValue = new DateTime(2023, 6, 15, 12, 30, 45, DateTimeKind.Utc);
        var expectedUnixTimestamp = ((DateTimeOffset)defaultValue.Value).ToUnixTimeSeconds();

        // Act
        var result = _modOptions.LoadConfig<DateTime?>(key, defaultValue);

        // Assert
        result.ShouldBe(defaultValue);

        // 验证底层存储的是 Unix 时间戳
        var savedUnixTimestamp = _modOptions.LoadConfig<long>(key);
        savedUnixTimestamp.ShouldBe(expectedUnixTimestamp);
    }

    [Test]
    public void LoadConfig_WithMissingKeyAndComplexTypeDefaultValue_ShouldSerializeToJson()
    {
        // Arrange
        var key = "test_missing_complex";
        var defaultValue = new TestComplexObject
        {
            Name = "Test Object",
            Value = 123,
            IsActive = true
        };

        // Act
        var result = _modOptions.LoadConfig<TestComplexObject>(key, defaultValue);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(defaultValue.Name);
        result.Value.ShouldBe(defaultValue.Value);
        result.IsActive.ShouldBe(defaultValue.IsActive);

        // 验证底层存储的是 JSON 字符串
        var savedJson = _modOptions.LoadConfig<string>(key);
        savedJson.ShouldNotBeNull();
        savedJson.ShouldContain("Test Object");
        savedJson.ShouldContain("123");
    }

    [Test]
    public void LoadConfig_WithMissingKeyAndListDefaultValue_ShouldSerializeToJson()
    {
        // Arrange
        var key = "test_missing_list";
        var defaultValue = new List<string> { "item1", "item2", "item3" };

        // Act
        var result = _modOptions.LoadConfig<List<string>>(key, defaultValue);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(defaultValue.Count);
        for (int i = 0; i < defaultValue.Count; i++)
        {
            result[i].ShouldBe(defaultValue[i]);
        }

        // 验证底层存储的是 JSON 字符串
        var savedJson = _modOptions.LoadConfig<string>(key);
        savedJson.ShouldNotBeNull();
        savedJson.ShouldStartWith("[");
        savedJson.ShouldEndWith("]");
    }

    [Test]
    public void LoadConfig_WithMissingKeyAndArrayDefaultValue_ShouldSerializeToJson()
    {
        // Arrange
        var key = "test_missing_array";
        var defaultValue = new[] { "array1", "array2", "array3" };

        // Act
        var result = _modOptions.LoadConfig<string[]>(key, defaultValue);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(defaultValue.Length);
        for (int i = 0; i < defaultValue.Length; i++)
        {
            result[i].ShouldBe(defaultValue[i]);
        }

        // 验证底层存储的是 JSON 字符串
        var savedJson = _modOptions.LoadConfig<string>(key);
        savedJson.ShouldNotBeNull();
        savedJson.ShouldStartWith("[");
        savedJson.ShouldEndWith("]");
    }

    [Test]
    public void LoadConfig_WithMissingKeyAndDictionaryDefaultValue_ShouldSerializeToJson()
    {
        // Arrange
        var key = "test_missing_dictionary";
        var defaultValue = new Dictionary<string, int>
        {
            { "key1", 1 },
            { "key2", 2 },
            { "key3", 3 }
        };

        // Act
        var result = _modOptions.LoadConfig<Dictionary<string, int>>(key, defaultValue);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(defaultValue.Count);
        foreach (var kvp in defaultValue)
        {
            result.ContainsKey(kvp.Key).ShouldBeTrue();
            result[kvp.Key].ShouldBe(kvp.Value);
        }

        // 验证底层存储的是 JSON 字符串
        var savedJson = _modOptions.LoadConfig<string>(key);
        savedJson.ShouldNotBeNull();
        savedJson.ShouldStartWith("{");
        savedJson.ShouldEndWith("}");
    }

    [Test]
    public void LoadConfig_WithUnserializableDefaultValue_ShouldReturnDefaultWithoutPersisting()
    {
        // Arrange
        var key = "test_missing_unserializable";
        var unserializableValue = new UnserializableObject();

        // Act
        var result = _modOptions.LoadConfig<UnserializableObject>(key, unserializableValue);

        // Assert
        result.ShouldBe(unserializableValue);

        // 验证默认值没有被持久化（键仍然不存在）
        var keyExists = _storage.KeyExists(key, _modOptions.GetConfigFilePath());
        keyExists.ShouldBeFalse();
    }

    #endregion

    /// <summary>
    /// 简单的控制台日志类，便于在测试失败时查看内部异常
    /// </summary>
    private sealed class TestConsoleLogger : ILog
    {
        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception? exception = null,
            params object[] formatParameters)
        {
            var message = messageFunc?.Invoke();
            Console.WriteLine($"[{logLevel}] {message}");
            if (exception != null)
            {
                Console.WriteLine(exception);
            }

            return true;
        }
    }

    private sealed class InMemoryModOptionsStorage : IModOptionsStorage
    {
        private readonly Dictionary<string, Dictionary<string, object?>> _files = new(StringComparer.Ordinal);

        public bool FileExists(string path) => _files.ContainsKey(path);

        public bool KeyExists(string key, string path) =>
            _files.TryGetValue(path, out var file) && file.ContainsKey(key);

        public void Save<T>(string key, T data, string path)
        {
            var file = GetOrCreateFile(path);
            file[key] = data;
        }

        public T Load<T>(string key, string path)
        {
            if (!_files.TryGetValue(path, out var file) || !file.TryGetValue(key, out var value))
            {
                throw new InvalidOperationException($"Key '{key}' does not exist for path '{path}'.");
            }

            if (value is T typed)
            {
                return typed;
            }

            if (value == null)
                return default!;

            return (T)Convert.ChangeType(value, typeof(T));
        }

        public void DeleteFile(string path) => _files.Remove(path);

        private Dictionary<string, object?> GetOrCreateFile(string path)
        {
            if (!_files.TryGetValue(path, out var file))
            {
                file = new Dictionary<string, object?>(StringComparer.Ordinal);
                _files[path] = file;
            }

            return file;
        }
    }

    /// <summary>
    /// 测试用的复杂对象类
    /// </summary>
    public class TestComplexObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// 测试用的不可序列化对象类
    /// </summary>
    public class UnserializableObject
    {
        // 通过自定义序列化来强制失败
        [JsonProperty]
        public string Name { get; set; } = "Test";

        // 这个属性会在序列化时抛出异常
        [JsonProperty]
        public object ProblematicProperty => throw new InvalidOperationException("This property cannot be serialized");
    }
}
