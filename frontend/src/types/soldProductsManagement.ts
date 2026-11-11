export type ProductMaterial = {
    id: string;
    name: string;
    kg: number;
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

export type SoldProductsManagementView =
    | "products"
    | "materials"
    | "topTen"
    | "projects";



export type ProductInclusion = Record<string, boolean | null>;