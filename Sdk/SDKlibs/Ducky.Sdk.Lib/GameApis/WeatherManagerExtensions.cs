using Duckov.Weathers;

namespace Ducky.Sdk.GameApis;

/// <summary>Weather calls of the game, forwarded to <c>WeatherManager</c>.</summary>
public static class WeatherManagerExtensions
{
    /// <summary>
    /// Forces the weather to <paramref name="value"/> when <paramref name="forceWeather"/> is true, or turns the forcing
    /// off when it is false. Forwards to <c>WeatherManager.SetForceWeather</c>.
    /// </summary>
    public static void SetForceWeather(bool forceWeather, Weather value = Weather.Sunny) =>
        WeatherManager.SetForceWeather(forceWeather, value);
}
