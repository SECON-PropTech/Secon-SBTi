import type { MeasurementQuality } from "./energyConsumption";

// Product group from GET /api/product-groups.
export type ProductGroup = {
    id: string;
    name: string;
    description: string | null;
};

// the body for PUT /api/products/{id}
// The backend replaces all five fields, so we always send all of them.
export type UpdateProductRequest = {
    groupId: string | null;
    kwhPerYear: number | null;
    lifetimeYears: number | null;
    measurementQuality: MeasurementQuality;
    comment: string | null;
};

// The value while a row is being edited
export type ProductDraft = {
    groupId: string | null;
    kwhPerYear: string;
    lifetimeYears: string;
    measurementQuality: MeasurementQuality;
    comment: string;

};

// which field the mouse should start in
export type EditField = "kwhPerYear" | "lifetimeYears" | "groupId" | "comment";

// One message per field that is wrong, for example { lifetimeYears: "Ange en livslängd över 0"}
export type DraftErrors = Partial<Record<EditField, string>>;