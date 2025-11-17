using System;
using System.Collections.Generic;
using System.IO;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Options;
using NUnit.Framework;

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
}
