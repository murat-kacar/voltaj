namespace Voltflow.Domain.Common;

public static class Guard
{
    public static string NotEmpty(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} cannot be empty.", name);

        return value;
    }

    public static Guid AgainstEmptyGuid(Guid value, string name)
    {
        if (value == Guid.Empty)
            throw new ArgumentException($"{name} cannot be empty.", name);

        return value;
    }

    public static decimal AgainstNegative(decimal value, string name)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(name, value, $"{name} cannot be negative.");

        return value;
    }

    public static decimal AgainstNegativeOrZero(decimal value, string name)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be greater than zero.");

        return value;
    }

    public static decimal AgainstOutOfRange(decimal value, decimal min, decimal max, string name)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be between {min} and {max}.");

        return value;
    }

    public static T NotNull<T>(T? value, string name) where T : class
    {
        if (value is null)
            throw new ArgumentNullException(name, $"{name} cannot be null.");

        return value;
    }
}

