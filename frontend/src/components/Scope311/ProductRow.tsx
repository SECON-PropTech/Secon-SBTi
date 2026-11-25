import type { EnergyProductRow } from "../../types/energyConsumption";
import { formatNumber } from "../../utils/format";
import ProductDetails from "./ProductDetails";
import AddButton from "./AddButton";
import Tooltip from "./Tooltip";
import { ChevronRight, Info, Pencil, TriangleAlert, Check, X } from "lucide-react";
import type { DraftErrors, EditField, ProductDraft} from "../../types/product";
import type { ProductGroup } from "../../types/soldProducts";
import { calculateActivityKwh } from "../../utils/calculateActivityKwh";
import { firstMissingField, toNumber } from "../../utils/productDraft";


//product = the product to show
//isOpen = true if the details should be shown
//onToggle = called when the row is clicked

type Props = {
    product: EnergyProductRow;
    isOpen: boolean;
    onToggle: () => void;
    draft: ProductDraft | null;
    errors: DraftErrors;
    focusField: EditField;
    groups: ProductGroup[];
    canSave: boolean;
    isFlashing: boolean;
    year: number;
    onEdit: (field: EditField) => void;
    onChange: (changes: Partial<ProductDraft>) => void;
    onSave: () => void;
    onCancel: () => void;

};

// Explains why a product has no total e.g. "Saknar kwh per år - räknas inte i totalen"
function missingTotalText(product: EnergyProductRow): string {
    const missingValues: string[] = [];
    if (product.missing.includes("kwh")) {
        missingValues.push("kWh per år");
    }
    if (product.missing.includes("lifetime")) {
        missingValues.push("livslängd");
    }
    return "Saknar " + missingValues.join(" och ") + " - räknas inte i totalen";
}

//One product in the table, the row itself and the details below it when the row is open
function ProductRow(props: Props) {
    const { product, isOpen, onToggle, draft, errors, focusField, groups} = props;
    const { canSave, isFlashing, year, onEdit, onChange, onSave, onCancel} = props;
    const isEditing = draft !== null;
    const missingGroup = product.missing.includes("group");

    //(total) counted from the draft while editing, otherwise the value from the backend
    const activityKwh = draft ? calculateActivityKwh(product.quantity,
    toNumber(draft.kwhPerYear), toNumber(draft.lifetimeYears)) : product.activityKwh;

    // total per product, warning icon when it cant be calculated
    let totalCell;
    if (activityKwh === null) {
        totalCell = (
            <Tooltip text={isEditing ? "Fyll i värdena så räknas produkten med" : missingTotalText(product)}>
                <TriangleAlert size={18} className="energy-warning-icon"/>
            </Tooltip>
        );
    } else if (activityKwh === 0) {
        totalCell = <span className="fw-normal text-secondary">0</span>;
    } else {
        totalCell = formatNumber(activityKwh);
    }

    //Enter saves and Esc cancels while the row is being edited
    function handleKeyDown(event: React.KeyboardEvent) {
      if (!isEditing) {
        return;
      }
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

    //classes for the row (being edited, saved or open)
    let rowClassName = "energy-row";
    if (isOpen) {
        rowClassName += " energy-row-open";
    }
    if (isEditing) {
        rowClassName += " energy-row-editing";
    }
    if (isFlashing) {
        rowClassName += " energy-row-saved";
    }

    return (
     <>
        <tr className={rowClassName} onClick={onToggle} onKeyDown={handleKeyDown}>
            <td className="energy-chevron-cell">
                <ChevronRight size={18} className="energy-chevron"/>
            </td>
            <td>
                <div className="fw-semibold d-flex align-items-center gap-1">
                {missingGroup && !isEditing && (
                    <Tooltip text="Saknar produktgrupp">
                        <Info size={16} className="text-secondary"/>
                    </Tooltip>
                ) }
                {product.name}
                </div>
                {product.articleNo !== null && (
                    <div className="small text-secondary">{product.articleNo}</div>
                )}  
            </td>

            {/* stopPropagation() so that rows don't collapse when editing (fields) */}
            <td onClick={(event) => isEditing && event.stopPropagation()}>
                {draft ? (
                    <select aria-label="Produktgrupp" className={draft.groupId === null ?
                        "form-select form-select-sm energy-field energy-field-missing" :
                        "form-select form-select-sm energy-field"}
                        value={draft.groupId ?? ""}
                        autoFocus={focusField === "groupId"}
                        onChange={(event) => onChange({groupId: event.target.value === "" ? null : event.target.value})}>
                        <option value="">Välj grupp</option>
                        {groups.map((group) => (
                            <option key={group.id} value={group.id}>{group.name}</option>
                        ))}
                    </select>): missingGroup ? (
                        <AddButton text="Lägg till grupp" onClick={() => onEdit("groupId")}/>
                    ) : ( product.groupName
                    )}
            </td>
            
            {/* stopPropagation() so that rows don't collapse when editing (buttons) */}
            <td className="text-end">{formatNumber(product.quantity)}</td>
            <td className="text-end fw-semibold">{totalCell}</td>
            <td className="text-end" onClick={(event) => event.stopPropagation()}>
               {isEditing ? (
            <div className="d-inline-flex align-items-center gap-1">
            <Tooltip text="Avbryt (Esc)">
              <button
                type="button"
                aria-label="Avbryt"
                className="btn btn-sm energy-edit-button"
                onClick={onCancel}>
                <X size={18}/>
              </button>
            </Tooltip>
            <button
              type="button"
              className="btn btn-sm energy-save-button d-inline-flex align-items-center gap-1"
              disabled={!canSave}
              onClick={onSave}>
              <Check size={16}/> Spara
            </button>
          </div>                
               ) : ( 
                <Tooltip text="Redigera produkt">
                 <button type="button" 
                 aria-label={"Redigera " + product.name}
                 className="btn btn-sm energy-edit-button"
                 onClick={() => onEdit(firstMissingField(product))}>
                   <Pencil size={18}/>
                 </button>
                </Tooltip>
               )}  
            </td>
        </tr>

        {/*The details is only shown when the row is open*/}
        {isOpen && (
            <tr className={isEditing ? "energy-details-row energy-details-editing" : "energy-details-row"}
            onKeyDown={handleKeyDown}>
                <td></td>
                <td colSpan={5}>
                    <ProductDetails 
                    product={product}
                    draft={draft}
                    errors={errors}
                    focusField={focusField}
                    year={year}
                    onEdit={onEdit}
                    onChange={onChange}/>
                </td>
            </tr>
        )}
     </>   
    );
} 

export default ProductRow;