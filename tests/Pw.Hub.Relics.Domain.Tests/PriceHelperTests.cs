using FluentAssertions;
using Pw.Hub.Relics.Shared.Helpers;
using Xunit;

namespace Pw.Hub.Relics.Domain.Tests;

public class PriceHelperTests
{
    [Theory]
    [InlineData(0, "0 зол.")]
    [InlineData(100, "1 зол.")]
    [InlineData(150, "1 зол. 50 сер.")]
    [InlineData(50, "50 сер.")]
    [InlineData(1500, "15 зол.")]
    [InlineData(150050, "1500 зол. 50 сер.")]
    [InlineData(10000, "100 зол.")]
    public void FormatPrice_ShouldReturnCorrectFormat(long priceInSilver, string expected)
    {
        // Act
        var result = PriceHelper.FormatPrice(priceInSilver);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 100)]
    [InlineData(1, 50, 150)]
    [InlineData(15, 0, 1500)]
    [InlineData(1500, 50, 150050)]
    public void ToSilver_ShouldConvertCorrectly(long gold, int silver, long expected)
    {
        // Act
        var result = PriceHelper.ToSilver(gold, silver);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 1)]
    [InlineData(150, 1)]
    [InlineData(50, 0)]
    [InlineData(150050, 1500)]
    public void GetGold_ShouldExtractGoldCorrectly(long priceInSilver, long expectedGold)
    {
        // Act
        var result = PriceHelper.GetGold(priceInSilver);

        // Assert
        result.Should().Be(expectedGold);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 0)]
    [InlineData(150, 50)]
    [InlineData(50, 50)]
    [InlineData(150050, 50)]
    public void GetSilver_ShouldExtractSilverCorrectly(long priceInSilver, int expectedSilver)
    {
        // Act
        var result = PriceHelper.GetSilver(priceInSilver);

        // Assert
        result.Should().Be(expectedSilver);
    }
}
