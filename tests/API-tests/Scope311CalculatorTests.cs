using FluentAssertions;
using SeconSbtiBackend.Calculations;

namespace api_tests;

public class Scope311CalculatorTests
{

    /// <summary>
    /// Tests the Activity method of the Scope311Calculator class to make sure math is correct.
    /// </summary>
    [Fact]
    public void Activity_multiplies_quantity_kwh_and_lifetime()
    {
        Scope311Calculator.Activity(10, 50, 15).Should().Be(7500);
    }

    /// <summary>
    /// Tests the Activity method of the Scope311Calculator class to make sure it returns null when kWh/year is missing.
    /// </summary>
    [Fact]
    public void Activity_is_null_when_kwh_is_missing()
    {
        Scope311Calculator.Activity(10, null, 15).Should().BeNull();
    }

    /// <summary>
    /// Tests the Activity method of the Scope311Calculator class to make sure it returns null when lifetime is missing.
    /// </summary>
    [Fact]
    public void Activity_is_null_when_lifetime_is_missing()
    {
        Scope311Calculator.Activity(10, 50, null).Should().BeNull();
    }

    /// <summary>
    /// Tests the Activity method of the Scope311Calculator class to make sure it returns 0 when kWh/year is 0, even if lifetime is missing.
    /// </summary>
    [Fact]
    public void Activity_is_zero_when_kwh_is_zero_even_without_lifetime()
    {
        Scope311Calculator.Activity(10, 0, null).Should().Be(0);
    }
}