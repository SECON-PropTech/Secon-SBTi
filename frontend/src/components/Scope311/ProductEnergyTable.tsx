import { useEffect, useRef, useState  } from "react";
import type { EnergyProductRow } from "../../types/energyConsumption";
import { formatNumber } from "../../utils/format";
import ProductRow from "./ProductRow"

//Sold products table
//click a row to show its details below it. Several rows can be open at once
//clicks anywhere outside the table closes all rows.
//products = one row per product sold during the year
type Props = {
    products: EnergyProductRow[];
    total: number;
};

function ProductEnergyTable({ products, total }: Props) {
    //The ids of the rows that are open. Empty list = all rows close
    const[openIds, setOpenIds] = useState<string[] > ([]);

    //opens a closed table row or closes an open row
    function toggleRow(productId: string) {
        if (openIds.includes(productId)) {
            setOpenIds(openIds.filter((id) => id !== productId));
        } else {
            setOpenIds([...openIds, productId]);
        }
    }

    // close all rows when the user clicks outside the table
    const tableRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        function handleClickOutside(event: MouseEvent) {
            if (tableRef.current !== null && !tableRef.current.contains(event.target as Node)) {
                setOpenIds([]);
            }
        }

        //listens for clicks on the page
        document.addEventListener("mousedown", handleClickOutside);
        //Stop listening when the table disappears from the page
        return () => {
            document.removeEventListener("mousedown", handleClickOutside);
        };
    }, []);

    return (
        <div ref={tableRef}>
          <h3 className="fs-5 mb-0">Sålda produkter</h3>
          <p className="small text-secondary mb-3">
            {products.length} produkter. Klicka på en rad för att se detaljer.
          </p>
    
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
                  <th className="text-end">Totalt kWh</th>
                  <th className="text-end">Åtgärder</th>
                </tr>
              </thead>
    
              <tbody>
                {products.map((product) => (
                  <ProductRow
                    key={product.productId}
                    product={product}
                    isOpen={openIds.includes(product.productId)}
                    onToggle={() => toggleRow(product.productId)}
                  />
                ))}
              </tbody>
    
              {/* The total row is in tfoot so it ends up last */}
              <tfoot>
                <tr className="fw-bold">
                  <td></td>
                  <td colSpan={3}>Totalt</td>
                  <td className="text-end">{formatNumber(total)}</td>
                  <td></td>
                </tr>
              </tfoot>
            </table>
          </div>
           </div>
    );
}

export default ProductEnergyTable;