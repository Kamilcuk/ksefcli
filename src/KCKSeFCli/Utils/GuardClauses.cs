// Copied from thirdparty/ksef-client-csharp/KSeF.Client.Tests.Utils/Compatibility/GuardClauses.cs
#nullable enable
using System.Runtime.CompilerServices;

namespace KCKSeFCli.Utils;

/// <summary>
/// Uniwersalna klasa walidacji argumentów kompilująca się na wszystkich TFM.
/// Na net48 (NETFRAMEWORK): pełna implementacja polyfill.
/// Na net8.0+: inline forwarding do wbudowanych metod .NET.
/// </summary>
internal static class Guard
{
    /// <summary>
    /// Zgłasza <see cref="ArgumentNullException"/> gdy <paramref name="argument"/> jest null.
    /// </summary>
    public static void ThrowIfNull(
        object? argument,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName);
    }

    /// <summary>
    /// Zgłasza <see cref="ArgumentException"/> gdy <paramref name="argument"/> jest null, pusty lub whitespace.
    /// </summary>
    public static void ThrowIfNullOrWhiteSpace(
        string? argument,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(argument))
            throw new ArgumentException("Wartość nie może być null, pusta ani zawierać wyłącznie białych znaków.", paramName);
    }

    /// <summary>
    /// Zgłasza <see cref="ArgumentException"/> gdy <paramref name="argument"/> jest null lub pusty.
    /// </summary>
    public static void ThrowIfNullOrEmpty(
        string? argument,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (string.IsNullOrEmpty(argument))
            throw new ArgumentException("Wartość nie może być null ani pusta.", paramName);
    }

    /// <summary>
    /// Zgłasza <see cref="ArgumentOutOfRangeException"/> gdy <paramref name="value"/> jest ujemna lub równa zero.
    /// </summary>
    public static void ThrowIfNegativeOrZero(
        int value,
        [CallerArgumentExpression("value")] string? paramName = null)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(paramName, value, "Wartość musi być dodatnia.");
    }

    /// <summary>
    /// Zgłasza <see cref="ArgumentOutOfRangeException"/> gdy <paramref name="value"/> jest mniejsza od <paramref name="other"/>.
    /// </summary>
    public static void ThrowIfLessThan<T>(
        T value,
        T other,
        [CallerArgumentExpression("value")] string? paramName = null) where T : IComparable<T>
    {
        if (value.CompareTo(other) < 0)
            throw new ArgumentOutOfRangeException(paramName, value, $"Wartość musi być większa lub równa {other}.");
    }

    /// <summary>
    /// Zgłasza <see cref="ArgumentOutOfRangeException"/> gdy <paramref name="value"/> jest mniejsza lub równa <paramref name="other"/>.
    /// </summary>
    public static void ThrowIfLessThanOrEqual<T>(
        T value,
        T other,
        [CallerArgumentExpression("value")] string? paramName = null) where T : IComparable<T>
    {
        if (value.CompareTo(other) <= 0)
            throw new ArgumentOutOfRangeException(paramName, value, $"Wartość musi być większa niż {other}.");
    }
}
