import { Fragment, useState } from "react";
import { Calculator, Recycle, TriangleAlert, Pencil } from "lucide-react";
import EditPeriodEntry from "./EditPeriodEntry";
import type { SoldProducts, ProductMaterial, ProductPeriodEntry, UpdateProductPeriodEntry} from "../../types/soldProducts";

// Data sent from FoldOutRow
type Props = {
    product: SoldProducts;
    materials: ProductMaterial[];
    entries: ProductPeriodEntry[];
    quantity: number;
    weightPerUnit: number;
    totalWeight: number;
};

const formatNumber = (value: number) =>
    new Intl.NumberFormat("sv-SE", {
        maximumFractionDigits: 2,
    }).format(value);

// ProductDetails presents all the details for a specific product for the selected time period
function ProductDetails({ product, materials, entries, quantity, weightPerUnit, totalWeight }: Props) {
    
    // Check if the product is missing material information.
    const missingMaterials = materials.length === 0;

    // Tracks which period entry is currently being edited.
    const [editingEntryId, setEditingEntryId] = useState<string | null>(null);
    
    return (
        <div>
            {/* Basic information about the product */}
            <dl className="energy-details-grid">
                <div>
                    <dt>Antal registrerade produkter</dt>
                    <dd>{formatNumber(quantity)} st</dd>
                </div>

                <div>
                    <dt>Materialvikt per produkt</dt>
                    <dd>
                        {missingMaterials ? (
                            <span className="energy-missing-text">
                                <TriangleAlert size={16} className="energy-warning-icon" />
                                {" "}Saknar materialuppgifter
                            </span>
                        ) : (
                            <>{formatNumber(weightPerUnit)} kg</>
                        )}
                    </dd>
                </div>

                <div>
                    <dt>Total materialvikt</dt>
                    <dd>
                        {missingMaterials ? (
                            <span className="energy-missing-text">
                                Ej beräknad
                            </span>
                        ) : (
                            <>{formatNumber(totalWeight)} kg</>
                        )}
                    </dd>
                </div>

                <div>
                    <dt>Mätkvalitet</dt>
                    <dd>
                        {/* Show different colors depending on if the data is measured or estimated */}
                        {product.measurementQuality === "measured" ? (
                            <span className="energy-pill energy-pill-green">Uppmätt</span>
                        ) : (
                            <span className="energy-pill">Uppskattad</span>
                        )}
                    </dd>
                </div>

                <div>
                    <dt>Förväntad livslängd</dt>
                    <dd>
                        {product.lifetimeYears !== null
                            ? `${formatNumber(product.lifetimeYears)} år`
                            : "Ej angiven"}
                    </dd>
                </div>

                <div>
                    <dt>Kommentar</dt>
                    <dd>{product.comment ?? "Ingen kommentar"}</dd>
                </div>
            </dl>
            {/* A list of the materiala and their end-of-life treatment*/}
            <h4 className="fs-6 fw-bold mt-4 mb-3 d-flex align-items-center gap-2">
                <Recycle size={18} />Material och slutbehandling</h4>

            {materials.length === 0 ? (
                <p className="energy-missing-text">Produkten saknar registrerade material.</p>
            ) : (
                <div className="table-responsive">
                    <table className="table table-sm align-middle">
                        <thead>
                            <tr>
                                <th>Material</th>
                                <th className="text-end">Vikt (kg)</th>
                                <th>Slutbehandling</th>
                            </tr>
                        </thead>

                        <tbody>
                            {/*Displays one material per row*/}
                            {materials.map((material) => (
                                <tr key={material.materialId}>
                                    <td>{material.materialName}</td>

                                    <td className="text-end">
                                        {formatNumber(material.weightKg)}
                                    </td>

                                    <td>{material.treatmentName}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
            {/* Shows the registered products for the chosen time period*/}
            <h4 className="fs-6 fw-bold mt-4 mb-3">Periodposter</h4>

            {entries.length === 0 ? (
                <p className="text-secondary">Inga registreringar för vald period.</p>
            ) : (
                <div className="table-responsive">
                    <table className="table table-sm align-middle">
                        <thead>
                            <tr>
                                <th>Datum</th>
                                <th className="text-end">Antal</th>
                                <th>Mätkvalitet</th>
                                <th>Kommentar</th>
                                <th></th> 
                            </tr>
                        </thead>

                        <tbody>
                            {entries.map((entry) => (
                                <Fragment key={entry.id}>
                                    <tr>
                                        <td>{entry.date}</td>

                                        <td className="text-end">
                                            {formatNumber(entry.quantity)}
                                        </td>

                                        <td>
                                            {entry.measurementQuality === "measured"
                                                ? "Uppmätt"
                                                : "Uppskattad"}
                                        </td>

                                        <td>{entry.comment ?? "–"}</td>

                                        <td className="text-end">
                                            <button
                                                type="button"
                                                className="btn btn-sm btn-outline-secondary"
                                                onClick={() => setEditingEntryId(entry.id)}
                                            >
                                                <Pencil size={14} className="me-1" />
                                            </button>
                                        </td>
                                    </tr>

                                    {editingEntryId === entry.id && (
                                        <tr>
                                            <td colSpan={5}>
                                                <EditPeriodEntry
                                                    entry={entry}
                                                    onSave={async (data: UpdateProductPeriodEntry) => {
                                                        // Saving is unavailable until the backend endpoint is ready.
                                                        console.log("Updated period entry:", data);
                                                    
                                                    }}
                                                    onCancel={() => setEditingEntryId(null)}
                                                />
                                            </td>
                                        </tr>
                                    )}
                                </Fragment>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
            {/* Show the calculation only when material information exists. */}
            <div className="energy-calculation mt-3">
                {missingMaterials ? (
                    <>
                        <TriangleAlert size={16} className="energy-warning-icon" />
                        <span>Kan inte beräkna materialvikt – materialuppgifter saknas.</span>
                    </>
                ) : (
                    <>
                        <Calculator size={16} />
                        <span>{formatNumber(quantity)} st</span>
                        <span> × </span>
                        <span>{formatNumber(weightPerUnit)} kg/st</span>
                        <span> = </span>
                        <strong>{formatNumber(totalWeight)} kg</strong>
                    </>
                )}
            </div>
        </div>
    );
}

export default ProductDetails;