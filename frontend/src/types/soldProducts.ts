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
    measurementQuality: "measured" | "estimated";
    comment: string | null;
};

export type ProductGroup = {
    id: string;
    name: string;
    description: string | null;
};

export type SoldProductsManagementView =
    | "products"
    | "materials"
    | "topTen"
    | "projects";

export type Product = {
    id: string;
    articleNo?: string;
    name: string;
    groupId?: string | null;
    quantity: number;
    date: string;
    materials: ProductMaterial[];
    measurementQuality: "measured" | "estimated";
    "billing-status"?: string;
    included?: boolean;
    "life-cycle"?: number;
    "waste-managing"?: string;
    projects?: string[];     
}


export type ProductInclusion = Record<string, boolean>;