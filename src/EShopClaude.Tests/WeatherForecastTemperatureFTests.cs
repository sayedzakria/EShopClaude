using WebForecast = EShopClaude.Web.WeatherForecast;
using ApiForecast = global::WeatherForecast;

namespace EShopClaude.Tests;

public class WeatherForecastTemperatureFTests
{
    [Theory]
    [InlineData(100, 212)]
    [InlineData(20, 68)]
    [InlineData(-20, -4)]
    [InlineData(37, 99)]
    [InlineData(0, 32)]
    public void ApiService_WeatherForecast_TemperatureF_ConvertsCorrectly(int celsius, int expectedFahrenheit)
    {
        var forecast = new ApiForecast(DateOnly.FromDateTime(DateTime.Today), celsius, "Test");
        Assert.Equal(expectedFahrenheit, forecast.TemperatureF);
    }

    [Theory]
    [InlineData(100, 212)]
    [InlineData(20, 68)]
    [InlineData(-20, -4)]
    [InlineData(37, 99)]
    [InlineData(0, 32)]
    public void Web_WeatherForecast_TemperatureF_ConvertsCorrectly(int celsius, int expectedFahrenheit)
    {
        var forecast = new WebForecast(DateOnly.FromDateTime(DateTime.Today), celsius, "Test");
        Assert.Equal(expectedFahrenheit, forecast.TemperatureF);
    }
}
