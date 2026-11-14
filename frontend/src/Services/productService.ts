// Communicates with ProductsController and ProductGroupsController in backend API
import type { ProductGroup, UpdateProductRequest } from "../types/product";

//Gets the product groups used for the dropdown when a row is edited
export async function getProductGroups(): Promise<ProductGroup[]> {
    const response = await fetch("/api/product-groups");

    if (!response.ok) {
        throw new Error("could not fetch product groups: " + response.status);
    }
    return await response.json();
}

// Saves one product. Backend replaces all five fields, so all of them are always sent.
export async function updateProduct(
    productId: string,
    body: UpdateProductRequest):
Promise<void> {
    const response = await fetch("/api/products/" + productId, {
        method: "PUT",
        headers: { "Content-Type": "application/json"},
        body: JSON.stringify(body),
    });

    if (response.ok) {
        return;
    }

    //400 - the backend found something wrong. It sends one list of messages per field.
    if (response.status === 400) {
        const problem = await response.json();
        throw new Error(firstErrorMessage(problem));
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