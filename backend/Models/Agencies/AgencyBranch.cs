namespace Fourway.Api.Models;

/// <summary>
/// Provozovna dopravce s kontaktními údaji.
/// </summary>
/// <remarks>
/// Odpovídá záznamu v <c>Dopravci.txt</c>. Jeden dopravce může mít více
/// provozoven, každá s vlastními kontaktními údaji. Identifikována kombinací
/// <see cref="AgencyId"/> a <see cref="BranchNumber"/>.
/// </remarks>
public sealed class AgencyBranch
{
    /// <summary>Maximální délka adresy sídla.</summary>
    public const int AddressMaxLength = 300;

    /// <summary>Maximální délka telefonního předvolby.</summary>
    public const int PhonePrefixMaxLength = 10;

    /// <summary>Maximální délka telefonního čísla.</summary>
    public const int PhoneMaxLength = 30;

    /// <summary>Maximální délka faxového čísla.</summary>
    public const int FaxMaxLength = 30;

    /// <summary>Maximální délka e-mailové adresy.</summary>
    public const int EmailMaxLength = 200;

    /// <summary>Maximální délka URL webových stránek.</summary>
    public const int WebMaxLength = 300;

    /// <summary>Identifikátor provozovny.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Dopravce, ke kterému provozovna patří.
    /// </summary>
    public Guid AgencyId { get; private set; }

    /// <summary>
    /// Pořadové číslo provozovny z pole <c>CisloDopravce</c>.
    /// </summary>
    /// <remarks>
    /// Klíč pro vazbu z <c>Linky.txt</c> (sloupec 16) a <c>Altdop.txt</c> (sloupec 14).
    /// </remarks>
    public int BranchNumber { get; private set; }

    /// <summary>
    /// Adresa sídla nebo název provozovny z pole <c>SidloProvozovna</c>.
    /// </summary>
    public string? Address { get; private set; }

    /// <summary>
    /// Telefonní předvolba z pole <c>TelefonPredvolba</c>.
    /// </summary>
    public string? PhonePrefix { get; private set; }

    /// <summary>
    /// Telefon kanceláře nebo dispečinku z pole <c>Telefon1</c>.
    /// </summary>
    public string? Phone1 { get; private set; }

    /// <summary>
    /// Kontaktní telefon pro cestující / infolinka z pole <c>Telefon2</c>.
    /// </summary>
    public string? Phone2 { get; private set; }

    /// <summary>
    /// Faxové číslo z pole <c>Fax</c> (historické pole).
    /// </summary>
    public string? Fax { get; private set; }

    /// <summary>
    /// Kontaktní e-mail z pole <c>Email</c>.
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Webové stránky z pole <c>Web</c>.
    /// </summary>
    public string? Web { get; private set; }

    // Pro EF Core. V doménovém kódu se nepoužívá.
    private AgencyBranch()
    {
    }

    /// <summary>
    /// Vytvoří novou provozovnu dopravce.
    /// </summary>
    /// <param name="agencyId">Identifikátor dopravce.</param>
    /// <param name="branchNumber">Pořadové číslo provozovny.</param>
    /// <param name="address">Adresa sídla nebo název provozovny.</param>
    /// <param name="phonePrefix">Telefonní předvolba.</param>
    /// <param name="phone1">Telefon kanceláře / dispečinku.</param>
    /// <param name="phone2">Infolinka pro cestující.</param>
    /// <param name="fax">Faxové číslo.</param>
    /// <param name="email">Kontaktní e-mail.</param>
    /// <param name="web">Webové stránky.</param>
    public AgencyBranch(
        Guid agencyId,
        int branchNumber,
        string? address = null,
        string? phonePrefix = null,
        string? phone1 = null,
        string? phone2 = null,
        string? fax = null,
        string? email = null,
        string? web = null)
    {
        Id = Guid.CreateVersion7();
        AgencyId = agencyId;
        BranchNumber = DomainValidation.NotNegative(branchNumber, nameof(branchNumber));
        Address = DomainValidation.Optional(address, nameof(address), AddressMaxLength);
        PhonePrefix = DomainValidation.Optional(phonePrefix, nameof(phonePrefix), PhonePrefixMaxLength);
        Phone1 = DomainValidation.Optional(phone1, nameof(phone1), PhoneMaxLength);
        Phone2 = DomainValidation.Optional(phone2, nameof(phone2), PhoneMaxLength);
        Fax = DomainValidation.Optional(fax, nameof(fax), FaxMaxLength);
        Email = DomainValidation.Optional(email, nameof(email), EmailMaxLength);
        Web = DomainValidation.Optional(web, nameof(web), WebMaxLength);
    }

    /// <summary>
    /// Aktualizuje kontaktní údaje provozovny.
    /// </summary>
    public void UpdateContactDetails(
        string? address = null,
        string? phonePrefix = null,
        string? phone1 = null,
        string? phone2 = null,
        string? fax = null,
        string? email = null,
        string? web = null)
    {
        Address = DomainValidation.Optional(address, nameof(address), AddressMaxLength);
        PhonePrefix = DomainValidation.Optional(phonePrefix, nameof(phonePrefix), PhonePrefixMaxLength);
        Phone1 = DomainValidation.Optional(phone1, nameof(phone1), PhoneMaxLength);
        Phone2 = DomainValidation.Optional(phone2, nameof(phone2), PhoneMaxLength);
        Fax = DomainValidation.Optional(fax, nameof(fax), FaxMaxLength);
        Email = DomainValidation.Optional(email, nameof(email), EmailMaxLength);
        Web = DomainValidation.Optional(web, nameof(web), WebMaxLength);
    }
}
