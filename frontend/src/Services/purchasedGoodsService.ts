import type {
  Purchase,
  PurchaseFormData,
} from "../components/Scope31/types";

/** Error response from the backend. */
type ApiProblem = {
  message?: string;
  errors?: Record<string, string[]>;
};

/**the saved purchase or information about why saving failed. */
type SavePurchasedGoodResult = {
  purchase: Purchase | null;
  message: string;
  fieldErrors: Record<string, string[]>;
};

/**fetch saved goods for the selected reporting year. */
export async function getPurchasedGoods(
  year: string,
  signal: AbortSignal
): Promise<Purchase[]> {
  const response = await fetch(
    "/api/purchased-goods?year=" + year,
    {
      credentials: "include",
      signal: signal,
    }
  );

  if (response.status === 401) {
    throw new Error("Logga in för att hämta varor.");
  }

  if (response.status === 403) {
    throw new Error("Du saknar behörighet att hämta varor.");
  }

  if (!response.ok) {
    throw new Error("Kunde inte hämta varor.");
  }

  const goods: Purchase[] = await response.json();

  return goods;
}

/** Create a new purchase or update an existing purchase. */
export async function savePurchasedGood(
  formData: PurchaseFormData,
  editingId: string | null
): Promise<SavePurchasedGoodResult> {
  let url = "/api/purchased-goods";
  let method = "POST";

  if (editingId !== null) {
    url = url + "/" + editingId;
    method = "PUT";
  }

  const response = await fetch(url, {
    method: method,
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      supplierId: formData.supplierId.trim(),
      transactionDate: formData.transactionDate,
      value: Number(formData.amount),
      unit: "sek",
      goodsName: formData.name.trim(),
      accountNo: formData.accountNo.trim(),
      measurementQuality: formData.measurementQuality,
      comment: formData.comment.trim(),
    }),
  });

  if (response.status === 401) {
    throw new Error("Logga in för att spara varor.");
  }

  if (response.status === 403) {
    throw new Error("Du saknar behörighet att spara varor.");
  }

  if (!response.ok) {
    const problem: ApiProblem = await response.json().catch(() => ({}));

    let errorMessage = "Kunde inte spara varan.";
    let errors: Record<string, string[]> = {};

    if (problem.message) {
      errorMessage = problem.message;
    }

    if (problem.errors) {
      errors = problem.errors;
      errorMessage = "Rätta uppgifterna nedan och försök igen.";
    }

    return {
      purchase: null,
      message: errorMessage,
      fieldErrors: errors,
    };
  }

  const savedGood: Purchase = await response.json();

  return {
    purchase: savedGood,
    message: "",
    fieldErrors: {},
  };
}