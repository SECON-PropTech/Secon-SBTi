import {  useState  } from "react";
import type { EnergyProductRow } from "../../types/energyConsumption";
import { formatNumber } from "../../utils/format";
import ProductRow from "./ProductRow"
import Tooltip from "./Tooltip";
import { CircleCheck, Info, TriangleAlert } from "lucide-react";

//Sold products table
//Click a row to show its details below it
// The chips on top can show only the products that need more data.

type Props = {
    products: EnergyProductRow[];
    total: number;
};

function ProductTable({ products, total }: Props) {
    //The ids of the rows that are open. Empty list = all rows close
    const[openIds, setOpenIds] = useState<string[] > ([]);
    //if True only show the products that need more data
    const[onlyIncomplete, setOnlyIncomplete] = useState(false);

    // Products that lack something and products that are part of the total
    const incompleteCount = products.filter((product) => product.missing.length > 0).length;
    const countedCount = products.filter((product) => product.activityKwh !== null).length;

    // the filter only works when thre is something to show
    const showIncomplete = onlyIncomplete && incompleteCount > 0;
    const visibleProducts = showIncomplete ? products.filter((product) => product.missing.length > 0) : products;
    //True when every visible row is open, then the button closes them instead
    const allOpen = visibleProducts.every((product) => openIds.includes(product.productId));

    //opens a closed table row or closes an open row
    function toggleRow(productId: string) {
        if (openIds.includes(productId)) {
            setOpenIds(openIds.filter((openId) => openId !== productId));
        } else {
            setOpenIds([...openIds, productId]);
        }
    }

    //open every visible row or close them all if they're already open
    function toggleAll() {
      if (allOpen) {
        setOpenIds([]);
      } else {
        setOpenIds(visibleProducts.map((product) => product.productId));
      }
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
                onClick={() => setOnlyIncomplete(false)}
              >
                Alla <span className="energy-chip-count">{products.length}</span>
              </button>
  
              {/* Hidden when every product is complete */}
              {incompleteCount > 0 && (
                <button
                  type="button"
                  className={showIncomplete ? "energy-chip energy-chip-warning" : "energy-chip"}
                  onClick={() => setOnlyIncomplete(true)}
                >
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
                onToggle={() => toggleRow(product.productId)}/>
              ))}
              </tbody>
    
              {/* The total is for all products */}
              <tfoot>
                <tr className="fw-bold">
                  <td></td>
                  <td colSpan={3}>Totalt</td>
                  <td className="text-end">
                    <Tooltip text={"Beräknat på " + countedCount + " av " + products.length + " produkter"}>
                      <Info size={16} className="text-secondary"/>
                    </Tooltip>{" "}
                    {formatNumber(total)}
                  </td>
                  <td></td>
                </tr>
              </tfoot>
            </table>
          </div>
        </div>
    );
}

export default ProductTable;