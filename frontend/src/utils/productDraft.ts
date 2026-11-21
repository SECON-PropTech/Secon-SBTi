// Helpers for the row that is being edited
import type { EnergyProductRow } from "../types/energyConsumption";
import type { DraftErrors, EditField, ProductDraft, UpdateProductRequest} from "../types/product"

// Turns a text field into a number. Empty text means value is missing.
// Users can either enter "," or "." (1.5/1,5)
export function toNumber(text: string): number | null {
    const trimmed = text.trim().replace(",", ".");
    if (trimmed === "") {
        return null;
    }
    const value = Number(trimmed);
    return Number.isFinite(value) ? value:null;

}

//turns a number into text for an input.
function toText(value: number | null): string {
    return value === null ? "" : String(value);
}

//The highest values the backend accepts
const maxKwhPerYear = 1_000_000_000;
const maxLifetimeYears = 1_000;

//Builds the first draft from the product the user clicked on
export function createDraft(product: EnergyProductRow): ProductDraft {
    return {
        groupId: product.groupId,
        kwhPerYear: toText(product.kwhPerYear),
        lifetimeYears: toText(product.lifetimeYears),
        measurementQuality: product.measurementQuality,
        comment: product.comment ?? "", 
    };
}

// The field the mouse should start in, the first one
// that is missing, otherwise kwh per år
export function firstMissingField(product: EnergyProductRow): EditField
{
    if (product.missing.includes("kwh")) {
        return "kwhPerYear";
    }
    if (product.missing.includes("lifetime")) {
        return "lifetimeYears";
    }
    if (product.missing.includes("group")) {
        return "groupId";
    }
    return "kwhPerYear";
}

// Same limits as UpdateSoldProductRequest in the backend, we don't send
// something it refuses
// An empty field is allowed but the value is still missing.
export function findDraftErrors(draft: ProductDraft): DraftErrors {
    const errors: DraftErrors = {};

    const kwh = toNumber(draft.kwhPerYear);
    if (draft.kwhPerYear.trim() !== "" && (kwh === null || kwh < 0 || kwh > maxKwhPerYear)) {
        errors.kwhPerYear = "Ange mellan 0 och 1 000 000 000.";
    }

    const lifetime = toNumber(draft.lifetimeYears);
    if (draft.lifetimeYears.trim() !== "" && (lifetime === null || lifetime <= 0 || lifetime > maxLifetimeYears)) {
        errors.lifetimeYears = "Ange mer än 0 och högst 1 000.";
    }
    return errors;
}

// True when the user has changed something, so Spara is only on
//when there is something to save
export function isDraftChanged(draft: ProductDraft, product: EnergyProductRow): boolean {
    return (
        draft.groupId !== product.groupId ||
        toNumber(draft.kwhPerYear) !== product.kwhPerYear ||
        toNumber(draft.lifetimeYears) !== product.lifetimeYears ||
        draft.measurementQuality !== product.measurementQuality ||
        draft.comment.trim() !== (product.comment ?? "")
    );
}

// The body for PUT /api/products/{id}. All five fields
export function draftToRequest(draft: ProductDraft): UpdateProductRequest {
    return {
        groupId: draft.groupId,
        kwhPerYear: toNumber(draft.kwhPerYear),
        lifetimeYears: toNumber(draft.lifetimeYears),
        measurementQuality: draft.measurementQuality,
        comment: draft.comment.trim() === "" ? null: draft.comment.trim(),
    
    };
}