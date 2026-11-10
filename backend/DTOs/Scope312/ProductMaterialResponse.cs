namespace SeconSbtiBackend.DTOs.Scope312;

// Beskriver en materialdel med lagrad vikt i kg och slutbehandling.
public sealed record ProductMaterialResponse(
    Guid MaterialId,
    string MaterialName,
    decimal WeightKg,
    Guid TreatmentId,
    string TreatmentName
);