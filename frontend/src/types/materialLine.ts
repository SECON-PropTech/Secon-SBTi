import type { MeasurementQuality } from "./energyConsumption";

//A sales line, same shape as the rows 3.12 reads.
export type { ProductPeriodEntry as MaterialLine } from "./soldProducts";

// The body for POST and PUT. Backend replaces all five fields
export type MaterialLineRequest = {
    productId: string;
    quantity: number | null;
    date: string;
    measurementQuality: MeasurementQuality;
    comment: string | null;
};

//the values while a line is being edited
export type MaterialLineDraft = {
    quantity: string;
    date: string;
    measurementQuality: MeasurementQuality;
    comment: string;
};

// One message per field that is wrong
export type MaterialLineErrors = Partial<Record<"quantity" | "date", string>>;