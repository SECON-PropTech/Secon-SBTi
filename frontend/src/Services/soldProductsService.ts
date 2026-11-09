export type ProductGroup = {
    id: string;
    name: string;
    description: string | null;
};

export async function getProductGroups(): Promise<ProductGroup[]> {
    const response = await fetch("/api/product-groups", {
        method: "GET",
        credentials: "include",
    });

    if (!response.ok) {
        throw new Error("Kunde inte hämta produkter");
    }
    return await response.json() as ProductGroup[];
}