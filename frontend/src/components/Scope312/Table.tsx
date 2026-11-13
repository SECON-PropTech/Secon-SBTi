import { useEffect, useMemo, useState } from "react";
import { CircleCheck, TriangleAlert, Info } from "lucide-react";

import Tooltip from "../Scope311/Tooltip";
import type { SoldProducts, ProductMaterials, ProductPeriodEntry } from "../../types/soldProducts";
import { getProductPeriod } from "../../Services/soldProductsService";
import FoldOutRow from "./FoldOutRow";

type Props = {
    products: SoldProducts[];
    productMaterials: ProductMaterials[];
    year: number;
    selectedMonth: string;
};

const formatNumber = (value: number) =>
    new Intl.NumberFormat("sv-SE", { maximumFractionDigits: 2 }).format(value);

function Table({ products, productMaterials, year, selectedMonth }: Props) {
    const [openIds, setOpenIds] = useState<string[]>([]);
    const [onlyIncomplete, setOnlyIncomplete] = useState(false);
    const [entries, setEntries] = useState<ProductPeriodEntry[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // Hämtar periodposter för alla produkter under valt år.
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

    // Filtrera registreringar efter vald månad.
    const filteredEntries = useMemo(() => {
        if (selectedMonth === "all") {
            return entries;
        }
        return entries.filter(
            (entry) => entry.date.split("-")[1] === selectedMonth
        );
    }, [entries, selectedMonth]);

    // Koppla material och periodposter till respektive produkt.
    const rows = useMemo(() => products.map((product) => {
            const materialData = productMaterials.find(
                (item) => item.productId === product.id
            );

            const productEntries = filteredEntries.filter(
                (entry) => entry.productId === product.id
            );

            const quantity = productEntries.reduce(
                (sum, entry) => sum + entry.quantity, 0
            );

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
    }).filter((row) => row.quantity > 0),
    [products, productMaterials, filteredEntries]);

    const incompleteCount = rows.filter((row) => row.incomplete).length;
    const countedCount = rows.filter((row) => row.materials.length > 0).length;
    const showIncomplete = onlyIncomplete && incompleteCount > 0;
    const visibleRows = showIncomplete ? rows.filter((row) => row.incomplete) : rows;

    const allOpen = visibleRows.length > 0 &&
        visibleRows.every((row) => openIds.includes(row.product.id));

    const totalWeight = rows.reduce((sum, row) => sum + row.totalWeight, 0);

    function toggleRow(productId: string) {
        setOpenIds((previous) =>
            previous.includes(productId)
                ? previous.filter((id) => id !== productId)
                : [...previous, productId]
        );
    }

    function toggleAll() {
        if (allOpen) {
            setOpenIds([]);
        } else {
            setOpenIds(visibleRows.map((row) => row.product.id));
        }
    }

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

                    <div className="d-flex flex-wrap align-items-center gap-2">
                        <button
                            type="button"
                            className={showIncomplete ? "energy-chip" : "energy-chip energy-chip-active"}
                            onClick={() => setOnlyIncomplete(false)}
                        >
                            Alla <span className="energy-chip-count">{rows.length}</span>
                        </button>

                        {incompleteCount > 0 && (
                            <button
                                type="button"
                                className={showIncomplete
                                        ? "energy-chip energy-chip-warning"
                                        : "energy-chip"
                                }
                                onClick={() => setOnlyIncomplete(true)}
                            >
                                <TriangleAlert size={16} className="energy-warning-icon" />
                                Behöver kompletteras
                                <span className="energy-chip-count">{incompleteCount}</span>
                            </button>
                        )}

                        <span className="d-flex align-items-center gap-1 text-secondary">
                            <CircleCheck size={16} className="text-success"/>
                            {countedCount} av {rows.length} med material och registrerat antal
                        </span>
                    </div>
                </div>

                <button
                    type="button"
                    className="btn energy-outline-button"
                    onClick={toggleAll}
                >
                    {allOpen ? "Fäll ihop alla" : "Fäll ut alla"}
                </button>
            </div>

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
        </div>
    );
}

export default Table;
