export type Visma4010RowsSummary = {
    count: number;
    netSek: number;
};

export type Visma4010Row = {
    rowNumber: number;
    account: string;
    voucher: string;
    date: string;
    text: string;
    netSek: number;
};

export type Visma4010ImportResponse = {
    fileName: string;
    company: string | null;
    periodFrom: string | null;
    periodTo: string | null;
    isPreliminary: boolean;
    accounts: { number: string; name: string }[];
    canImport: boolean;
    blockers: string[];
    warnings: string[];
    issues: { rowNumber: number; code: string }[];
    previousImport: { importedAt: string; importedBy: string } | null;
    toImport: Visma4010RowsSummary;
    alreadyImported: Visma4010RowsSummary;
    withoutSupplier: Visma4010RowsSummary;
    newSuppliers: string[];
    rowsWithoutSupplier: Visma4010Row[];
    importBatchId: string | null;
};

export type Visma4010ImportOutcome =
    | { kind: "ok"; data: Visma4010ImportResponse }
    | { kind: "blocked"; data: Visma4010ImportResponse }
    | { kind: "error"; message: string };
