import type { Visma4010ImportOutcome, Visma4010ImportResponse } from "../types/visma4010Import";

const retryMessage = "Resultatet kunde inte bekräftas. " +
    "Förhandsgranska filen igen innan du försöker importera på nytt.";

export function previewVisma4010(file: File): Promise<Visma4010ImportOutcome> {
    return upload("/api/imports/visma-4010/preview", file);
}

export function importVisma4010(file: File): Promise<Visma4010ImportOutcome> {
    return upload("/api/imports/visma-4010", file);
}

async function upload(path: string, file: File): Promise<Visma4010ImportOutcome> {
    const formData = new FormData();
    formData.append("file", file);

    let response: Response;
    try {
        // No Content-Type header, the browser sets it together with the boundary of the form.
        response = await fetch(path, {
            method: "POST",
            credentials: "include",
            body: formData,
        });
    } catch {
        return { kind: "error", message: retryMessage };
    }

    // 409 = the file has been imported before, 422 = another blocker. Both have the same body as the preview.
    if (response.ok || response.status === 409 || response.status === 422) {
        try {
            const data = await response.json() as Visma4010ImportResponse;
            return response.ok ? { kind: "ok", data } : { kind: "blocked", data };
        } catch {
            return { kind: "error", message: retryMessage };
        }
    }

    if (response.status === 400) {
        const problem = await response.json().catch(() => null) as { errors?: { file?: string[] } } | null;
        return { kind: "error", message: problem?.errors?.file?.[0] ?? "Filen kunde inte läsas." };
    }

    if (response.status === 401) {
        return { kind: "error", message: "Du är inte inloggad. Logga in igen." };
    }

    if (response.status === 403) {
        return { kind: "error", message: "Du saknar behörighet att importera från Visma." };
    }

    if (response.status === 413) {
        return { kind: "error", message: "Filen är större än 10 MB." };
    }

    if (response.status >= 500) {
        const problem = await response.json().catch(() => null) as { detail?: unknown } | null;
        const detail = problem?.detail;
        return {
            kind: "error",
            message: typeof detail === "string" && detail.trim() ? detail : retryMessage,
        };
    }

    return { kind: "error", message: "Något gick fel: " + response.status };
}
