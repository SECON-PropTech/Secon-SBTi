import { ChevronRight, Info, TriangleAlert } from "lucide-react";

import type {
    SoldProducts,
    ProductMaterial,
    ProductPeriodEntry,
} from "../../types/soldProducts";

import ProductDetails from "./ProductDetails";

type Props = {
    product: SoldProducts;
    materials: ProductMaterial[];
    entries: ProductPeriodEntry[];
    quantity: number;
    weightPerUnit: number;
    totalWeight: number;
    isOpen: boolean;
    onToggle: () => void;
}

const formatNumber = (value: number) =>
    new Intl.NumberFormat("sv-SE", {
        maximumFractionDigits: 2,
    }).format(value);

function FoldOutRow({
    product,
    materials,
    entries,
    quantity,
    weightPerUnit,
    totalWeight,
    isOpen,
    onToggle,
}: Props) {
    const missingGroup = product.groupId === null;
    const missingMaterials = materials.length === 0;

    return (
        <>
            <tr
                className={
                    isOpen
                        ? "energy-row energy-row-open"
                        : "energy-row"
                }
                onClick={onToggle}
                style={{ cursor: "pointer" }}
            >
                <td className="energy-chevron-cell">
                    <ChevronRight
                        size={18}
                        className="energy-chevron"
                    />
                </td>

                <td>
                    <div className="fw-semibold d-flex align-items-center gap-1">
                        {missingGroup && (
                            <Info
                                size={16}
                                className="text-secondary"
                                aria-label="Saknar produktgrupp"
                            />
                        )}

                        {product.name}
                    </div>

                    {product.articleNo !== null && (
                        <div className="small text-secondary">
                            {product.articleNo}
                        </div>
                    )}
                </td>

                <td>
                    {missingGroup ? (
                        <span className="energy-missing-text">
                            Saknar produktgrupp
                        </span>
                    ) : (
                        product.groupName ?? "Okänd grupp"
                    )}
                </td>

                <td className="text-end">
                    {formatNumber(quantity)}
                </td>

                <td className="text-end">
                    {missingMaterials ? (
                        <TriangleAlert
                            size={18}
                            className="energy-warning-icon"
                            aria-label="Saknar materialuppgifter"
                        />
                    ) : (
                        formatNumber(weightPerUnit)
                    )}
                </td>

                <td className="text-end fw-semibold">
                    {missingMaterials ? (
                        <span className="text-secondary">–</span>
                    ) : (
                        formatNumber(totalWeight)
                    )}
                </td>
            </tr>

            {isOpen && (
                <tr className="energy-details-row">
                    <td></td>
                    <td colSpan={5}>
                        <ProductDetails
                            product={product}
                            materials={materials}
                            entries={entries}
                            quantity={quantity}
                            weightPerUnit={weightPerUnit}
                            totalWeight={totalWeight}
                        />
                    </td>
                </tr>
            )}
        </>
    );
}

export default FoldOutRow;