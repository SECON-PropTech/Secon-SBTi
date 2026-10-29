namespace SeconSbtiBackend.DTOs.Scope312;

// Beskriver en produkt och dess materialdelar för Scope 3.12.
// DTO:n innehåller bara de uppgifter som hämtningen behöver returnera.
public sealed record ProductMaterialsResponse(
    Guid ProductId,
    string ProductName,
    string? ArticleNo,
    IReadOnlyList<ProductMaterialResponse> Materials
);

// Beskriver en materialdel med lagrad vikt i kg och slutbehandling.
// Behandlingen hämtas från materialet
public sealed record ProductMaterialResponse(
    Guid MaterialId,
    string MaterialName,
    decimal WeightKg,
    Guid TreatmentId,
    string TreatmentName
);