export type ImportBatch = {
    id: string;
    source: string;
    importedAt: string;
    importedBy: string;
    fileNames: string[];
    rowCount: number;
};

export type ImportBatchesOutcome =
    | { kind: "ok"; data: ImportBatch[] }
    | { kind: "error"; message: string };

export type RevertImportOutcome =
    | { kind: "ok" }
    | { kind: "error"; message: string };
