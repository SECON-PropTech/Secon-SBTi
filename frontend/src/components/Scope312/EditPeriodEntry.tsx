import { useState } from "react";
import type { ProductPeriodEntry, UpdateProductPeriodEntry } from "../../types/soldProducts";

type Props = {
    entry: ProductPeriodEntry;
    onSave: (data: UpdateProductPeriodEntry) => Promise<void>;
    onCancel: () => void;
};

function EditPeriodEntry({ entry, onSave, onCancel }: Props) {
    const [date, setDate] = useState(entry.date);
    const [quantity, setQuantity] = useState(String(entry.quantity));
    const [measurementQuality, setMeasurementQuality] = useState(
        entry.measurementQuality
    );
    const [comment, setComment] = useState(entry.comment ?? "");
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    // Validates the form and sends the updated values to the parent.
    async function handleSubmit(event: React.SubmitEvent<HTMLFormElement>) {
        event.preventDefault();

        const parsedQuantity = Number(quantity);

        if (
            quantity.trim() === "" ||
            !Number.isFinite(parsedQuantity) ||
            parsedQuantity < 0 ||
            parsedQuantity > 1_000_000_000
        ) {
            setError("Ange ett antal mellan 0 och 1 000 000 000.");
            return;
        }

        if (!date || Number(date.slice(0, 4)) < 2000 ||
            Number(date.slice(0, 4)) > 2100) {
            setError("Ange ett datum med ett år mellan 2000 och 2100.");
            return;
        }

        setSaving(true);
        setError(null);

        try {
            await onSave({
                date,
                quantity: parsedQuantity,
                measurementQuality,
                comment: comment.trim() || null,
            });
        } catch {
            setError("Kunde inte spara ändringarna. Försök igen.");
        } finally {
            setSaving(false);
        }
    }

    return (
        <form onSubmit={handleSubmit} className="border rounded p-3 mt-2">
            <h5 className="fs-6 fw-bold mb-3">Redigera periodpost</h5>

            <div className="row g-3">
                <div className="col-md-6">
                    <label className="form-label" htmlFor="entry-date">
                        Datum
                    </label>
                    <input
                        id="entry-date"
                        type="date"
                        className="form-control"
                        min="2000-01-01"
                        max="2100-12-31"
                        value={date}
                        onChange={(event) => setDate(event.target.value)}
                        required
                    />
                </div>

                <div className="col-md-6">
                    <label className="form-label" htmlFor="entry-quantity">
                        Antal
                    </label>
                    <input
                        id="entry-quantity"
                        type="number"
                        className="form-control"
                        min="0"
                        max="1000000000"
                        step="any"
                        value={quantity}
                        onChange={(event) => setQuantity(event.target.value)}
                        required
                    />
                </div>

                <div className="col-md-6">
                    <label className="form-label" htmlFor="entry-quality">
                        Mätkvalitet
                    </label>
                    <select
                        id="entry-quality"
                        className="form-select"
                        value={measurementQuality}
                        onChange={(event) =>
                            setMeasurementQuality(
                                event.target.value as "measured" | "estimated"
                            )
                        }
                    >
                        <option value="measured">Uppmätt</option>
                        <option value="estimated">Uppskattad</option>
                    </select>
                </div>

                <div className="col-12">
                    <label className="form-label" htmlFor="entry-comment">
                        Kommentar
                    </label>
                    <textarea
                        id="entry-comment"
                        className="form-control"
                        rows={3}
                        value={comment}
                        onChange={(event) => setComment(event.target.value)}
                    />
                </div>
            </div>

            {error && (
                <div className="alert alert-danger mt-3 mb-0" role="alert">
                    {error}
                </div>
            )}

            <div className="d-flex justify-content-end gap-2 mt-3">
                <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={onCancel}
                    disabled={saving}
                >
                    Avbryt
                </button>

                <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={saving}
                >
                    {saving ? "Sparar..." : "Spara"}
                </button>
            </div>
        </form>
    );
}

export default EditPeriodEntry;