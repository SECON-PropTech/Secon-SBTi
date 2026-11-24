import type { ImportBatch, ImportBatchesOutcome, RevertImportOutcome } from "../types/importBatch";

const serverUnreachable = "Kunde inte nå servern. Kontrollera att backend körs.";

export async function getImportBatches(): Promise<ImportBatchesOutcome> {
    let response: Response;
    try {
        response = await fetch("/api/imports", { credentials: "include" });
    } catch {
        return { kind: "error", message: serverUnreachable };
    }

    if (response.ok) {
        return { kind: "ok", data: await response.json() as ImportBatch[] };
    }

    if (response.status === 403) {
        return { kind: "error", message: "Du saknar behörighet att se importer." };
    }

    return { kind: "error", message: otherErrorMessage(response.status) };
}

export async function revertImportBatch(id: string): Promise<RevertImportOutcome> {
    let response: Response;
    try {
        response = await fetch(`/api/imports/${id}`, { method: "DELETE", credentials: "include" });
    } catch {
        return { kind: "error", message: serverUnreachable };
    }

    if (response.ok) {
        return { kind: "ok" };
    }

    if (response.status === 403) {
        return { kind: "error", message: "Du saknar behörighet att ångra importer." };
    }

    if (response.status === 404) {
        return { kind: "error", message: "Importen finns inte längre. Den kan redan ha ångrats." };
    }

    return { kind: "error", message: otherErrorMessage(response.status) };
}

function otherErrorMessage(status: number): string {
    if (status === 401) {
        return "Du är inte inloggad. Logga in igen.";
    }

    return "Något gick fel: " + status;
}
