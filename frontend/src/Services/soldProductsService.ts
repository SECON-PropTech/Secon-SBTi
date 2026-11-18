// API functions for fetching sold products, groups, materials and period entries.

import type {
    SoldProducts,
    ProductGroup,
    ProductMaterials,
    ProductPeriodEntry
} from "../types/soldProducts";

// Fetch all product groups.
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

// Fetch all products with their registered materials and treatments.
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

// Fetch period entries for a specific product and year.
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

// Fetch all sold products and their basic information.
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