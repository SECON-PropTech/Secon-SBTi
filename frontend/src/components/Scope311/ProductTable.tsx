import {  useState, useEffect  } from "react";
import type { EnergyProductRow } from "../../types/energyConsumption";
import { formatNumber } from "../../utils/format";
import ProductRow from "./ProductRow"
import Tooltip from "./Tooltip";
import { CircleCheck, Info, TriangleAlert } from "lucide-react";
import type { EditField, ProductDraft } from "../../types/product";
import type { ProductGroup } from "../../types/soldProducts";
import {createDraft, draftToRequest, findDraftErrors, isDraftChanged } from "../../utils/productDraft"
import ConfirmSaveDialog from "./ConfirmSaveDialog";
import { updateProduct } from "../../Services/productService";
import { getProductGroups } from "../../Services/soldProductsService";
//Sold products table
//Click a row to show its details below it
// The chips on top can show only the products that need more data.

type Props = {
    products: EnergyProductRow[];
    total: number;
    year: number;
    onSaved: () => void;
};

function ProductTable({ products, total, year, onSaved }: Props) {
    //The ids of the rows that are open. Empty list = all rows close
    const[openIds, setOpenIds] = useState<string[] > ([]);
    //if True only show the products that need more data
    const[onlyIncomplete, setOnlyIncomplete] = useState(false);

    //The product groups for the dropdown
    const [groups, setGroups] = useState<ProductGroup[] > ([]);
    // The row being edited
    const [editingId, setEditingId] = useState<string | null>(null);
    const [draft, setDraft] = useState<ProductDraft | null>(null);
    const [focusField, setFocusField] = useState <EditField> ("kwhPerYear");

    //Was the row already open before we started editing? if not = close again afterwards
    const [wasOpen, setWasOpen] = useState(false);
    const [pending, setPending] = useState<{productId: string; field: EditField } | null>(null);

    // short messages after saving, green blink, confirmation and errors
    const [flashId, setFlashId] =useState<string | null>(null);
    const [savedName, setSavedName] = useState<string | null>(null);
    const [saveError, setSaveError] = useState<string | null>(null);
    const [groupsError, setGroupsError] = useState<string | null >(null);
    
    // true while a save is on its way to the backend
    const[isSaving, setIsSaving] = useState(false)

    //runs once when the table is shown
    useEffect(() => {
      getProductGroups().then((data) => {
        setGroups(data);
        setGroupsError(null);
      })
      .catch(() => {
        setGroups([]);
        setGroupsError("Kunde inte hämta produktgrupper. Ladda om sidan.");
      });
    }, []);
    
    // Products that lack something and products that are part of the total
    const incompleteCount = products.filter((product) => product.missing.length > 0).length;
    const countedCount = products.filter((product) => product.activityKwh !== null).length;

    // the filter only works when thre is something to show
    const showIncomplete = onlyIncomplete && incompleteCount > 0;
    const visibleProducts = showIncomplete
      ? products.filter((product) => product.missing.length > 0 || product.productId === editingId)
      : products;

    const allOpen = visibleProducts.every((product) => openIds.includes(product.productId))

    // the product being edited and wheter Saving changes should be possible
    const editingProduct = products.find((product) => product.productId === editingId) ?? null;
    const errors = draft ? findDraftErrors(draft) : {};
    const canSave = !isSaving && draft !== null && editingProduct !== null &&
    isDraftChanged(draft, editingProduct) && 
    Object.keys(errors).length === 0;

    //open and closes one row
    function openRow(productId: string) {
      setOpenIds((current) => current.includes(productId) ? current : [...current, productId]);
    } 

    function closeRow(productId: string) {
      setOpenIds((current) => current.filter((openId) => openId !== productId))
    }
        
    //opens a closed table row or closes an open row
    function toggleRow(productId: string) {
        if (openIds.includes(productId)) {
            closeRow(productId)
        } else {
            openRow(productId)
        }
    }

    //open every visible row or close them all if they're already open
    function toggleAll() {
      if (allOpen) {
        setOpenIds(editingId === null ? [] : [editingId]);
      } else {
        setOpenIds(visibleProducts.map((product) => product.productId));
      }
    }

    //starts editing a row
    function startEdit(product: EnergyProductRow, field: EditField) {
      setEditingId(product.productId);
      setDraft(createDraft(product));
      setFocusField(field);
      setWasOpen(openIds.includes(product.productId));
      setSaveError(null);
      openRow(product.productId);
    }
  
    //leaves editing mode and closes the row again if it was closed before
    function closeEdit() {
      if (editingId !== null && !wasOpen) {
        closeRow(editingId);
      }
      setEditingId(null);
      setDraft(null);
      setSaveError(null);
    }
  

    function requestEdit(product: EnergyProductRow, field: EditField) {
      if (editingId === product.productId) {
        setFocusField(field);
        return;
      }
      //unsaved changes in another row, ask first before moving on
      if (draft !== null && editingProduct !== null && isDraftChanged(draft, editingProduct)) {
        setPending({ productId: product.productId, field });
        return
      }
      closeEdit();
      startEdit(product, field);
    }
  
    //one field in the draft was changed
    function changeDraft(changes: Partial<ProductDraft>) {
      if (draft !== null) {
        setDraft({ ...draft, ...changes });
      }
    }
  
    //sends the five fields to the backend. next = the row to edit afterwards
    function save(next: { productId: string; field: EditField } | null) {
      if (draft === null || editingProduct === null) {
        return;
      }
      const savedProduct = editingProduct;

      setIsSaving(true);

      updateProduct(savedProduct.productId, draftToRequest(draft))
        .then(() => {
          setFlashId(savedProduct.productId);
          setSavedName(savedProduct.name);
          closeEdit();
          //the page fetches the report again so the total comes from the backend
          onSaved();
  
          if (next !== null) {
            const nextProduct = products.find((product) => product.productId === next.productId);
            if (nextProduct) {
              startEdit(nextProduct, next.field);
            }
          }
          window.setTimeout(() => setFlashId(null), 1200);
          window.setTimeout(() => setSavedName(null), 3000);
        })
        .catch((error: Error) => setSaveError(error.message))
        .finally(() => setIsSaving(false));
    }

    return (
      <div>
        <div className="d-flex justify-content-between align-items-start gap-3 mb-3">
          <div>
            <h3 className="fs-4 fw-bold mb-2">Sålda produkter</h3>
  
            {/* Filter chips and how many products are part of the total */}
            <div className="d-flex flex-wrap align-items-center gap-2">
              <button
                type="button"
                className={showIncomplete ? "energy-chip" : "energy-chip energy-chip-active"}
                onClick={() => setOnlyIncomplete(false)}>
                Alla <span className="energy-chip-count">{products.length}</span>
              </button>
  
              {/* Hidden when every product is complete */}
              {incompleteCount > 0 && (
                <button
                  type="button"
                  className={showIncomplete ? "energy-chip energy-chip-warning" : "energy-chip"}
                  onClick={() => setOnlyIncomplete(true)}>
                  <TriangleAlert size={16} className="energy-warning-icon" />
                  Behöver kompletteras <span className="energy-chip-count">{incompleteCount}</span>
                </button>
              )}
  
              <span className="d-flex align-items-center gap-1 text-secondary">
                <CircleCheck size={16} className="text-success" />
                {countedCount} av {products.length} i totalen
              </span>
            </div>
          </div>
  
          <button type="button" className="btn energy-outline-button" onClick={toggleAll}>
            {allOpen ? "Fäll ihop alla" : "Fäll ut alla"}
          </button>
        </div>

            {/*Shown when the product groups couldnt be fetched */}
            {groupsError !== null && (
              <div className="alert alert-warning py-2"> {groupsError}</div>
            )}

            {/*shown when backend refused to save */}
           {saveError !== null && (
            <div className="alert alert-danger py-2"> {saveError}</div>
           )}

          {/* table-responsive lets the table scroll sideways on small screens */}
          <div className="table-responsive">
            <table className="table align-middle mb-0">
              {/*energy-table-header makes all header cells blue (EnergyConsumption.css)*/}
              <thead className="energy-table-header">
                <tr>
                  <th className="energy-chevron-cell"></th>
                  <th>Produkt</th>
                  <th>Produktgrupp</th>
                  <th className="text-end">Antal</th>
                  <th className="text-end">
                    Totalt kWh{" "}
                    <Tooltip text="Antal sålda × kWh per år × livslängd">
                      <Info size={14}/>
                    </Tooltip>
                  </th>
                  <th className="text-end">Åtgärder</th>
                </tr>
              </thead>
    
              <tbody>
                {visibleProducts.map((product) => (
                  <ProductRow
                    key={product.productId}
                    product={product}
                    isOpen={openIds.includes(product.productId)}
                    onToggle={() => {
                      //a row being edited shouldnt close when you click it
                      if (editingId !== product.productId) {
                        toggleRow(product.productId);
                      }
                    }}
                    draft={editingId === product.productId ? draft : null}
                    errors={errors}
                    focusField={focusField}
                    groups={groups}
                    canSave={canSave}
                    isFlashing={flashId === product.productId}
                    year={year}
                    onSaved={onSaved}
                    onEdit={(field) => requestEdit(product, field)}
                    onChange={changeDraft}
                    onSave={() => save(null)}
                    onCancel={closeEdit}/>
                ))}              
              </tbody>
    
          <tfoot>
            <tr className="fw-bold">
              <td></td>
              <td colSpan={3}>Totalt</td>
              <td className="text-end">
                <Tooltip text={"Beräknat på " + countedCount + " av " + products.length + " produkter"}>
                  <Info size={16} className="text-secondary" />
                </Tooltip>{" "}
                {formatNumber(total)}
              </td>
              <td></td>
            </tr>
          </tfoot>
        </table>
      </div>

      {/* Asks what to do when another pencil/edit button is clicked with unsaved changes */}
      {pending !== null && editingProduct !== null && (
        <ConfirmSaveDialog
          productName={editingProduct.name}
          canSave={canSave}
          onKeepEditing={() => setPending(null)}
          onDiscard={() => {
            const next = pending;
            const nextProduct = products.find((product) => product.productId === next.productId);
            setPending(null);
            closeEdit();
            if (nextProduct) {
              startEdit(nextProduct, next.field);
            }
          }}
          onSave={() => {
            const next = pending;
            setPending(null);
            save(next);
          }}
        />
      )}

      {/* Short confirmation after saving, bottom right */}
      {savedName !== null && (
        <div className="energy-saved-toast">
          <CircleCheck size={16} /> {savedName} uppdaterad
        </div>
      )}
    </div>
  );
}

export default ProductTable;