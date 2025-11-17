using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// Tests for DateTime and DateTimeOffset handling in ModOptions
/// These tests verify Unix timestamp-based serialization
/// </summary>
[TestFixture]
public class ModOptionsDateTimeTests
{
    private Type _modOptionsType;
    private MethodInfo _convertToUnixSecondsMethod;
    private MethodInfo _convertFromUnixSecondsMethod;
    private MethodInfo _isDateTimeTypeMethod;

    [SetUp]
    public void Setup()
    {
        // Use reflection to access private methods for testing
        _modOptionsType = typeof(Ducky.Sdk.Options.ModOptions);

        _convertToUnixSecondsMethod = _modOptionsType.GetMethod(
            "ConvertToUnixSeconds",
            BindingFlags.NonPublic | BindingFlags.Static);

        _convertFromUnixSecondsMethod = _modOptionsType.GetMethod(
            "ConvertFromUnixSeconds",
            BindingFlags.NonPublic | BindingFlags.Static);

        _isDateTimeTypeMethod = _modOptionsType.GetMethod(
            "IsDateTimeType",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.IsNotNull(_convertToUnixSecondsMethod, "ConvertToUnixSeconds method should exist");
        Assert.IsNotNull(_convertFromUnixSecondsMethod, "ConvertFromUnixSeconds method should exist");
        Assert.IsNotNull(_isDateTimeTypeMethod, "IsDateTimeType method should exist");
    }

    [Test]
    public void IsDateTimeType_RecognizesDateTime()
    {
        var result = (bool)_isDateTimeTypeMethod.Invoke(null, new object[] { typeof(DateTime) });
        Assert.IsTrue(result, "DateTime should be recognized as a DateTime type");
    }

    [Test]
    public void IsDateTimeType_RecognizesDateTimeOffset()
    {
        var result = (bool)_isDateTimeTypeMethod.Invoke(null, new object[] { typeof(DateTimeOffset) });
        Assert.IsTrue(result, "DateTimeOffset should be recognized as a DateTime type");
    }

    [Test]
    public void IsDateTimeType_RecognizesNullableDateTime()
    {
        var result = (bool)_isDateTimeTypeMethod.Invoke(null, new object[] { typeof(DateTime?) });
        Assert.IsTrue(result, "DateTime? should be recognized as a DateTime type");
    }

    [Test]
    public void IsDateTimeType_RecognizesNullableDateTimeOffset()
    {
        var result = (bool)_isDateTimeTypeMethod.Invoke(null, new object[] { typeof(DateTimeOffset?) });
        Assert.IsTrue(result, "DateTimeOffset? should be recognized as a DateTime type");
    }

    [Test]
    public void IsDateTimeType_RejectsOtherTypes()
    {
        Assert.IsFalse((bool)_isDateTimeTypeMethod.Invoke(null, new object[] { typeof(int) }));
        Assert.IsFalse((bool)_isDateTimeTypeMethod.Invoke(null, new object[] { typeof(string) }));
        Assert.IsFalse((bool)_isDateTimeTypeMethod.Invoke(null, new object[] { typeof(TimeSpan) }));
        Assert.IsFalse((bool)_isDateTimeTypeMethod.Invoke(null, new object[] { typeof(Guid) }));
    }

    [Test]
    public void ConvertToUnixSeconds_HandlesNull()
    {
        var result = (long)_convertToUnixSecondsMethod.Invoke(null, new object[] { null });
        Assert.AreEqual(0, result, "Null should convert to 0");
    }

    [Test]
    public void ConvertToUnixSeconds_DateTime_UnixEpoch()
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = (long)_convertToUnixSecondsMethod.Invoke(null, new object[] { epoch });
        Assert.AreEqual(0, result, "Unix epoch should convert to 0");
    }

    [Test]
    public void ConvertToUnixSeconds_DateTime_KnownValue()
    {
        // 2024-01-01 00:00:00 UTC = 1704067200 Unix seconds
        var date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = (long)_convertToUnixSecondsMethod.Invoke(null, new object[] { date });
        Assert.AreEqual(1704067200, result);
    }

    [Test]
    public void ConvertToUnixSeconds_DateTimeOffset_KnownValue()
    {
        // 2024-01-01 00:00:00 UTC = 1704067200 Unix seconds
        var dateOffset = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var result = (long)_convertToUnixSecondsMethod.Invoke(null, new object[] { dateOffset });
        Assert.AreEqual(1704067200, result);
    }

    [Test]
    public void ConvertToUnixSeconds_DateTime_LocalTime_ConvertsToUtc()
    {
        // Create a local time and verify it gets converted to UTC
        var localTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var result = (long)_convertToUnixSecondsMethod.Invoke(null, new object[] { localTime });

        // The result should match the UTC conversion
        var expectedUtc = localTime.ToUniversalTime();
        var expectedSeconds = (long)(expectedUtc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        Assert.AreEqual(expectedSeconds, result);
    }

    [Test]
    public void ConvertFromUnixSeconds_DateTime_UnixEpoch()
    {
        var method = _convertFromUnixSecondsMethod.MakeGenericMethod(typeof(DateTime));
        var result = (DateTime)method.Invoke(null, new object[] { 0L });

        var expected = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void ConvertFromUnixSeconds_DateTime_KnownValue()
    {
        var method = _convertFromUnixSecondsMethod.MakeGenericMethod(typeof(DateTime));
        var result = (DateTime)method.Invoke(null, new object[] { 1704067200L });

        var expected = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void ConvertFromUnixSeconds_DateTimeOffset_KnownValue()
    {
        var method = _convertFromUnixSecondsMethod.MakeGenericMethod(typeof(DateTimeOffset));
        var result = (DateTimeOffset)method.Invoke(null, new object[] { 1704067200L });

        var expected = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void ConvertFromUnixSeconds_NullableDateTime_ZeroReturnsNull()
    {
        var method = _convertFromUnixSecondsMethod.MakeGenericMethod(typeof(DateTime?));
        var result = (DateTime?)method.Invoke(null, new object[] { 0L });

        Assert.IsNull(result, "0 should convert to null for nullable DateTime");
    }

    [Test]
    public void ConvertFromUnixSeconds_NullableDateTimeOffset_ZeroReturnsNull()
    {
        var method = _convertFromUnixSecondsMethod.MakeGenericMethod(typeof(DateTimeOffset?));
        var result = (DateTimeOffset?)method.Invoke(null, new object[] { 0L });

        Assert.IsNull(result, "0 should convert to null for nullable DateTimeOffset");
    }

    [Test]
    public void ConvertFromUnixSeconds_NullableDateTime_NonZeroReturnsValue()
    {
        var method = _convertFromUnixSecondsMethod.MakeGenericMethod(typeof(DateTime?));
        var result = (DateTime?)method.Invoke(null, new object[] { 1704067200L });

        Assert.IsNotNull(result, "Non-zero should return a value for nullable DateTime");
        Assert.AreEqual(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Test]
    public void ConvertFromUnixSeconds_NullableDateTimeOffset_NonZeroReturnsValue()
    {
        var method = _convertFromUnixSecondsMethod.MakeGenericMethod(typeof(DateTimeOffset?));
        var result = (DateTimeOffset?)method.Invoke(null, new object[] { 1704067200L });

        Assert.IsNotNull(result, "Non-zero should return a value for nullable DateTimeOffset");
        Assert.AreEqual(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), result.Value);
    }

    [Test]
    public void RoundTrip_DateTime()
    {
        var original = new DateTime(2024, 6, 15, 14, 30, 45, DateTimeKind.Utc);

        var unixSeconds = (long)_convertToUnixSecondsMethod.Invoke(null, new object[] { original });

        var method = _convertFromUnixSecondsMethod.MakeGenericMethod(typeof(DateTime));
        var roundTrip = (DateTime)method.Invoke(null, new object[] { unixSeconds });

        Assert.AreEqual(original, roundTrip, "DateTime should round-trip correctly");
    }

    [Test]
    public void RoundTrip_DateTimeOffset()
    {
        var original = new DateTimeOffset(2024, 6, 15, 14, 30, 45, TimeSpan.Zero);

        var unixSeconds = (long)_convertToUnixSecondsMethod.Invoke(null, new object[] { original });

        var method = _convertFromUnixSecondsMethod.MakeGenericMethod(typeof(DateTimeOffset));
        var roundTrip = (DateTimeOffset)method.Invoke(null, new object[] { unixSeconds });

        Assert.AreEqual(original, roundTrip, "DateTimeOffset should round-trip correctly");
    }

    [Test]
    public void RoundTrip_NullableDateTime_WithValue()
    {
        DateTime? original = new DateTime(2024, 6, 15, 14, 30, 45, DateTimeKind.Utc);

        var unixSeconds = (long)_convertToUnixSecondsMethod.Invoke(null, new object[] { original });

        var method = _convertFromUnixSecondsMethod.MakeGenericMethod(typeof(DateTime?));
        var roundTrip = (DateTime?)method.Invoke(null, new object[] { unixSeconds });

        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(original.Value, roundTrip.Value, "Nullable DateTime should round-trip correctly");
    }

    [Test]
    public void RoundTrip_NullableDateTime_Null()
    {
        DateTime? original = null;

        var unixSeconds = (long)_convertToUnixSecondsMethod.Invoke(null, new object[] { original });
        Assert.AreEqual(0, unixSeconds, "Null should convert to 0");

        var method = _convertFromUnixSecondsMethod.MakeGenericMethod(typeof(DateTime?));
        var roundTrip = (DateTime?)method.Invoke(null, new object[] { unixSeconds });

        Assert.IsNull(roundTrip, "0 should convert back to null");
    }

    [Test]
    public void ConvertToUnixSeconds_ThrowsForInvalidType()
    {
        Assert.Throws<TargetInvocationException>(
            () => { _convertToUnixSecondsMethod.Invoke(null, new object[] { "invalid" }); },
            "Should throw for non-DateTime types");
    }

    [Test]
    public void ConvertFromUnixSeconds_ThrowsForInvalidType()
    {
        Assert.Throws<TargetInvocationException>(() =>
        {
            var method = _convertFromUnixSecondsMethod.MakeGenericMethod(typeof(string));
            method.Invoke(null, new object[] { 0L });
        }, "Should throw for non-DateTime types");
    }

    [Test]
    public void ConvertToUnixSeconds_HandlesNegativeTimestamps()
    {
        // Date before Unix epoch: 1960-01-01
        var date = new DateTime(1960, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = (long)_convertToUnixSecondsMethod.Invoke(null, new object[] { date });

        Assert.Less(result, 0, "Dates before 1970 should have negative Unix timestamps");
    }

    [Test]
    public void ConvertFromUnixSeconds_HandlesNegativeTimestamps()
    {
        // -315619200 = 1960-01-01 00:00:00 UTC
        var method = _convertFromUnixSecondsMethod.MakeGenericMethod(typeof(DateTime));
        var result = (DateTime)method.Invoke(null, new object[] { -315619200L });

        var expected = new DateTime(1960, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void ConvertToUnixSeconds_HandlesFarFutureDates()
    {
        // Test a date far in the future: 2099-12-31
        var date = new DateTime(2099, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var result = (long)_convertToUnixSecondsMethod.Invoke(null, new object[] { date });

        Assert.Greater(result, 1704067200, "Future dates should have larger Unix timestamps");
    }

    [Test]
    public void ConvertFromUnixSeconds_HandlesFarFutureDates()
    {
        // 4102444799 = 2099-12-31 23:59:59 UTC
        var method = _convertFromUnixSecondsMethod.MakeGenericMethod(typeof(DateTime));
        var result = (DateTime)method.Invoke(null, new object[] { 4102444799L });

        var expected = new DateTime(2099, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        Assert.AreEqual(expected, result);
    }
}
