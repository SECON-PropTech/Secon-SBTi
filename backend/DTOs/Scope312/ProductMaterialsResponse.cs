namespace SeconSbtiBackend.DTOs.Scope312;

// Beskriver en produkt och dess materialdelar för Scope 3.12.
// DTO:n innehåller bara de uppgifter som hämtningen behöver returnera.
public sealed record ProductMaterialsResponse(
    Guid ProductId,
    string ProductName,
    string? ArticleNo,
    IReadOnlyList<ProductMaterialResponse> Materials
);

