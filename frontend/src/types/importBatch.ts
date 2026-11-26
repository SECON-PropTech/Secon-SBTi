export type ImportBatch = {
    id: string;
    source: string;
    importedAt: string;
    importedBy: string;
    fileNames: string[];
    rowCount: number;
    // False for a Mobigo import that has a later Mobigo import, which has to be reverted first.
    canRevert: boolean;
};

export type ImportBatchesOutcome =
    | { kind: "ok"; data: ImportBatch[] }
    | { kind: "error"; message: string };

export type RevertImportOutcome =
    | { kind: "ok" }
    | { kind: "error"; message: string };
