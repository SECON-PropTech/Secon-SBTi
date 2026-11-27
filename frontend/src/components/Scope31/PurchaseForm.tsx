import { useState} from "react";
import type {FormEvent} from "react";
import type {PurchaseFormData, PurchaseType } from "../../types/purchases";
import {Save, X} from "lucide-react";

/**Information that the form gets from Scope31Overview --- */
type Scope31FormProps = {
    fieldErrors?: Record<string, string[]>;
    type: PurchaseType;
    initialData?: PurchaseFormData;
    isEditing: boolean;
    selectedPeriod: string;
    onSave: (formData: PurchaseFormData) => void;
    onCancel: () => void;

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

const fieldLabels: Record<string, string> = {
  supplierId: "Leverantörens ID (UUID)",
  transactionDate: "Datum",
  value: "Belopp (SEK)",
  unit: "Enhet",
  measurementQuality: "Datakvalitet",
  goodsName: "Vara",
  accountNo: "Konto",
  comment: "Kommentar",
  specification: "Specifikation"
};

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
    fieldErrors = {},
}: Scope31FormProps) {
    //**Storing what the user types into the form */
    const [formData, setFormData] = useState<PurchaseFormData>(
        initialData ?? {...emptyForm}
    )
    const [validationError, setValidationError] = useState("");
    const [localFieldErrors, setLocalFieldErrors] = useState<
        Record<string, string>
    >({});

    // Match backend field names regardless of their first letter's casing.
    function getFieldError(field: string): string {
        if (localFieldErrors[field]) {
            return localFieldErrors[field];
        }

        const backendError = Object.entries(fieldErrors).find(
            ([key]) => key.toLowerCase() === field.toLowerCase()
        );

        if (backendError) {
            return backendError[1].join(" ");
        }

        return "";
    }

    // Keep the Bootstrap error style connected to the relevant field.
    function getFieldClass(field: string): string {
        if (getFieldError(field)) {
            return "form-control is-invalid";
        }

        return "form-control";
    }

    function showFieldError(field: string) {
        const message = getFieldError(field);

        if (!message) return null;

        return (
            <div
                id={`${field}-error`}
                className="invalid-feedback"
                role="alert"
            >
                {message}
            </div>
        );
    }

    //**update one form field when the user types or selects a value --- */
    function handleChange(field: keyof PurchaseFormData, value: string) {
        setFormData({...formData, [field]: value});
        setValidationError("");

        // Local errors are checked again when the form is submitted.
        setLocalFieldErrors({});
    }

    //** Check the required fields and then send the values to Overview
    //  */
    function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();

        if (isSaving) return;

        const errors: Record<string, string> = {};

        // Accept both Swedish decimal commas and decimal points.
        const normalizedAmount = formData.amount.trim().replace(",", ".");
        const amount = Number(normalizedAmount);
        const amountPattern = /^[+-]?(?:\d+(?:\.\d{0,2})?|\.\d{1,2})$/;

        if (type === "services" && !formData.name.trim()) {
            errors.goodsName = "Ange tjänstens namn.";
        }

        if (type === "goods" && !formData.supplierId.trim()) {
            errors.supplierId = "Ange leverantörens ID.";
        }

        if (!normalizedAmount) {
            errors.value = "Ange beloppet i SEK.";
        } else if (
            !amountPattern.test(normalizedAmount) ||
            !Number.isFinite(amount)
        ) {
            errors.value = "Ange ett giltigt belopp med högst två decimaler.";
        } else if (type === "services" && amount < 0) {
            errors.value = "Beloppet för en tjänst får inte vara negativt.";
        }

        if (!formData.transactionDate) {
            errors.transactionDate = "Ange datum.";
        } else if (
            !formData.transactionDate.startsWith(selectedPeriod + "-")
        ) {
            // Check that the purchase date matches the selected reporting year.
            errors.transactionDate =
                "Datumet måste tillhöra den valda rapporteringsperioden.";
        }

        //check supplier ID format before sending to the backend
        const uuidPattern = /^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i;

        if (
            type === "goods" &&
            formData.supplierId.trim() &&
            !uuidPattern.test(formData.supplierId.trim())
        ) {
            errors.supplierId =
                "Ange ett giltigt leverantörs-ID i UUID-format.";
        }

        setLocalFieldErrors(errors);

        if (Object.keys(errors).length > 0) {
            setValidationError("Rätta de markerade fälten och försök igen.");
            return;
        }

        setValidationError("");

        //**Sending form data to the parent component, which is Scope31Overview.tsx
        //  */
        onSave({...formData, amount: normalizedAmount});
    }

    return (
        <section
            className="scope31__form-panel"
            aria-labelledby="purchase-form-heading"
        >
            {/**This section handles JSX */}
            <h2 id="purchase-form-heading" className="fs-4 fw-bold mb-2">
                {isEditing ? "Redigera" : "Lägg till"}{" "}
                {type === "goods" ? "vara" : "tjänst"}
            </h2>

            <p className="small text-secondary mb-4">
                Fält markerade med{" "}
                <span className="scope31__required" aria-hidden="true">*</span>
                {" "}är obligatoriska.
            </p>

            <form onSubmit={handleSubmit} noValidate>
                <div className="row g-3">
                    {/* Name of the good or service */}
                    <div className="col-12">
                        <label htmlFor="purchaseName" className="form-label">
                            {type === "goods" ? "Vara" : "Tjänst"}
                            {type === "services" && (
                                <span
                                    className="scope31__required ms-1"
                                    aria-hidden="true"
                                >
                                    *
                                </span>
                            )}
                        </label>

                        <input
                            id="purchaseName"
                            className={getFieldClass("goodsName")}
                            type="text"
                            value={formData.name}
                            required={type === "services"}
                            aria-invalid={Boolean(getFieldError("goodsName"))}
                            aria-describedby={
                                getFieldError("goodsName")
                                    ? "goodsName-error"
                                    : undefined
                            }
                            onChange={(event) =>
                                handleChange("name", event.target.value)
                            }
                        />

                        {showFieldError("goodsName")}
                    </div>

                    {/* Reminder: Goods currently use a supplier ID. Potential dropdown later. */}
                    {type === "goods" && (
                        <div className="col-12">
                            <label htmlFor="supplierId" className="form-label">
                                Leverantörens ID (UUID)
                                <span
                                    className="scope31__required ms-1"
                                    aria-hidden="true"
                                >
                                    *
                                </span>
                            </label>

                            <input
                                id="supplierId"
                                className={getFieldClass("supplierId")}
                                type="text"
                                value={formData.supplierId}
                                required
                                aria-invalid={Boolean(getFieldError("supplierId"))}
                                aria-describedby={
                                    getFieldError("supplierId")
                                        ? "supplier-help supplierId-error"
                                        : "supplier-help"
                                }
                                onChange={(event) =>
                                    handleChange("supplierId", event.target.value)
                                }
                            />

                            {showFieldError("supplierId")}

                            <p id="supplier-help" className="form-text mb-0">
                                Ange ID för en befintlig leverantör.
                            </p>
                        </div>
                    )}

                    <div className="col-12 col-md-6">
                        <label htmlFor="accountNo" className="form-label">
                            Konto
                        </label>

                        <input
                            id="accountNo"
                            className={getFieldClass("accountNo")}
                            type="text"
                            value={formData.accountNo}
                            aria-invalid={Boolean(getFieldError("accountNo"))}
                            aria-describedby={
                                getFieldError("accountNo")
                                    ? "accountNo-error"
                                    : undefined
                            }
                            onChange={(event) =>
                                handleChange("accountNo", event.target.value)
                            }
                        />

                        {showFieldError("accountNo")}
                    </div>

                    <div className="col-12 col-md-6">
                        <label htmlFor="amount" className="form-label">
                            Belopp (SEK)
                            <span
                                className="scope31__required ms-1"
                                aria-hidden="true"
                            >
                                *
                            </span>
                        </label>

                        {/* A text field prevents the mouse wheel from changing the amount. */}
                        <input
                            id="amount"
                            className={getFieldClass("value")}
                            type="text"
                            inputMode="decimal"
                            value={formData.amount}
                            required
                            aria-invalid={Boolean(getFieldError("value"))}
                            aria-describedby={
                                getFieldError("value")
                                    ? "amount-help value-error"
                                    : "amount-help"
                            }
                            onChange={(event) =>
                                handleChange("amount", event.target.value)
                            }
                        />

                        {showFieldError("value")}

                        <p id="amount-help" className="form-text mb-0">
                            Ange belopp utan tusentalsavgränsare, till exempel 1250,50.
                        </p>
                    </div>

                    <div className="col-12 col-md-6">
                        <label htmlFor="transactionDate" className="form-label">
                            Datum
                            <span
                                className="scope31__required ms-1"
                                aria-hidden="true"
                            >
                                *
                            </span>
                        </label>

                        <input
                            id="transactionDate"
                            className={getFieldClass("transactionDate")}
                            type="date"
                            min={`${selectedPeriod}-01-01`}
                            max={`${selectedPeriod}-12-31`}
                            value={formData.transactionDate}
                            required
                            aria-invalid={Boolean(getFieldError("transactionDate"))}
                            aria-describedby={
                                getFieldError("transactionDate")
                                    ? "transactionDate-error"
                                    : undefined
                            }
                            onChange={(event) =>
                                handleChange("transactionDate", event.target.value)
                            }
                        />

                        {showFieldError("transactionDate")}
                    </div>

                    <div className="col-12 col-md-6">
                        <label htmlFor="measurementQuality" className="form-label">
                            Datakvalitet
                            <span
                                className="scope31__required ms-1"
                                aria-hidden="true"
                            >
                                *
                            </span>
                        </label>

                        <select
                            id="measurementQuality"
                            className={
                                getFieldError("measurementQuality")
                                    ? "form-select is-invalid"
                                    : "form-select"
                            }
                            value={formData.measurementQuality}
                            required
                            aria-invalid={Boolean(getFieldError("measurementQuality"))}
                            aria-describedby={
                                getFieldError("measurementQuality")
                                    ? "measurementQuality-error"
                                    : undefined
                            }
                            onChange={(event) =>
                                handleChange("measurementQuality", event.target.value)
                            }
                        >
                            <option value="measured">Uppmätt</option>
                            <option value="estimated">Uppskattat</option>
                        </select>

                        {showFieldError("measurementQuality")}
                    </div>

                    <div className="col-12">
                        <label htmlFor="comment" className="form-label">
                            Kommentar
                        </label>

                        <textarea
                            id="comment"
                            className={getFieldClass("comment")}
                            rows={4}
                            value={formData.comment}
                            aria-invalid={Boolean(getFieldError("comment"))}
                            aria-describedby={
                                getFieldError("comment")
                                    ? "comment-error"
                                    : undefined
                            }
                            onChange={(event) =>
                                handleChange("comment", event.target.value)
                            }
                        />

                        {showFieldError("comment")}
                    </div>
                </div>

                {validationError && (
                    <p className="alert alert-danger mt-3 mb-0" role="alert">
                        {validationError}
                    </p>
                )}

                {error && (
                    <p className="alert alert-danger mt-3 mb-0" role="alert">
                        {error}
                    </p>
                )}

                {/* Keep the backend's field errors and Swedish field labels. */}
                {Object.entries(fieldErrors).map(([field, messages]) => (
                    <p
                        className="alert alert-danger mt-3 mb-0"
                        role="alert"
                        key={field}
                    >
                        {fieldLabels[
                            Object.keys(fieldLabels).find(
                                (key) => key.toLowerCase() === field.toLowerCase()
                            ) ?? ""
                        ] ?? "Uppgift"}: {messages.join(" ")}
                    </p>
                ))}

                <div className="d-flex flex-wrap gap-2 mt-4">
                    <button
                        type="submit"
                        className="btn scope31__primary d-inline-flex align-items-center gap-2"
                        disabled={isSaving}
                    >
                        <Save size={18} aria-hidden="true" />
                        {isSaving
                            ? "Sparar..."
                            : isEditing
                                ? "Spara ändringar"
                                : "Lägg till"}
                    </button>

                    <button
                        type="button"
                        className="btn scope31__secondary d-inline-flex align-items-center gap-2"
                        onClick={onCancel}
                        disabled={isSaving}
                    >
                        <X size={18} aria-hidden="true" />
                        Avbryt
                    </button>
                </div>
            </form>
        </section>
    );
}