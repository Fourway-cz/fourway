using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Fourway.Core.Models;

/// <summary>
/// Kalendářní definice, která určuje, ve kterých dnech spoje jezdí.
/// </summary>
/// <remarks>
/// Jeden <see cref="Service"/> může sdílet více spojů i linek. Identita je dána
/// <see cref="RuleHash"/>, který se počítá z celé množiny vzorů a výjimek.
/// Pravidelnost je v <see cref="Patterns"/>, skutečné odchylky ve
/// <see cref="Exceptions"/>.
/// </remarks>
public sealed class Service
{
    /// <summary>Maximální délka hexadecimálního hashe.</summary>
    public const int RuleHashMaxLength = 64;

    /// <summary>Identifikátor kalendáře.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Deterministický hash normalizované definice kalendáře.
    /// </summary>
    /// <remarks>
    /// Počítá se z <see cref="Patterns"/> a <see cref="Exceptions"/>. Neobsahuje
    /// identifikátory linek ani spojů, aby šel kalendář sdílet.
    /// </remarks>
    public string RuleHash { get; private set; } = string.Empty;

    /// <summary>
    /// Pravidelné vzory, kdy spoj jede.
    /// </summary>
    public List<ServicePattern> Patterns { get; private set; } = [];

    /// <summary>
    /// Konkrétní datové výjimky vůči vzorům.
    /// </summary>
    public List<ServiceException> Exceptions { get; private set; } = [];

    /// <summary>
    /// Textové poznámky k kalendáři.
    /// </summary>
    /// <remarks>
    /// Poznámky z <c>Caskody.txt</c> a <c>Udaje.txt</c>.
    /// </remarks>
    public List<ServiceNote> Notes { get; private set; } = [];

    /// <summary>
    /// Vytvoří prázdný kalendář. Před uložením je nutné přidat alespoň jeden vzor.
    /// </summary>
    /// <remarks>
    /// Bezparametrový konstruktor vyžaduje EF Core. V doménovém kódu ho používej
    /// jen tehdy, když bude kalendář ihned naplněn vzory.
    /// </remarks>
    public Service()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>
    /// Přidá pravidelný vzor a přepočítá <see cref="RuleHash"/>.
    /// </summary>
    public ServicePattern AddPattern(
        bool monday,
        bool tuesday,
        bool wednesday,
        bool thursday,
        bool friday,
        bool saturday,
        bool sunday,
        DateOnly validFrom,
        DateOnly validTo,
        WeekParity weekParity = WeekParity.All,
        DateOnly? parityAnchor = null)
    {
        var pattern = new ServicePattern(
            Id,
            monday,
            tuesday,
            wednesday,
            thursday,
            friday,
            saturday,
            sunday,
            validFrom,
            validTo,
            weekParity,
            parityAnchor);

        Patterns.Add(pattern);
        RecomputeRuleHash();
        return pattern;
    }

    /// <summary>
    /// Přidá datovou výjimku a přepočítá <see cref="RuleHash"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Pokud už pro dané datum existuje jiná výjimka.
    /// </exception>
    public ServiceException AddException(DateOnly date, ServiceExceptionType exceptionType)
    {
        if (Exceptions.Any(x => x.Date == date))
        {
            throw new InvalidOperationException(
                $"Pro datum {date:yyyy-MM-dd} už existuje výjimka.");
        }

        var exception = new ServiceException(Id, date, exceptionType);
        Exceptions.Add(exception);
        RecomputeRuleHash();
        return exception;
    }

    /// <summary>
    /// Přidá textovou poznámku k kalendáři.
    /// </summary>
    public ServiceNote AddNote(
        int sequence,
        string? label,
        string text,
        ServiceNoteSource source,
        DateOnly? validFrom = null,
        DateOnly? validTo = null)
    {
        var note = new ServiceNote(Id, sequence, label, text, source, validFrom, validTo);
        Notes.Add(note);
        return note;
    }

    /// <summary>
    /// Ověří, zda kalendář obsahuje alespoň jeden vzor.
    /// </summary>
    /// <exception cref="InvalidOperationException">Pokud kalendář nemá žádný vzor.</exception>
    public void EnsureHasPatterns()
    {
        if (Patterns.Count == 0)
        {
            throw new InvalidOperationException("Kalendář musí mít alespoň jeden vzor.");
        }
    }

    /// <summary>
    /// Ověří, zda spoj podle tohoto kalendáře jede v daném dni.
    /// </summary>
    /// <remarks>
    /// Metoda pracuje s načtenými <see cref="Patterns"/> a <see cref="Exceptions"/>.
    /// Explicitní výjimka má přednost před vzory.
    /// </remarks>
    public bool IsActiveOn(DateOnly date)
    {
        var exception = Exceptions.FirstOrDefault(x => x.Date == date);
        if (exception is not null)
        {
            return exception.ExceptionType == ServiceExceptionType.Runs;
        }

        return Patterns.Any(pattern => pattern.Includes(date));
    }

    /// <summary>
    /// Přepočítá <see cref="RuleHash"/> z aktuálních vzorů a výjimek.
    /// </summary>
    /// <remarks>
    /// Hash je SHA-256 v hexadecimálním tvaru. Vzory i výjimky se před výpočtem
    /// deterministicky seřadí.
    /// </remarks>
    public void RecomputeRuleHash()
    {
        if (Patterns.Count == 0)
        {
            RuleHash = string.Empty;
            return;
        }

        var builder = new StringBuilder();

        foreach (var pattern in Patterns
            .OrderBy(x => x.ValidFrom)
            .ThenBy(x => x.ValidTo)
            .ThenBy(x => x.WeekParity)
            .ThenBy(x => x.ParityAnchor)
            .ThenBy(x => x.Monday)
            .ThenBy(x => x.Tuesday)
            .ThenBy(x => x.Wednesday)
            .ThenBy(x => x.Thursday)
            .ThenBy(x => x.Friday)
            .ThenBy(x => x.Saturday)
            .ThenBy(x => x.Sunday))
        {
            builder
                .Append(pattern.ValidFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('|')
                .Append(pattern.ValidTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('|')
                .Append((int)pattern.WeekParity).Append('|')
                .Append(pattern.ParityAnchor?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-").Append('|')
                .Append(pattern.Monday ? '1' : '0')
                .Append(pattern.Tuesday ? '1' : '0')
                .Append(pattern.Wednesday ? '1' : '0')
                .Append(pattern.Thursday ? '1' : '0')
                .Append(pattern.Friday ? '1' : '0')
                .Append(pattern.Saturday ? '1' : '0')
                .Append(pattern.Sunday ? '1' : '0')
                .Append('\n');
        }

        foreach (var exception in Exceptions
            .OrderBy(x => x.Date)
            .ThenBy(x => x.ExceptionType))
        {
            builder
                .Append(exception.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('|')
                .Append((int)exception.ExceptionType)
                .Append('\n');
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        RuleHash = Convert.ToHexString(hash).ToLowerInvariant();
    }
}
