import type {DemoPurchase, PurchaseType} from "./types";

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
    <div
      className="scope31__table-scroll"
      role="region"
      aria-label="Registrerade inköp"
      tabIndex={0}
    >
      {/* Simple overview. */}
      <table>
        <thead>
          <tr>
            <th>Datum</th>
            <th>Beskrivning</th>
            <th>Leverantör</th>
            <th>Konto</th>
            <th>Belopp (SEK)</th>

            {activeTab === "goods" && <th>Kategori</th>}

            <th>Datakvalitet</th>
            <th>Åtgärder</th>
          </tr>
        </thead>

        <tbody>
          {purchases.map((purchase) => (
            <tr key={purchase.id}>
              <td>{purchase.data.transactionDate}</td>
              <td>{purchase.data.name}</td>
              <td>{purchase.data.supplierId || "–"}</td>
              <td>{purchase.data.accountNo || "–"}</td>

              <td>
                {Number(purchase.data.amount).toLocaleString("sv-SE")}
              </td>

              {/* ADDED: Show each allocation as a percentage. */}
              {activeTab === "goods" && (
                <td>
                  {purchase.categories && purchase.categories.length > 0 ? (
                    <ul>
                      {purchase.categories.map((category) => (
                        <li key={category.purchaseCategoryId}>
                          {category.name}{" "}
                          {category.share.toLocaleString("sv-SE", {
                            style: "percent",
                            maximumFractionDigits: 2,
                          })}
                        </li>
                      ))}
                    </ul>
                  ) : (
                    "Saknar kategori"
                  )}
                </td>
              )}

              <td>
                {purchase.data.measurementQuality === "measured"
                  ? "Uppmätt"
                  : "Uppskattat"}
              </td>

              <td>
                <button
                  type="button"
                  onClick={() => onEdit(purchase.id)}
                  disabled={isSaving}
                >
                  Redigera
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}