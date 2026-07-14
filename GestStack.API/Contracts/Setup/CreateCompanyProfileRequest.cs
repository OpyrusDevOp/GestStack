namespace GestStack.API.Contracts.Setup;

public record CreateCompanyProfileRequest(
    string LegalName,
    string? Email,
    string? Country,
    byte[]? Logo,
    string? LogoContentType
);
