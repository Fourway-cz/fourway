namespace Fourway.Api.Models;

/// <summary>
/// Společná validační pravidla pro doménové modely.
/// </summary>
internal static class DomainValidation
{
    /// <summary>
    /// Ověří povinný text, ořízne ho a zkontroluje maximální délku.
    /// </summary>
    public static string Required(string? value, string paramName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Hodnota může mít maximálně {maxLength} znaků.",
                paramName);
        }

        return normalized;
    }

    /// <summary>
    /// Ověří volitelný text, ořízne ho a zkontroluje maximální délku.
    /// Prázdný text se ukládá jako <see langword="null"/>.
    /// </summary>
    public static string? Optional(string? value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Hodnota může mít maximálně {maxLength} znaků.",
                paramName);
        }

        return normalized;
    }

    /// <summary>
    /// Ověří, že číslo je kladné.
    /// </summary>
    public static int Positive(int value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Hodnota musí být kladná.");
        }

        return value;
    }

    /// <summary>
    /// Ověří, že číslo je nezáporné.
    /// </summary>
    public static int NotNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Hodnota nesmí být záporná.");
        }

        return value;
    }

    /// <summary>
    /// Ověří, že číslo je nezáporné.
    /// </summary>
    public static short NotNegative(short value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Hodnota nesmí být záporná.");
        }

        return value;
    }

    /// <summary>
    /// Ověří platnost rozsahu dat a vrátí normalizované počáteční datum.
    /// </summary>
    public static DateOnly ValidateDateRange(
        DateOnly from,
        DateOnly? to,
        string paramName)
    {
        if (to is not null && to < from)
        {
            throw new ArgumentException(
                "Koncové datum nesmí být před počátečním datem.",
                paramName);
        }

        return from;
    }

    /// <summary>
    /// Ověří časový interval od půlnoci. Povoluje i časy přes 24 hodin.
    /// </summary>
    public static TimeSpan? ValidateTime(TimeSpan? value, string paramName)
    {
        if (value is null)
        {
            return null;
        }

        if (value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(paramName, "Čas nesmí být záporný.");
        }

        if (value >= TimeSpan.FromDays(7))
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                "Čas nesmí přesáhnout 7 dní.");
        }

        return value;
    }
}
