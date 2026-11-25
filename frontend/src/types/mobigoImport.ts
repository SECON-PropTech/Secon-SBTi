export type MobigoLinesSummary = {
    lines: number;
    rows: number;
    quantity: number;
};

export type MobigoLine = {
    workOrder: string;
    articleNumber: string;
    productName: string;
    isNewProduct: boolean;
    deliveryDate: string;
    quantity: number;
    rowNumbers: number[];
};

export type MobigoChangedLine = {
    workOrder: string;
    articleNumber: string;
    productName: string;
    deliveryDate: string;
    quantity: number;
    importedQuantity: number;
    rowNumbers: number[];
};

export type MobigoSkippedRow = {
    rowNumber: number;
    reason: string;
    workOrder: string;
    status: string | null;
    articleNumber: string | null;
    name: string | null;
    deliveryDate: string | null;
    quantity: number | null;
    unit: string | null;
};

export type MobigoImportResponse = {
    fileName: string;
    deliveryDateFrom: string | null;
    deliveryDateTo: string | null;
    total: number;
    fileTotal: number | null;
    canImport: boolean;
    blockers: string[];
    warnings: string[];
    issues: { rowNumber: number; code: string }[];
    previousImport: { importedAt: string; importedBy: string } | null;
    toImport: MobigoLinesSummary;
    alreadyImported: MobigoLinesSummary;
    linesToImport: MobigoLine[];
    changedLines: MobigoChangedLine[];
    newProducts: { articleNumber: string; name: string }[];
    skippedRows: MobigoSkippedRow[];
    importBatchId: string | null;
};

export type MobigoImportOutcome =
    | { kind: "ok"; data: MobigoImportResponse }
    | { kind: "blocked"; data: MobigoImportResponse }
    | { kind: "error"; message: string };
