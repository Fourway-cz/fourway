namespace Fourway.Api.Models;

public sealed class Example
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public ExampleStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Description) ? Name : $"{Name} – {Description}";

    // EF Core
    private Example()
    {
    }

    public Example(string name, string? description = null)
    {
        Id = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow;

        CreatedAt = now;
        UpdatedAt = now;
        Name = NormalizeName(name);
        Description = NormalizeDescription(description);
        Status = ExampleStatus.Active;
    }

    public void Rename(string name)
    {
        Name = NormalizeName(name);
        Touch();
    }

    public void ChangeDescription(string? description)
    {
        Description = NormalizeDescription(description);
        Touch();
    }

    public void Archive()
    {
        if (Status == ExampleStatus.Archived)
        {
            throw new InvalidOperationException("Example už je archivovaný.");
        }

        Status = ExampleStatus.Archived;
        Touch();
    }

    public void Activate()
    {
        if (Status == ExampleStatus.Active)
        {
            throw new InvalidOperationException("Example už je aktivní.");
        }

        Status = ExampleStatus.Active;
        Touch();
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalized = name.Trim();
        if (normalized.Length > NameMaxLength)
        {
            throw new ArgumentException(
                $"Název může mít maximálně {NameMaxLength} znaků.",
                nameof(name));
        }

        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalized = description.Trim();
        if (normalized.Length > DescriptionMaxLength)
        {
            throw new ArgumentException(
                $"Popis může mít maximálně {DescriptionMaxLength} znaků.",
                nameof(description));
        }

        return normalized;
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
