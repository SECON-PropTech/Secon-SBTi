/** Represents a purchased good returned by the backend API. */
export type Purchase = {
  id: string;
  supplierId: string;
  transactionDate: string;
  value: number;
  unit: "sek";
  measurementQuality: "measured" | "estimated";
  comment: string | null;
  goodsName: string | null;
  specification: string | null;
  accountNo: string | null;
  createdBy: string;
  createdAt: string;
  categories?: { purchaseCategoryId: string; name: string; share: number }[];
};

/** Temporary service type until the service API is finished. */
export type PurchasedService = {
  id: string;
  serviceName: string;
  accountNo: string | null;
  transactionDate: string;
  amountSek: number;
  measurementQuality: "measured" | "estimated";
  comment: string | null;
};

export type PurchaseType = "goods" | "services";

/** Shared fields used by the form. */
export type PurchaseFormData = {
  name: string;
  supplierId: string;
  accountNo: string;
  amount: string;
  transactionDate: string;
  measurementQuality: "measured" | "estimated";
  comment: string;
};