import type { EnergyProductRow } from "../../types/energyConsumption";
import { formatNumber } from "../../utils/format";
import ProductDetails from "./ProductDetails";
import AddButton from "./AddButton";
import Tooltip from "./Tooltip";
import { ChevronRight, Info, Pencil, TriangleAlert } from "lucide-react";


//product = the product to show
//isOpen = true if the details should be shown
//onToggle = called when the row is clicked

type Props = {
    product: EnergyProductRow;
    isOpen: boolean;
    onToggle: () => void;

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
function ProductRow({ product, isOpen, onToggle }: Props) {
    const missingGroup = product.missing.includes("group");

    //(total kWh) a warning icon when it can't be calculated, 0 or the number
    let totalCell;
    if (product.activityKwh === null) {
        totalCell = (
            <Tooltip text={missingTotalText(product)}>
                <TriangleAlert size={18} className="energy-warning-icon"/>
            </Tooltip>
        );
    } else if (product.activityKwh === 0) {
        totalCell = <span className="fw-normal text-secondary">0</span>;
    } else {
        totalCell = formatNumber(product.activityKwh);
    }


    return (
     <>
        <tr className={isOpen ? "energy-row energy-row-open" : "energy-row"} onClick={onToggle}>
            <td className="energy-chevron-cell">
                <ChevronRight size={18} className="energy-chevron"/>
            </td>
            <td>
                <div className="fw-semibold d-flex align-items-center gap-1">
                {missingGroup && (
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
            <td>{missingGroup ? <AddButton text="Lägg till grupp"/> : product.groupName}</td>
            <td className="text-end">{formatNumber(product.quantity)}</td>
            <td className="text-end fw-semibold">{totalCell}</td>
            
            <td className="text-end" onClick={(event) => event.stopPropagation()}>
            {/* TODO: open the edit form (not built yet, disabled for now) */}
                <Tooltip text="Redigera produkt">
                 <button type="button" className="btn btn-sm energy-edit-button" disabled>
                   <Pencil size={18}/>
                 </button>
                </Tooltip>  
            </td>
        </tr>

        {/*The details is only shown when the row is open*/}
        {isOpen && (
            <tr className="energy-details-row">
                <td></td>
                <td colSpan={5}>
                    <ProductDetails product={product}/>
                </td>
            </tr>
        )}
     </>   
    );
} 

export default ProductRow;