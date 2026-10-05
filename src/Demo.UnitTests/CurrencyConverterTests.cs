using System.Globalization;
using Demo.Application.Modules;

namespace Demo.UnitTests;

public class CurrencyConverterTests
{
    [Theory]
    [InlineData("103.30", "0.92010000", "95.05")]   // 95.04633 rounds up
    [InlineData("102.20", "0.92010000", "94.03")]   // 94.03422 rounds down
    [InlineData("59.99", "1.38160000", "82.88")]    // 82.881784
    [InlineData("100.00", "1", "100.00")]
    public void Convert_MultipliesAndRoundsToTwoDecimals(string amount, string rate, string expected)
    {
        var result = CurrencyConverter.Convert(Parse(amount), Parse(rate));

        Assert.Equal(Parse(expected), result);
    }

    [Fact]
    public void Convert_RoundsMidpointAwayFromZero()
    {
        // 10.00 × 0.12345 = 1.2345 → 1.23, but 1.00 × 0.125 = 0.125 → 0.13 (banker's rounding would give 0.12).
        Assert.Equal(0.13M, CurrencyConverter.Convert(1.00M, 0.125M));
        Assert.Equal(1.23M, CurrencyConverter.Convert(10.00M, 0.12345M));
    }

    // Decimal attributes are not allowed in [InlineData], so values are passed as strings and parsed culture-independently.
    private static decimal Parse(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
}
