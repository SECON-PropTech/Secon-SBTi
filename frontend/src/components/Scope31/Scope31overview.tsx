import {useState} from "react";
import PurchaseForm from "./PurchaseForm";
import "./scope31.css";
import type {PurchaseFormData, PurchaseType} from "./Types";

//**Row for testing frontend before backend */
type DemoPurchase = {
  id: string;
  type: PurchaseType;
  data: PurchaseFormData;
}

//**Main page for the scope 3.1
// Displays purchased goods and services. */

export default function Scope31Overview() {
  //**The tab in which the user starts by default */
  const [activeTab, setActiveTab] = useState<PurchaseType>("goods");
  const [selectedPeriod, setSelectedPeriod] = useState("2026");
  
  
  //*stores the demo registrations in React state.
  const [purchases, setPurchases] = useState<DemoPurchase[]>([]);


  //**for controlling whether the form is open & which row row is being currently edited */
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [message, setMessage] = useState("");

  //**Shows only rows from selected tab and reporting year chosen by the user */
  const visiblePurchases = purchases.filter((purchase) => {
    return (
      purchase.type === activeTab && 
      purchase.data.transactionDate.startsWith(selectedPeriod)
    );
  })

  //For findinf the row that the user wants to edit IF there is any
  const editingPurchase = visiblePurchases.find((purchase) => purchase.id === editingId)

  //**open a completely empty form for NEW registration */
  function handleAdd() {
    setEditingId(null);
    setIsFormOpen(true);
    setMessage("");
  }

  /** Open form with existing registration (editing) */
  function handleEdit(id: string) {
    setEditingId(id);
    setIsFormOpen(true);
    setMessage("");
  }

  /** close the form without saving. */
  function handleCancel() {
    setIsFormOpen(false);
    setEditingId(null);
  }

  /** for the change between Goods and Services. */
  function handleTabChange(type: PurchaseType) {
    setActiveTab(type);
    handleCancel();
    setMessage("");
  }

  //*Reporting year change*/
  function handlePeriodChange(year: string) {
    setSelectedPeriod(year);
    handleCancel();
    setMessage("");
  }

  //**note: 
  // Temporary function here for frontend testing for saving. 
  //  Connection between API and backend will be done later.*/
  function handleSave(formData: PurchaseFormData) {
      if (editingId !== null) {
        // Update the selected row in the local list.
        setPurchases((currentPurchases) =>
          currentPurchases.map((purchase) => {
            if (purchase.id === editingId) {
              return { ...purchase, data: { ...formData } };
            }
            return purchase;
          })
        );
      } else {
        // Add a new row to the local list.
        const newPurchase: DemoPurchase = {
          id: crypto.randomUUID(),
          type: activeTab,
          data: { ...formData },
        };

        setPurchases((currentPurchases) => [...currentPurchases, newPurchase]);
      }

      setMessage("Ändringen visas i demoläget men är inte sparad i databasen.");
      handleCancel();
    }

    return (
      <main className="scope31">
        <h1>Scope 3.1: Köpta varor och tjänster</h1>
        <p>Registrera och redigera inköp för en vald rapporteringsperiod.</p>
        <p>(Just nu i demoläge. Allt försvinner när sidan laddas om.)</p>

        {/* Reporting period */}
        <label htmlFor="period">Rapporteringsperiod</label>
        <select
          id="period"
          value={selectedPeriod}
          onChange={(event) => handlePeriodChange(event.target.value)}
        >
          <option value="2025">2025</option>
          <option value="2026">2026</option>
        </select>

        {/* Tabs for goods and services */}
        <nav aria-label="Typ av inköp">
          <button
            type="button"
            aria-pressed={activeTab === "goods"}
            onClick={() => handleTabChange("goods")}
          >
            Varor
          </button>
          <button
            type="button"
            aria-pressed={activeTab === "services"}
            onClick={() => handleTabChange("services")}
          >
            Tjänster
          </button>
        </nav>

        {/* Open the form for a new registration */}
        <button type="button" className="scope31__primary" onClick={handleAdd}>
          Lägg till {activeTab === "goods" ? "vara" : "tjänst"}
        </button>

        {message && <p role="status">{message}</p>}

        {/* Simple overview. Backend data can replace the demo list later. */}
        <div
        className="scope31__table-scroll"
        role="region"
        aria-label="Registrerade inköp"
        tabIndex={0}
        
        >
        <table>
          <thead>
            <tr>
              <th>Datum</th>
              <th>Beskrivning</th>
              <th>Leverantör</th>
              <th>Konto</th>
              <th>Belopp (SEK)</th>
              <th>Datakvalitet</th>
              <th>Åtgärder</th>
            </tr>
          </thead>
          <tbody>
            {visiblePurchases.map((purchase) => (
              <tr key={purchase.id}>
                <td>{purchase.data.transactionDate}</td>
                <td>{purchase.data.name}</td>
                <td>{purchase.data.supplierId || "–"}</td>
                <td>{purchase.data.accountNo || "–"}</td>
                <td>{Number(purchase.data.amount).toLocaleString("sv-SE")}</td>
                <td>
                  {purchase.data.measurementQuality === "measured"
                    ? "Uppmätt"
                    : "Uppskattat"}
                </td>
                <td>
                  <button type="button" onClick={() => handleEdit(purchase.id)}>
                    Redigera
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        </div>

        {visiblePurchases.length === 0 && (
          <p>Inga registreringar för den valda perioden.</p>
        )}

        {/* Reuse the same form for adding and editing goods or services. */}
        {isFormOpen && (
          <PurchaseForm
            key={`${activeTab}-${editingId ?? "new"}`}
            type={activeTab}
            initialData={editingPurchase?.data}
            isEditing={editingId !== null}
            selectedPeriod={selectedPeriod}
            onSave={handleSave}
            onCancel={handleCancel}
          />
        )}
      </main>
    );
  }


