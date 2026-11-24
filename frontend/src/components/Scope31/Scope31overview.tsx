import {useEffect, useRef, useState} from "react";
import PurchaseForm from "./PurchaseForm";
import "./scope31.css";
import type {Purchase, PurchaseFormData, PurchaseType} from "./Types";


type DemoPurchase = {
  id: string;
  type: PurchaseType;
  data: PurchaseFormData;
  // Keep the supplier's category allocations from the API.
  categories?: Purchase["categories"];
}

//**Main page for the scope 3.1
// Displays purchased goods and services. */

export default function Scope31Overview() {
  //**The tab in which the user starts by default */
  const [activeTab, setActiveTab] = useState<PurchaseType>("goods");
  const [selectedPeriod, setSelectedPeriod] = useState("2026");
  
  
  
  const [purchases, setPurchases] = useState<DemoPurchase[]>([]);


  //**for controlling whether the form is open & which row row is being currently edited */
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [message, setMessage] = useState("");

  const [isLoading, setIsLoading] = useState(false);
  const [loadError, setLoadError] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState("")
  // Keep field errors from the API and prevent duplicate save requests.
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const savingRef = useRef(false);


  //fetch saved goods when the Goods tab or report year changes
  useEffect(() => {
    if (activeTab !== "goods") return;
    
    const controller = new AbortController();

    async function loadGoods() {
      setIsLoading(true);
      setLoadError("");

      //previouslt loaded goods clears but keep demo services
      setPurchases((currentPurchases) =>
      currentPurchases.filter((purchase) => purchase.type === "services")
    );

    try {
      const response = await fetch(
        "/api/purchased-goods?year=" + selectedPeriod,
        {
          credentials: "include",
          signal: controller.signal,
        }
      )

      if (response.status === 401) {
        throw new Error("Logga in för att hämta varor.");
      }

      if (response.status === 403) {
        throw new Error("Du saknar behörighet att hämta varor.")
      }

      if (!response.ok) {
        throw new Error("Kunde inte hämta varor.")
      }

      const goods: Purchase[] = await response.json();

      //convert the API response to the format used by the table
      const savedPurchases: DemoPurchase[] = goods.map((good) => ({
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
      }));

      if (controller.signal.aborted) return;

      setPurchases((currentPurchases) => [
        ...currentPurchases.filter((purchases) => purchases.type ===
      "services"),
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

    //Cancel the request is the user changes tab or the reporting year.
    return () => controller.abort();
  }, [activeTab, selectedPeriod]);
  

  //**Shows only rows from selected tab and reporting year chosen by the user */
  const visiblePurchases = purchases.filter((purchase) => {
    return (
      purchase.type === activeTab && 
      purchase.data.transactionDate.startsWith(selectedPeriod)
    );
  })

  //For findinf the row that the user wants to edit IF there is any
  const editingPurchase = visiblePurchases.find((purchase) => purchase.id === editingId)

  //**open a completely empty form for NEW registration and clear error for save attempt */
  function handleAdd() {
    if (savingRef.current || (activeTab === "goods" && isLoading)) return;
    setFieldErrors({});
    setSaveError("")
    setEditingId(null);
    setIsFormOpen(true);
    setMessage("");
  }

  /** Open form with existing registration (editing) */
  function handleEdit(id: string) {
    if (savingRef.current) return;
    setFieldErrors({});
    setSaveError("")
    setEditingId(id);
    setIsFormOpen(true);
    setMessage("");
  }

  /** close the form without saving. */
  function handleCancel() {
    setFieldErrors({});
    setSaveError("")
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


  async function handleSave(formData: PurchaseFormData) {
 
    if (savingRef.current || (activeTab === "goods" && isLoading)) return;


    if (activeTab === "goods") {

      
      setIsSaving(true);
      savingRef.current = true;
      setFieldErrors({});
      setSaveError("");
      setMessage("");

      

      try {
        let url = "/api/purchased-goods";
        let method = "POST";

        if (editingId !== null) {
          url = url + "/" + editingId;
          method = "PUT";
        }

        const response = await fetch(url, {
          method: method,
          credentials: "include",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            supplierId: formData.supplierId.trim(),
            transactionDate: formData.transactionDate,
            value: Number(formData.amount),
            unit: "sek",
            goodsName: formData.name.trim(),
            accountNo: formData.accountNo.trim(),
            measurementQuality: formData.measurementQuality,
            comment: formData.comment.trim(),
          }),
        });

        if (response.status === 401) {
          throw new Error("Logga in för att spara varor.");
        }

        if (response.status === 403) {
          throw new Error("Du saknar behörighet att spara varor.");
        }

        if (!response.ok) {
          const problem: {
            message?: string;
            errors?: Record<string, string[]>;
          } = await response.json().catch(() => ({}));

          let errorMessage = "Kunde inte spara varan.";

          if (problem.message) {
            errorMessage = problem.message;
          }

          if (problem.errors) {
            setFieldErrors(problem.errors);
            errorMessage = "Rätta uppgifterna nedan och försök igen.";
          }

          throw new Error(errorMessage);
        }

        const savedGood: Purchase = await response.json();

        // Use the saved values and ID returned by the backend.
        const savedPurchase: DemoPurchase = {
          id: savedGood.id,
          type: "goods",
          categories: savedGood.categories,
          data: {
            name: savedGood.goodsName ?? "",
            supplierId: savedGood.supplierId,
            accountNo: savedGood.accountNo ?? "",
            amount: String(savedGood.value),
            transactionDate: savedGood.transactionDate,
            measurementQuality: savedGood.measurementQuality,
            comment: savedGood.comment ?? "",
          },
        };

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
      //update selected row in the local list.
      setPurchases((currentPurchases) =>
      currentPurchases.map((purchase) => {
        if (purchase.id === editingId) {
          return { ...purchase, data: {...formData}};
        }

        return purchase;
      })
    );
  } else {
    //Add new row to the local list
    const newPurchase: DemoPurchase = {
      id: crypto.randomUUID(),
      type: activeTab,
      data: {...formData},
    };

    setPurchases((currentPurchases) => [...currentPurchases, newPurchase]);
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
        <button type="button" className="scope31__primary" onClick={handleAdd} disabled={isSaving || (activeTab === "goods" && isLoading)}>
          Lägg till {activeTab === "goods" ? "vara" : "tjänst"}
        </button>

        {message && <p role="status">{message}</p>}
        {activeTab === "goods" && isLoading && (
          <p role="status">Hämtar varor...</p>
        )}

        {activeTab === "goods" && loadError && (
          <p role="alert">{loadError}</p>
        )}

        {/* Simple overview. */}
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

              {activeTab === "goods" && <th>Kategori</th>}
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
                  <button type="button" onClick={() => handleEdit(purchase.id)} disabled={isSaving}>
                    Redigera
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        </div>

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
