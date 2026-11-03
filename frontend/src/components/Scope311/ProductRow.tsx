import type { EnergyProductRow } from "../../types/energyConsumption";
import { formatNumber, formatOptional } from "../../utils/format";
import MissingBadges from "./MissingBadges";
import ProductDetails from "./ProductDetails";

//One product in the table, the row itself and the details below it when the row is open
//product = the product to show
//isOpen = true if the details should be shown
//onToggle = called when the row is clicked

type Props = {
    product: EnergyProductRow;
    isOpen: boolean;
    onToggle: () => void;

};

function ProductRow({ product, isOpen, onToggle }: Props) {
    return (
     <>
        <tr className={isOpen ? "energy-row energy-row-open" : "energy-row"} onClick={onToggle}>
            <td className="energy-chevron-cell">
                {/*The arrow turns down when the row is open */}
                <span className="energy-chevron">›</span>
            </td>
            <td>
                <div className="fw-semibold">{product.name}</div>
                {product.articleNo !== null && (
                    <div className="small text-secondary">{product.articleNo}</div>
                )}
                <MissingBadges missing={product.missing}/>
            </td>
            <td>{product.groupName ?? "-"}</td>
            <td className="text-end">{formatNumber(product.quantity)}</td>
            {/* formatOptional shows "-" when data is missing (null) */}
            <td className="text-end fw-semibold">{formatOptional(product.activityKwh)}</td>
            {/*A click in the cell should not open or close the row*/}
            <td className="text-end" onClick={(event) => event.stopPropagation()}>
                <button type="button" className="btn btn-sm energy-edit-button" disabled title="Kommer snart">
                    <i className="bi bi-pencil"></i>
                </button>
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