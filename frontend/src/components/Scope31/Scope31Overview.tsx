import {useEffect, useRef, useState} from "react";
import PurchaseForm from "./PurchaseForm";
import PurchaseTable from "./PurchaseTable";
import "./scope31.css";

import type {
  Purchase,
  PurchaseFormData,
  PurchaseType,
  DemoPurchase,
} from "../../types/purchases";

import {
  getPurchasedGoods,
  savePurchasedGood,
} from "../../Services/purchasedGoodsService";

// Convert the API response to the format used by the table.
// Used after both fetching and saving a purchase.
function toPurchaseRow(good: Purchase): DemoPurchase {
  return {
    id: good.id,
    type: "goods",
    categories: good.categories,
    data: {
      name: good.goodsName ?? "",
      supplierId: good.supplierId,
      accountNo: good.accountNo ?? "",
      amount: String(good.value),
      transactionDate: good.transactionDate,
      measurementQuality: good.measurementQuality,
      comment: good.comment ?? "",
    },
  };
}

//**Main page for the scope 3.1
// Displays purchased goods and services. */

export default function Scope31Overview() {
  //**The tab in which the user starts by default */
  const [activeTab, setActiveTab] = useState<PurchaseType>("goods");
  const [selectedPeriod, setSelectedPeriod] = useState("2026");

  const [purchases, setPurchases] = useState<DemoPurchase[]>([]);

  //**for controlling whether the form is open & which row is being currently edited */
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [message, setMessage] = useState("");

  const [isLoading, setIsLoading] = useState(false);
  const [loadError, setLoadError] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState("");

  // Keep field errors from the API and prevent duplicate save requests.
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const savingRef = useRef(false);

  // Fetch saved goods when the Goods tab or report year changes.
  useEffect(() => {
    if (activeTab !== "goods") return;

    const controller = new AbortController();

    async function loadGoods() {
      setIsLoading(true);
      setLoadError("");

      // Clear previously loaded goods but keep demo services.
      setPurchases((currentPurchases) =>
        currentPurchases.filter((purchase) => purchase.type === "services")
      );

      try {
        const goods = await getPurchasedGoods(
          selectedPeriod,
          controller.signal
        );

        const savedPurchases = goods.map(toPurchaseRow);

        if (controller.signal.aborted) return;

        setPurchases((currentPurchases) => [
          ...currentPurchases.filter(
            (purchase) => purchase.type === "services"
          ),
          ...savedPurchases,
        ]);
      } catch (error) {
        if (controller.signal.aborted) return;

        if (error instanceof Error) {
          setLoadError(error.message);
        } else {
          setLoadError("Kunde inte hämta varor.");
        }
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      }
    }

    void loadGoods();

    // Cancel the request if the user changes tab or reporting year.
    return () => controller.abort();
  }, [activeTab, selectedPeriod]);

  //**Shows only rows from selected tab and reporting year chosen by the user */
  const visiblePurchases = purchases.filter((purchase) => {
    return (
      purchase.type === activeTab &&
      purchase.data.transactionDate.startsWith(selectedPeriod)
    );
  });

  // Find the row that the user wants to edit, if there is any.
  const editingPurchase = visiblePurchases.find(
    (purchase) => purchase.id === editingId
  );

  //**open a completely empty form for NEW registration and clear error for save attempt */
  function handleAdd() {
    if (savingRef.current || (activeTab === "goods" && isLoading)) return;

    setFieldErrors({});
    setSaveError("");
    setEditingId(null);
    setIsFormOpen(true);
    setMessage("");
  }

  /** Open form with existing registration (editing) */
  function handleEdit(id: string) {
    if (savingRef.current) return;

    setFieldErrors({});
    setSaveError("");
    setEditingId(id);
    setIsFormOpen(true);
    setMessage("");
  }

  /** close the form without saving. */
  function handleCancel() {
    setFieldErrors({});
    setSaveError("");
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

  // Save goods through the API.
  async function handleSave(formData: PurchaseFormData) {
    if (savingRef.current || (activeTab === "goods" && isLoading)) return;

    if (activeTab === "goods") {
      setIsSaving(true);
      savingRef.current = true;
      setFieldErrors({});
      setSaveError("");
      setMessage("");

      try {
        const result = await savePurchasedGood(formData, editingId);

        // Keep the form open if the backend rejects the registration.
        if (result.purchase === null) {
          setFieldErrors(result.fieldErrors);
          setSaveError(result.message);
          return;
        }

        // Use the saved values and ID returned by the backend.
        const savedPurchase = toPurchaseRow(result.purchase);

        setPurchases((currentPurchases) => {
          if (editingId !== null) {
            return currentPurchases.map((purchase) => {
              if (purchase.id === editingId) {
                return savedPurchase;
              }

              return purchase;
            });
          }

          return [...currentPurchases, savedPurchase];
        });

        setMessage("Varan har sparats.");
        handleCancel();
      } catch (error) {
        if (error instanceof Error) {
          setSaveError(error.message);
        } else {
          setSaveError("Kunde inte spara varan.");
        }
      } finally {
        savingRef.current = false;
        setIsSaving(false);
      }

      return;
    }

    //For service API (WIP)
    if (editingId !== null) {
      // Update selected row in the local list.
      setPurchases((currentPurchases) =>
        currentPurchases.map((purchase) => {
          if (purchase.id === editingId) {
            return {...purchase, data: {...formData}};
          }

          return purchase;
        })
      );
    } else {
      // Add new row to the local list.
      const newPurchase: DemoPurchase = {
        id: crypto.randomUUID(),
        type: activeTab,
        data: {...formData},
      };

      setPurchases((currentPurchases) => [
        ...currentPurchases,
        newPurchase,
      ]);
    }

    setMessage("Tjänsten visas i demoläge och är inte sparad i databasen.");
    handleCancel();
  }

  return (
    <main className="scope31">
      <h1>Scope 3.1: Köpta varor och tjänster</h1>
      <p>Registrera och redigera inköp för en vald rapporteringsperiod.</p>

      {activeTab === "services" && (
        <p>(Just nu i demoläge. Allt försvinner när sidan laddas om.)</p>
      )}

      {/* Reporting period */}
      <label htmlFor="period">Rapporteringsperiod</label>
      <select
        id="period"
        value={selectedPeriod}
        disabled={isSaving}
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
          disabled={isSaving}
          onClick={() => handleTabChange("goods")}
        >
          Varor
        </button>

        <button
          type="button"
          aria-pressed={activeTab === "services"}
          disabled={isSaving}
          onClick={() => handleTabChange("services")}
        >
          Tjänster
        </button>
      </nav>

      {/* Open the form for a new registration */}
      <button
        type="button"
        className="scope31__primary"
        onClick={handleAdd}
        disabled={isSaving || (activeTab === "goods" && isLoading)}
      >
        Lägg till {activeTab === "goods" ? "vara" : "tjänst"}
      </button>

      {message && <p role="status">{message}</p>}

      {activeTab === "goods" && isLoading && (
        <p role="status">Hämtar varor...</p>
      )}

      {activeTab === "goods" && loadError && (
        <p role="alert">{loadError}</p>
      )}

      <PurchaseTable
        purchases={visiblePurchases}
        activeTab={activeTab}
        isSaving={isSaving}
        onEdit={handleEdit}
      />

      {visiblePurchases.length === 0 &&
        (activeTab === "services" || (!isLoading && !loadError)) && (
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
          isSaving={isSaving}
          error={saveError}
          fieldErrors={fieldErrors}
        />
      )}
    </main>
  );
}
