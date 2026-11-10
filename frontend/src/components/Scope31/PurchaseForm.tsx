import { useState} from "react";
import type {FormEvent} from "react";
import type {PurchaseFormData, PurchaseType } from "./Types";

/**Information that the form gets from Scope31Overview --- */
type Scope31FormProps = {
    type: PurchaseType;
    initialData?: PurchaseFormData;
    isEditing: boolean;
    selectedPeriod: string;
    onSave: (formData: PurchaseFormData) => void;
    onCancel: () => void;

    //* temporary optional props for error and saving/
    isSaving?: boolean;
    error?: string;
}

/**Values for new registrations */
const emptyForm: PurchaseFormData = {
    name: "",
    supplierId: "",
    accountNo: "",
    amount: "",
    transactionDate: "",
    measurementQuality: "measured",
    comment: "",
}

/**Form used for both purchased goods and services */
export default function PurchaseForm({
    type,
    initialData,
    isEditing,
    selectedPeriod,
    onSave,
    onCancel,
    isSaving = false,
    error = "",
}: Scope31FormProps) {
    //**Storing what the user types into the form */
    const [formData, setFormData] = useState<PurchaseFormData>(
        initialData ?? {...emptyForm}
    )
    const[validationError, setValidationError] = useState("");

    //**update one form field when the user types or selects a value --- */
    function handleChange(field: keyof PurchaseFormData, value: string) {
        setFormData({...formData, [field]: value});
        setValidationError("");
    }

    //** Check the required fields and then send the values to Overview
    //  */
    function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();

        const amount = Number(formData.amount);
        const missingRequiredField =
        !formData.name.trim() ||
        !formData.transactionDate ||
        !formData.amount.trim() ||
        (type === "goods" && !formData.supplierId.trim());

        if (missingRequiredField || !Number.isFinite(amount) || amount <0 ) {
            setValidationError("Kontrollera att alla obligatoriska fält är ifyllda.")
            return;
        }

        // Check that the purchase date matches the selected reporting year.
        if (!formData.transactionDate.startsWith(selectedPeriod + "-")) {
          setValidationError("Datumet måste tillhöra den valda rapporteringsperioden.");
          return;
        }

        setValidationError("");
        //**Sending form data to the parent component, which is Scope31overview.tsx
        //  */
        onSave(formData);
    }
return (
    
    <section>
        {/**This section handles JSX */}
      <h2>
        {isEditing ? "Redigera" : "Lägg till"} {type === "goods" ? "vara" : "tjänst"}
      </h2>

      <form onSubmit={handleSubmit}>
        {/* Name of the good or service */}
        <label htmlFor="purchaseName">{type === "goods" ? "Vara" : "Tjänst"}</label>
        <input
          id="purchaseName"
          type="text"
          value={formData.name}
          onChange={(event) => handleChange("name", event.target.value)}
        />

        {/* NOTE: Goods currently use a supplier ID. Potential dropdown later. */}
        {type === "goods" && (
          <>
            <label htmlFor="supplierId">Leverantörens ID (UUID)</label>
            <input
              id="supplierId"
              type="text"
              value={formData.supplierId}
              onChange={(event) => handleChange("supplierId", event.target.value)}
            />
            <p>Leverantörsväljare kan läggas till när backend är ansluten.</p>
          </>
        )}

        <label htmlFor="accountNo">Konto</label>
        <input
          id="accountNo"
          type="text"
          value={formData.accountNo}
          onChange={(event) => handleChange("accountNo", event.target.value)}
        />

        <label htmlFor="amount">Belopp (SEK)</label>
        <input
          id="amount"
          type="number"
          min="0"
          step="0.01"
          value={formData.amount}
          onChange={(event) => handleChange("amount", event.target.value)}
        />

        <label htmlFor="transactionDate">Datum</label>
        <input
          id="transactionDate"
          type="date"
          min={`${selectedPeriod}-01-01`}
          max={`${selectedPeriod}-12-31`}
          value={formData.transactionDate}
          onChange={(event) => handleChange("transactionDate", event.target.value)}
        />

        <label htmlFor="measurementQuality">Datakvalitet</label>
        <select
          id="measurementQuality"
          value={formData.measurementQuality}
          onChange={(event) => handleChange("measurementQuality", event.target.value)}
        >
          <option value="measured">Uppmätt</option>
          <option value="estimated">Uppskattat</option>
        </select>

        <label htmlFor="comment">Kommentar</label>
        <textarea
          id="comment"
          value={formData.comment}
          onChange={(event) => handleChange("comment", event.target.value)}
        />

        {/* Feedback for validation and future API errors */}
        {validationError && <p role="alert">{validationError}</p>}
        {error && <p role="alert">{error}</p>}

        <button type="submit" disabled={isSaving}>
          {isSaving ? "Sparar..." : isEditing ? "Spara ändringar" : "Lägg till"}
        </button>
        <button type="button" onClick={onCancel} disabled={isSaving}>
          Avbryt
        </button>
      </form>
    </section>
  );
}