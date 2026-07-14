namespace GestStack.Domain.Entities;

// Single-row entity: GestStack manages one company.
public class CompanyProfile : Entity<int>
{
    public CompanyProfile() => Id = 1;

    public required string LegalName { get; set; }
    public string? TradeName { get; set; }
    public byte[]? Logo { get; set; }
    public string? LogoContentType { get; set; }

    public string? Street { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    public string? TaxId { get; set; }
    public string? RegistrationNumber { get; set; }

    public string Currency { get; set; } = "USD";
}
