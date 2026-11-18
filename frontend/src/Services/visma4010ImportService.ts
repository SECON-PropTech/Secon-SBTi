import type { Visma4010ImportOutcome, Visma4010ImportResponse } from "../types/visma4010Import";

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
        return { kind: "error", message: "Kunde inte nå servern. Kontrollera att backend körs." };
    }

    if (response.ok) {
        return { kind: "ok", data: await response.json() as Visma4010ImportResponse };
    }

    // 409 = the file has been imported before, 422 = another blocker. Both have the same body as the preview.
    if (response.status === 409 || response.status === 422) {
        return { kind: "blocked", data: await response.json() as Visma4010ImportResponse };
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

    return { kind: "error", message: "Något gick fel: " + response.status };
}
