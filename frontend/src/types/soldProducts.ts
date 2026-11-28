// Basic information about a sold product from the backend.
export type SoldProducts = {
    id: string;
    articleNo: string | null;
    name: string;
    groupId: string | null;
    groupName: string | null;
    lifetimeYears: number | null;
    measurementQuality: "measured" | "estimated";
    comment: string | null;
}    

// A material belonging to a product, including weight and end-of-life treatment.
export type ProductMaterial = {
    materialId: string;
    materialName: string;
    weightKg: number;
    treatmentId: string;
    treatmentName: string;
};

// A product with all its registered materials.
export type ProductMaterials = {
    productId: string;
    productName: string;
    articleNo: string | null;
    materials: ProductMaterial[];
};

// A registered quantity for a product on a specific date.
export type ProductPeriodEntry = {
    id: string;
    productId: string;
    quantity: number;
    date: string;
    measurementQuality: "measured" | "estimated";
    comment: string | null;
    isManual: boolean;
};

// Information about a product group.
export type ProductGroup = {
    id: string;
    name: string;
    description: string | null;
};

// Available filters for the product table.
export type ProductFilter =
    | "all"
    | "incomplete"
    | "missingMaterials"
    | "missingGroup";

// Available views in the sold products management section.
export type SoldProductsManagementView =
    | "products";

export type ProductInclusion = Record<string, boolean>;

// Data sent when updating a period entry.
export type UpdateProductPeriodEntry = {
    quantity: number;
    date: string;
    measurementQuality: "measured" | "estimated";
    comment: string | null;
};