import type { MaterialLine, MaterialLineDraft, MaterialLineErrors, MaterialLineRequest } from "../types/materialLine";
import { toNumber } from "./productDraft";

//The date a new row starts on. Today when the current year is shown
// otherwise the first of Jan.
export function defaultDate(year: number): string {
    const now = new Date();

    if (year === now.getFullYear()) {
        return now.toLocaleDateString("sv-SE");
    }
    return year + "-01-01";
}

//Builds the draft for a new empty row
export function createNewDraft(year: number): MaterialLineDraft {
    return {
        quantity: "",
        date: defaultDate(year),
        //a line added by hand is an estimate until someone says otherwise
        measurementQuality: "estimated",
        comment: "",
    };
}

// builds the draft from a line that already exists
export function createLineDraft(line: MaterialLine): MaterialLineDraft {
    return {
        quantity: String(line.quantity),
        date: line.date,
        measurementQuality: line.measurementQuality,
        comment: line.comment ?? "",
    };
}

//same limits as MaterialLineRequest in the backend, dont type something it refuses.
//Quantity and date are both required here.
export function findLineErrors(draft: MaterialLineDraft): MaterialLineErrors {
    const errors: MaterialLineErrors = {};

    const quantity = toNumber(draft.quantity);
    if (quantity === null || quantity < 0) {
        errors.quantity = "Ange 0 eller mer.";
    } else if (quantity > 1_000_000_000) {
        errors.quantity = "Högst 1 000 000 000.";
    }

    if (draft.date === "") {
        errors.date = "Välj ett datum."; 
    } else {
        //backend only acepts years 2000-2100 and a date input is always YYYY-MM-DD
        const year = Number(draft.date.slice(0, 4));

        if (year < 2000 || year > 2100) {
            errors.date = "Välj ett datum mellan år 2000 och 2100.";
        }
    }
    return errors;
}

//True when the user has changed something, so the Save-button is only on
// when there is something to save
export function isLineDraftChanged(draft: MaterialLineDraft, line: MaterialLine): boolean {
    return (
        toNumber(draft.quantity) !== line.quantity ||
        draft.date !== line.date ||
        draft.measurementQuality !== line.measurementQuality ||
        draft.comment.trim() !== (line.comment ?? "")
    );
}

//Body for POST and PUT. All five fields
export function lineDraftToRequest(draft: MaterialLineDraft, productId: string): MaterialLineRequest {
    return {
        productId: productId,
        quantity: toNumber(draft.quantity),
        date: draft.date,
        measurementQuality: draft.measurementQuality,
        comment: draft.comment.trim() === "" ? null: draft.comment.trim(),
    };
}

