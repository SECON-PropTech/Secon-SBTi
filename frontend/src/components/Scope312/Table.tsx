import { useEffect, useMemo, useState } from "react";
import { CircleCheck, TriangleAlert, Info } from "lucide-react";

import Tooltip from "../Scope311/Tooltip";
import type { SoldProducts, ProductMaterials, ProductPeriodEntry, ProductFilter } from "../../types/soldProducts";
import { getProductPeriod } from "../../Services/soldProductsService";
import FoldOutRow from "./FoldOutRow";

// Data and filter sent from Scope312HomePage.
type Props = {
    products: SoldProducts[];
    productMaterials: ProductMaterials[];
    year: number;
    selectedMonth: string;
};

const formatNumber = (value: number) =>
    new Intl.NumberFormat("sv-SE", { maximumFractionDigits: 2 }).format(value);

// Table shows sold products for the chosen time-period.
function Table({ products, productMaterials, year, selectedMonth }: Props) {
    // Folded out product-rows.
    const [openIds, setOpenIds] = useState<string[]>([]);
// Tracks the selected product filter.
    const [productFilter, setProductFilter] = useState<ProductFilter>("all");
    const [entries, setEntries] = useState<ProductPeriodEntry[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // Fetches period entries from the backend when products or year change.
    useEffect(() => {
        let active = true;

        async function loadEntries() {
            setLoading(true);
            setError(null);
            setEntries([]);

            try {
                const results = await Promise.all(
                    products.map((product) => getProductPeriod(product.id, year))
                );
                // Prevents outdated API requests from updating the component.
                if (active) setEntries(results.flat());
            } catch {
                if (active) setError("Kunde inte hämta periodposter.");
            } finally {
                if (active) setLoading(false);
            }
        }

        loadEntries();

        return () => { active = false; };
    }, [products, year]);

    // Filters periodposter after chosen time-period.
    const filteredEntries = useMemo(() => {
        if (selectedMonth === "all") {
            return entries;
        }
        return entries.filter(
            (entry) => entry.date.split("-")[1] === selectedMonth
        );
    }, [entries, selectedMonth]);

    // Combines each product with its materials and period entries.
    const rows = useMemo(() => products.map((product) => {
            const materialData = productMaterials.find(
                (item) => item.productId === product.id
            );

            const productEntries = filteredEntries.filter(
                (entry) => entry.productId === product.id
            );
            // Sums up all sold product within the chosen time-period.
            const quantity = productEntries.reduce(
                (sum, entry) => sum + entry.quantity, 0
            );
        // Sums up material-weight for a product.
        const materials = materialData?.materials ?? [];
        const weightPerUnit = materials.reduce(
            (sum, material) => sum + material.weightKg, 0
        );

        return {
            product,
            materials,
            entries: productEntries,
            quantity,
            weightPerUnit,
            totalWeight: quantity * weightPerUnit,
            incomplete: product.groupId === null || materials.length === 0,
        };
    // Products that has not been sold will not show.
    }).filter((row) => row.quantity > 0),
    [products, productMaterials, filteredEntries]);
    // Counts all the product with missing data and complete data.
    const incompleteCount = rows.filter((row) => row.incomplete).length;
    const countedCount = rows.filter((row) => row.materials.length > 0).length;
    // Filters witch products that will be shown in the table.
    const visibleRows = rows.filter((row) => {switch (productFilter) {
            case "incomplete": return row.incomplete;
            case "missingMaterials": return row.materials.length === 0;
            case "missingGroup": return row.product.groupId === null;
            default: return true;
        }
    });
    // allOpen will be true when all the FoldOutRows are open.
    const allOpen = visibleRows.length > 0 &&
        visibleRows.every((row) => openIds.includes(row.product.id));
    // Total material-weight for all the product for the chosen time-period.
    const totalWeight = rows.reduce((sum, row) => sum + row.totalWeight, 0);

    // Will fold out or in a spedcifik row.
    function toggleRow(productId: string) {
        setOpenIds((previous) =>
            previous.includes(productId)
                ? previous.filter((id) => id !== productId)
                : [...previous, productId]
        );
    }
    // Will fold out or in all the rows.
    function toggleAll() {
        if (allOpen) {
            setOpenIds([]);
        } else {
            setOpenIds(visibleRows.map((row) => row.product.id));
        }
    }

     // Shows if the page is loading or if an error accured or text saying no product was found.
    if (loading) {
        return <p className="text-secondary">Hämtar periodposter...</p>;
    }

    if (error) {
        return <div className="alert alert-danger">{error}</div>;
    }
        if (rows.length === 0) {
        return <p className="text-secondary mb-0">Inga produkter registrerade för den valda perioden.</p>;
    }

    return (
        <div>
            <div className="d-flex justify-content-between align-items-start gap-3 mb-3">
                <div>
                    <h3 className="fs-4 fw-bold mb-2">Sålda produkter</h3>

                    {/* Filters for products with missing information. */}
                    <div className="d-flex flex-wrap align-items-center gap-2">

                        <button
                            type="button"
                            className={
                                productFilter === "all"
                                    ? "energy-chip energy-chip-active"
                                    : "energy-chip"
                            }
                            onClick={() => setProductFilter("all")}
                        >
                            Alla
                            <span className="energy-chip-count">{rows.length}</span>
                        </button>

                        <button
                            type="button"
                            className={
                                productFilter === "incomplete"
                                    ? "energy-chip energy-chip-warning"
                                    : "energy-chip"
                            }
                            onClick={() => setProductFilter("incomplete")}
                        >
                            <TriangleAlert size={16} className="energy-warning-icon" />
                            Behöver kompletteras
                            <span className="energy-chip-count">{incompleteCount}</span>
                        </button>

                        <button
                            type="button"
                            className={
                                productFilter === "missingMaterials"
                                    ? "energy-chip energy-chip-active"
                                    : "energy-chip"
                            }
                            onClick={() => setProductFilter("missingMaterials")}
                        >
                            Saknar materialuppgifter
                        </button>

                        <button
                            type="button"
                            className={
                                productFilter === "missingGroup"
                                    ? "energy-chip energy-chip-active"
                                    : "energy-chip"
                            }
                            onClick={() => setProductFilter("missingGroup")}
                        >
                            Saknar produktgrupp
                        </button>

                        <span className="d-flex align-items-center gap-1 text-secondary">
                            <CircleCheck size={16} className="text-success" />
                            {countedCount} av {rows.length} med material och registrerat antal
                        </span>

                    </div>
                </div>

                {/* Button for fold out or in all rows */}
                <button
                    type="button"
                    className="btn energy-outline-button"
                    onClick={toggleAll}
                >
                    {allOpen ? "Fäll ihop alla" : "Fäll ut alla"}
                </button>
            </div>

            {visibleRows.length === 0 ? (
                <p className="text-secondary">Inga produkter matchar det valda filtret.</p>
            ) : (
                <div className="table-responsive">
                    <table className="table align-middle mb-0">
                        <thead className="energy-table-header">
                            <tr>
                                <th className="energy-chevron-cell"></th>
                                <th>Produkt</th>
                                <th>Produktgrupp</th>
                                <th className="text-end">Antal</th>
                                <th className="text-end">Vikt/st (kg)</th>
                                <th className="text-end">
                                    Total vikt (kg){" "}
                                    <Tooltip text="Antal registrerade produkter × materialvikt per produkt">
                                        <Info size={14} />
                                    </Tooltip>
                                </th>
                            </tr>
                        </thead>

                        {/* Button for fold out or in, for a specifik product */}
                        <tbody>
                            {visibleRows.map((row) => (
                                <FoldOutRow
                                    key={row.product.id}
                                    product={row.product}
                                    materials={row.materials}
                                    entries={row.entries}
                                    quantity={row.quantity}
                                    weightPerUnit={row.weightPerUnit}
                                    totalWeight={row.totalWeight}
                                    isOpen={openIds.includes(row.product.id)}
                                    onToggle={() => toggleRow(row.product.id)}
                                />
                            ))}
                        </tbody>

                        {/*Shows the total weight for all products in the selected period regardless of the incomplete-data filter.*/}
                        <tfoot>
                            <tr className="fw-bold">
                                <td></td>
                                <td colSpan={4}>Totalt</td>
                                <td className="text-end">
                                    {formatNumber(totalWeight)}
                                </td>
                            </tr>
                        </tfoot>
                    </table>
                </div>
    )}
            </div>
        );
    }


export default Table;
