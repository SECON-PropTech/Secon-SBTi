// what a product lacks
export type MissingField = "kwh" | "lifetime" | "group";

//Measurement quality is either measured or estimated
export type MeasurementQuality = "measured" | "estimated";

// One sold product (Scope311ProductRow in the backend)
export type EnergyProductRow = {
    productId: string;
    articleNo: string | null;
    name: string;
    groupId: string | null;
    groupName: string | null;
    quantity: number; // per year
    kwhPerYear: number | null;
    lifetimeYears: number | null;
    measurementQuality: MeasurementQuality;
    comment: string | null;
    activityKwh: number | null; // quantity * kwh/year * lifetime, null if data is missing
    missing: MissingField[];
};

//Totals for one product group (Scope311GroupSummary in the backend).
export type EnergyGroupSummary = {
    groupId: string | null;
    groupName: string;
    activityKwh: number;
    sharePercent: number;
    productCount: number;
    incompleteCount: number;

};

//The whole responese (Scope311Response in the backend)
export type EnergyConsumptionResponse = {
    year: number;
    totalActivityKwh: number;
    groups: EnergyGroupSummary[];
    products: EnergyProductRow[];
};



