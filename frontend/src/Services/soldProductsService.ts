import type {
    SoldProducts,
    ProductGroup,
    ProductMaterials,
    ProductPeriodEntry
} from "../types/soldProducts";

export async function getProductGroups(): Promise<ProductGroup[]> {
    const response = await fetch("/api/product-groups", {
        method: "GET",
        credentials: "include",
    });

    if (!response.ok) {
        throw new Error("Kunde inte hämta produktgrupper");
    }

    return await response.json() as ProductGroup[];
}

export async function getAllProductMaterials(): Promise<ProductMaterials[]> {
    const response = await fetch("/api/product-materials", {
        method: "GET",
        credentials: "include",
    });

    if (!response.ok) {
        throw new Error("Kunde inte hämta produktmaterial");
    }

    return await response.json() as ProductMaterials[];
}

// Hämtar periodposter för en produkt och ett år
export async function getProductPeriod(
    productId: string,
    year: number
): Promise<ProductPeriodEntry[]> {
    const response = await fetch(
        `/api/product-materials/${productId}/entries?year=${year}`,
        {
            method: "GET",
            credentials: "include",
        }
    );

    if (!response.ok) {
        throw new Error("Kunde inte hämta produktens periodposter");
    }

    return await response.json() as ProductPeriodEntry[];
}

export async function getSoldProducts(): Promise<SoldProducts[]> {
    const response = await fetch("/api/products", {
        method: "GET",
        credentials: "include",
    });

    if (!response.ok) {
        throw new Error("Kunde inte hämta produkter");
    }

    return await response.json() as SoldProducts[];
}