//Communicates with MaterialLinesController in the backend API
import type { MaterialLineRequest } from "../types/materialLine";

//Adds a new sales line
export async function createMaterialLine(body: MaterialLineRequest): Promise<void> {
    const response = await send("/api/material-lines", "POST", body);

    await throwIfFailed(response);
}

//Backend replaces all five fields, even if only one line is changed
export async function updateMaterialLine(id: string, body: MaterialLineRequest): Promise<void> {
    const response = await send("/api/material-lines/" + id, "PUT", body);

    await throwIfFailed(response);
}

// shared function for sending API requests
// Handles connection errors.
async function send(url: string, method: string, body?: MaterialLineRequest): Promise<Response> {
    try {
        return await fetch(url, {
            method: method,
            credentials: "include",
            headers: { "Content-Type": "application/json"},
            body: body === undefined ? undefined : JSON.stringify(body),
        });
    } catch {
        throw new Error("Kunde inte nå servern. Kontrollera anslutningen och försök igen.");
    }
    
}

// The answers look the same for all of them, so this is shared too
async function throwIfFailed(response : Response): Promise<void> {
    if (response.ok) {
        return;
    }

    //400. It sends one list of messages per field.
    if (response.status === 400) {
        let message = "Kunde inte spara. Kontrollera värdena.";

        try {
            message = firstErrorMessage(await response.json());
        } catch {
            //Keep the default message if the response is not json
        }
       throw new Error(message);
    }
    
    //404. Someone else removed the line while it was open here
    if (response.status === 404) {
        throw new Error("Raden finns inte längre. Ladda om sidan.");
    }
    throw new Error("Kunde inte spara (" + response.status + ")");
}

//Picks out the first message from the backend
function firstErrorMessage(problem: { errors?: Record<string, string[] > }): string {
    const fields = Object.keys(problem.errors ?? {});

    if (problem.errors && fields.length > 0) {
        return problem.errors[fields[0]][0];
    }
   return "Kunde inte spara. Kontrollera värdena.";
}
