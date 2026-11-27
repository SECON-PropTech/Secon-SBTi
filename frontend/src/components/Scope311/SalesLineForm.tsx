import { Check, X } from "lucide-react";
import type { MaterialLineDraft, MaterialLineErrors } from "../../types/materialLine";

type Props = {
    draft: MaterialLineDraft;
    errors: MaterialLineErrors;
    canSave: boolean;
    onChange: (changes: Partial<MaterialLineDraft>) => void;
    onSave: () => void;
    onCancel: () => void;
};

//One row while it is being edited
function SalesLineForm({ draft, errors, canSave, onChange, onSave, onCancel }: Props) {
    const isEmpty = draft.quantity.trim() === "";
    //error message under the field only shows once something is typed
    const quantityError = isEmpty ? undefined: errors.quantity;

    let quantityClassName = "form-control form-control-sm energy-field energy-number-field text-end";
    if (isEmpty) {
        quantityClassName += " energy-field-missing";
    }
    if (quantityError) {
        quantityClassName += " energy-field-error";
    }

    let dateClassName = "form-control form-control-sm energy-field energy-sales-date-field";
    if (errors.date) {
        dateClassName += " energy-field-error";
    }

    //Enter saves, Esc cancels.
    function handleKeyDown(event: React.KeyboardEvent) {
        if (event.key === "Escape") {
            event.preventDefault();
            onCancel();
        }

        if (event.key === "Enter" && (event.target as HTMLElement).tagName !== "BUTTON") {
            event.preventDefault();
            if (canSave) {
                onSave();
            }
        }
    }
    //shows editable fields and validation errors for the sales row
    return (
        <tr className="energy-sales-editing" onKeyDown={handleKeyDown}>
            <td>
                <input type="date" aria-label="Datum"
                className={dateClassName}
                value={draft.date}
                onChange={(event) => onChange({ date: event.target.value})}/>

                {errors.date && <div className="small text-danger mt-1">{errors.date}</div>}
            </td>

            <td>
                <input type="text" inputMode="decimal" aria-label="Antal"
                className={quantityClassName}
                value={draft.quantity}
                placeholder="Saknas"
                autoFocus
                onFocus={(event) => event.target.select()}
                onChange={(event) => onChange({quantity: event.target.value})}/>

                {quantityError && <div className="small text-danger mt-1">{quantityError}</div>}
            </td>
            <td>
                <select aria-label="Datakvalitet"
                className="form-select form-select-sm energy-field"
                value={draft.measurementQuality}
                onChange={(event) => onChange({
                    measurementQuality: event.target.value === "measured" ? "measured" : "estimated"})}>
                    <option value="measured">Verifierad</option>
                    <option value="estimated">Uppskattad</option>
                </select>
            </td>

            <td>
                <input type="text" aria-label="Kommentar"
                className="form-control form-control-sm energy-field"
                value={draft.comment}
                placeholder="Valfritt"
                onChange={(event) => onChange({ comment: event.target.value })}/>
            </td>

            <td className="text-end text-nowrap">
                <button type="button" aria-label="Avbryt"
                className="btn btn-sm energy-edit-button" onClick={onCancel}>
                    <X size={16}/>
                </button>

                <button type="button"
                className="btn btn-sm energy-save-button d-inline-flex align-items-center gap-1"
                disabled={!canSave} onClick={onSave}>
                    <Check size={16}/> Spara
                </button>
            </td>
        </tr>
    );
}

export default SalesLineForm;

