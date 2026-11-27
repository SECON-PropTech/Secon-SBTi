import type {DemoPurchase, PurchaseType} from "../../types/purchases";
import {formatNumber} from "../../utils/format";
import {Pencil} from "lucide-react";

/** Information and actions provided by Scope31Overview. */
type PurchaseTableProps = {
  purchases: DemoPurchase[];
  activeTab: PurchaseType;
  isSaving: boolean;
  onEdit: (id: string) => void;
};

/** Displays purchased goods and demo services. */
export default function PurchaseTable({
  purchases,
  activeTab,
  isSaving,
  onEdit,
}: PurchaseTableProps) {
    return (
        <div className="scope31__table-panel">
            <h2 className="fs-4 fw-bold mb-3">
                {activeTab === "goods" ? "Köpta varor" : "Köpta tjänster"}
            </h2>

            <div
                className="table-responsive scope31__table-scroll"
                role="region"
                aria-label="Registrerade inköp"
                tabIndex={0}
            >
                {/* Simple overview. */}
                <table className="table align-middle mb-0">
                    <thead className="scope31__table-header">
                        <tr>
                            <th scope="col">Datum</th>
                            <th scope="col">
                                {activeTab === "goods" ? "Vara" : "Tjänst"}
                            </th>
                            <th scope="col">Leverantör</th>
                            <th scope="col">Konto</th>
                            <th scope="col" className="text-end">
                                Belopp (SEK)
                            </th>

                            {activeTab === "goods" && (
                                <th scope="col">Kategori</th>
                            )}

                            <th scope="col">Datakvalitet</th>
                            <th scope="col" className="text-end">
                                Åtgärder
                            </th>
                        </tr>
                    </thead>

                    <tbody>
                        {purchases.map((purchase) => (
                            <tr key={purchase.id}>
                                <td className="text-nowrap">
                                    {purchase.data.transactionDate}
                                </td>

                                <td>{purchase.data.name || "–"}</td>

                                <td className="scope31__supplier-cell">
                                    {purchase.data.supplierId || "–"}
                                </td>

                                <td>{purchase.data.accountNo || "–"}</td>

                                <td className="text-end text-nowrap scope31__amount">
                                    {formatNumber(Number(purchase.data.amount))}
                                </td>

                                {/* Show each allocation as a percentage. */}
                                {activeTab === "goods" && (
                                    <td>
                                        {purchase.categories &&
                                        purchase.categories.length > 0 ? (
                                            <ul className="list-unstyled mb-0">
                                                {purchase.categories.map((category) => (
                                                    <li key={category.purchaseCategoryId}>
                                                        {category.name}{" "}
                                                        <span className="text-nowrap">
                                                            {category.share.toLocaleString(
                                                                "sv-SE",
                                                                {
                                                                    style: "percent",
                                                                    maximumFractionDigits: 2,
                                                                }
                                                            )}
                                                        </span>
                                                    </li>
                                                ))}
                                            </ul>
                                        ) : (
                                            <span className="text-secondary">
                                                Saknar kategori
                                            </span>
                                        )}
                                    </td>
                                )}

                                <td>
                                    <span className="scope31__quality">
                                        {purchase.data.measurementQuality === "measured"
                                            ? "Uppmätt"
                                            : "Uppskattat"}
                                    </span>
                                </td>

                                <td className="text-end">
                                    <button
                                        type="button"
                                        className="btn btn-sm scope31__edit-button"
                                        onClick={() => onEdit(purchase.id)}
                                        disabled={isSaving}
                                        aria-label={
                                            "Redigera " +
                                            (purchase.data.name ||
                                                (activeTab === "goods"
                                                    ? "vara"
                                                    : "tjänst"))
                                        }
                                        title="Redigera"
                                    >
                                        <Pencil size={18} aria-hidden="true" />
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </div>
    );
}