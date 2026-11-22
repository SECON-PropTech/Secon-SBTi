// Communicates with ProductsController in backend API
import type { UpdateProductRequest } from "../types/product"

// Saves one product. Backend replaces all five fields, so all of them are always sent.
export async function updateProduct(
    productId: string,
    body: UpdateProductRequest):
Promise<void> {
    let response: Response;
    try {
        response = await fetch("/api/products/" + productId, {
            method: "PUT",
            credentials: "include",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body),
        });
    } catch {
        throw new Error("Kunde inte nå servern. Kontrollera anslutningen och försök igen.");
    }

    if (response.ok) {
        return;
    }

    //400 - the backend found something wrong. It sends one list of messages per field.
    if (response.status === 400) {
        let message = "Kunde inte spara. Kontrollera värdena.";

        try {
            message = firstErrorMessage(await response.json());
        } catch {
            //the answer wasnt json, so we keep the general message
        }
        throw new Error(message);
    }   

    if (response.status === 404) {
        throw new Error("Produkten finns inte längre. Ladda om sidan.")

    }
    throw new Error("Kunde inte spara (" + response.status + ")");   
}

//Picks out the first message from the backend
function firstErrorMessage(problem: { errors?: Record<string, string[]> }): string {
    const fields = Object.keys(problem.errors ?? {});

    if (problem.errors && fields.length > 0) {
        return problem.errors[fields[0]][0];
    }
    return "Kunde inte spara. Kontrollera värdena.";
}