export type ProductGroup = {
    id: string;
    name: string;
    description: string | null;
};

export type ProductMaterial = {
    materialId: string;
    materialName: string;
    weightKg: number;
    treatmentId: string;
    treatmentName: string;
};

export type ProductMaterials = {
    productId: string;
    productName: string;
    articleNo: string | null;
    materials: ProductMaterial[];
};

export type ProductPeriodEntry = {
    id: string;
    productId: string;
    quantity: number;
    date: string;
    measurementQuality: string;
    comment: string | null;
}

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

export async function getProductPeriod(
    productId: string,
    year: number
): Promise<ProductPeriodEntry[]> {
        const response = await fetch( `/api/product-materials/${productId}/entries?year=${year}`, {
            method: "GET",
            credentials: "include"
        }
    );

    if (!response.ok) {
        throw new Error("Kunde inte hämta produktregister");
    }

    return await response.json() as ProductPeriodEntry[];
}