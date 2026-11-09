import {
  getProductGroups, type ProductGroup,
} from "../Services/soldProductsService";
import { useEffect, useState } from "react";
import ImportedDataTable from "../components/SoldProductsManagement/ImportedDataTable";
import type {
  Product,
  ProductInclusion,
  SoldProductsManagementView,
} from "../types/soldProductsManagement";

const months = [
  { value: "01", label: "Januari" },
  { value: "02", label: "Februari" },
  { value: "03", label: "Mars" },
  { value: "04", label: "April" },
  { value: "05", label: "Maj" },
  { value: "06", label: "Juni" },
  { value: "07", label: "Juli" },
  { value: "08", label: "Augusti" },
  { value: "09", label: "September" },
  { value: "10", label: "Oktober" },
  { value: "11", label: "November" },
  { value: "12", label: "December" },
];
// Tillfällig tom lista tills vi kopplar in API:t.
const importedProducts: Product[] = [];

function createInclusion(products: Product[],
): ProductInclusion {
  return Object.fromEntries(
    products.map((product) => {
      const status = product["billing-status"]?.trim().toLowerCase();

      const isNonBillable = [
        "ej faktureringsbar",
        "ej faktureringsbara",
      ].includes(status ?? "");

      return [product.id, isNonBillable ? null : true];
    }),
  );
}

function SoldProductsManagement() {
  const [products, setProducts] = useState<Product[]>(importedProducts);
  const [productGroups, setProductGroups] = useState<ProductGroup[]>([]);
  console.log("Test av produktgrupp från backend", productGroups); //test

  const today = new Date();
  const currentYear = String(today.getFullYear());
  const currentMonth = String(today.getMonth() +1).padStart(2, "0");

  const [selectedMonth, setSelectedMonth] = useState(currentMonth);
  const [selectedView, setSelectedView] = useState<SoldProductsManagementView>("products");

  const [productInclusion, setProductInclusion] = useState<ProductInclusion>(() => createInclusion(importedProducts));

  useEffect(() => {
    async function loadProductGroups() {
      try {
        const groups = await getProductGroups();
        setProductGroups(groups);
      } catch (error) {
        console.error("Kunde inte hämta produktgrupper:", error);
      }
    }

    loadProductGroups();
  }, []);

  function toggleProductInclusion(productId: string) {
    setProductInclusion((previous) => ({
      ...previous, [productId]: !previous[productId],
    }));
  }

  function editProductRow(
    productId: string,
    updatedProduct: Product) {
    setProducts((previousProducts) =>
      previousProducts.map((product) =>
        product.id === productId
          ? { ...updatedProduct, id: productId }
          : product,
      ),
    );
  }

  const filteredProducts = products.filter((product) => {
    if (!product.date) {
      return false;
    }

    const [year, month] = product.date.split("-");

    return (
      year === currentYear && (selectedMonth ==="all" || month === selectedMonth)
    );
  });

  return (
    <div>
      <h2>Slutbehandling av sålda produkter</h2>
      {selectedView === "products" ? (
        <>
          <div className="mb-3">
            <label htmlFor="month-filter" className="form-label">
              Visa period
            </label>

            <select
              id="month-filter"
              className="form-select"
              value={selectedMonth}
              onChange={(event) => setSelectedMonth(event.target.value)}
            >
              <option value="all">Hela året</option>

              {months.map((month) => (
                <option key={month.value} value={month.value}>
                  {month.label}
                </option>
              ))}
            </select>
          </div>

          {filteredProducts.length > 0 ? (
            <ImportedDataTable
              products={filteredProducts}
              productInclusion={productInclusion}
              onToggleInclusion={toggleProductInclusion}
              onEditProduct={editProductRow}
            /> 
          ) : (
              <p>Inga produkter hittades för den valda perioden</p>
            )} </>
          ) : (
            <>
              <div>
                <p>Vyn "{selectedView}" är inte implementerad ännu.</p>
              </div>

              <button
                type="button"
                className="btn btn-secondary mb-3"
                onClick={() => setSelectedView("products")}
              >
                Tillbaka till produkter
              </button>
            </>
          )}

      <div className="d-flex flex-wrap gap-2 mb-3">
        <button 
          type="button" 
          className="btn btn-info" 
          onClick= { () => setSelectedView("materials")}
          >Visa materialkategorier
        </button>

        <button
          type="button"
          className="btn btn-info"
          onClick={() => setSelectedView("topTen")}
          >Visa top 10 största avfall
        </button>

        <button
          type="button"
          className="btn btn-info"
          onClick={() => setSelectedView("projects")}
          >Projekt
        </button> 
      </div>       
    </div>
  );

}

export default SoldProductsManagement;