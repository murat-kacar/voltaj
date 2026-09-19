namespace Voltflow.Domain.Common;

/// <summary>Money is kept to two decimals and rounded away from zero, the way a printed receipt is.</summary>
public static class Money
{
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
