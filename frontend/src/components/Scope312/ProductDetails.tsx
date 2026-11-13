import { Calculator, Recycle } from "lucide-react";

import type {
    SoldProducts,
    ProductMaterial,
    ProductPeriodEntry,
} from "../../types/soldProducts";

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

function ProductDetails({
    product,
    materials,
    entries,
    quantity,
    weightPerUnit,
    totalWeight,
}: Props) {
    return (
        <div>
            <dl className="energy-details-grid">
                <div>
                    <dt>Antal registrerade produkter</dt>
                    <dd>{formatNumber(quantity)} st</dd>
                </div>

                <div>
                    <dt>Materialvikt per produkt</dt>
                    <dd>{formatNumber(weightPerUnit)} kg</dd>
                </div>

                <div>
                    <dt>Total materialvikt</dt>
                    <dd>{formatNumber(totalWeight)} kg</dd>
                </div>

                <div>
                    <dt>Mätkvalitet</dt>
                    <dd>
                        {product.measurementQuality === "measured" ? (
                            <span className="energy-pill energy-pill-green">
                                Uppmätt
                            </span>
                        ) : (
                            <span className="energy-pill">
                                Uppskattad
                            </span>
                        )}
                    </dd>
                </div>

                <div>
                    <dt>Kommentar</dt>
                    <dd>{product.comment ?? "Ingen kommentar"}</dd>
                </div>
            </dl>

            <h4 className="fs-6 fw-bold mt-4 mb-3 d-flex align-items-center gap-2">
                <Recycle size={18} />
                Material och slutbehandling
            </h4>

            {materials.length === 0 ? (
                <p className="energy-missing-text">
                    Produkten saknar registrerade material.
                </p>
            ) : (
                <div className="table-responsive">
                    <table className="table table-sm align-middle">
                        <thead>
                            <tr>
                                <th>Material</th>
                                <th className="text-end">
                                    Vikt (kg)
                                </th>
                                <th>Slutbehandling</th>
                            </tr>
                        </thead>

                        <tbody>
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

            <h4 className="fs-6 fw-bold mt-4 mb-3">
                Periodposter
            </h4>

            {entries.length === 0 ? (
                <p className="text-secondary">
                    Inga registreringar för vald period.
                </p>
            ) : (
                <div className="table-responsive">
                    <table className="table table-sm align-middle">
                        <thead>
                            <tr>
                                <th>Datum</th>
                                <th className="text-end">Antal</th>
                                <th>Mätkvalitet</th>
                                <th>Kommentar</th>
                            </tr>
                        </thead>

                        <tbody>
                            {entries.map((entry) => (
                                <tr key={entry.id}>
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
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}

            <div className="energy-calculation mt-3">
                <Calculator size={16} />

                <span>{formatNumber(quantity)} st</span>
                <span> × </span>
                <span>{formatNumber(weightPerUnit)} kg/st</span>
                <span> = </span>

                <strong>{formatNumber(totalWeight)} kg</strong>
            </div>
        </div>
    );
}

export default ProductDetails;