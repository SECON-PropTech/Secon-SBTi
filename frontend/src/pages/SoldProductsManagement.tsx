import {
    //getProductGroups,
    getAllProductMaterials,
    getProductPeriod,
    //type ProductGroup,
    type ProductMaterials,
    type ProductPeriodEntry,
} from "../Services/soldProductsService";

import { useEffect, useState } from "react";

import ImportedDataTable from "../components/SoldProductsManagement/ImportedDataTable";

import type {
    Product,
    ProductInclusion,
    SoldProductsManagementView,
} from "../types/soldProductsManagement";

import "../../src/components/SoldProductsManagement/SoldProductManagement.css";

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

function mapProductsToFrontend(
    products: ProductMaterials[],
    entries: ProductPeriodEntry[]
): Product[] {

    return products.map((product) => {
        // Hitta alla registreringar som tillhör produkten
        const productEntries = entries.filter(
            (entry) => entry.productId === product.productId
        );

        // Summera antal för produktens registreringar
        const totalQuantity = productEntries.reduce(
            (sum, entry) => sum + entry.quantity,
            0
        );

        // Bestäm mätkvalitet för den summerade produkten
        const measurementQuality =
            productEntries.length > 0 &&
            productEntries.every(
                (entry) => entry.measurementQuality === "measured"
            )
                ? "measured"
                : "estimated";

        // Senaste registreringsdatum
        const latestDate = productEntries.length > 0
            ? productEntries.reduce((latest, entry) =>
                entry.date > latest ? entry.date : latest,
                productEntries[0].date
            )
            : "";

        return {
            id: product.productId,
            name: product.productName,
            articleNo: product.articleNo ?? undefined,
            quantity: totalQuantity,
            date: latestDate,
            materials: product.materials.map((material) => ({
                id: material.materialId,
                name: material.materialName,
                kg: material.weightKg,
            })),
            measurementQuality,
        };
    });
}


function SoldProductsManagement() {
  const [backendProducts, setBackendProducts] =
    useState<ProductMaterials[]>([]);

  const [periodEntries, setPeriodEntries] =
    useState<ProductPeriodEntry[]>([]);

  const today = new Date();
  const currentYear = String(today.getFullYear());
  const currentMonth = String(today.getMonth() +1).padStart(2, "0");

  const [selectedMonth, setSelectedMonth] = useState(currentMonth);
  const [selectedView, setSelectedView] = useState<SoldProductsManagementView>("products");

  const [productInclusion, setProductInclusion] = useState<ProductInclusion>({});


useEffect(() => {
    async function loadData() {
        try {
            // Hämta produkter och deras material
            const fetchedProducts = await getAllProductMaterials();

            // Hämta periodregistreringar för alla produkter
            const entriesPerProduct = await Promise.all(
                fetchedProducts.map((product) =>
                    getProductPeriod(
                        product.productId,
                        Number(currentYear)
                    )
                )
            );

            // Slå ihop alla registreringar
            const allEntries = entriesPerProduct.flat();

            // Spara backendinformationen separat
            setBackendProducts(fetchedProducts);
            const mappedProducts = mapProductsToFrontend(fetchedProducts, allEntries);
            setProductInclusion(createInclusion(mappedProducts));
            setPeriodEntries(allEntries);


        } catch (error) {
            console.error("Kunde inte hämta produktdata:", error);
        }
    }

    loadData();
}, []);




  function toggleProductInclusion(productId: string) {
    setProductInclusion((previous) => ({
      ...previous, [productId]: !previous[productId],
    }));
  }

  //function editProductRow(
    //productId: string,
    //updatedProduct: Product) {
    //setProducts((previousProducts) =>
      //previousProducts.map((product) =>
        //product.id === productId
          //? { ...updatedProduct, id: productId }
          //: product,
      //),
    //);
  //}

const entriesForSelectedPeriod = periodEntries.filter((entry) => {
    if (selectedMonth === "all") {
        return true;
    }

    const month = entry.date.split("-")[1];

    return month === selectedMonth;
});

const filteredProducts = mapProductsToFrontend(
    backendProducts,
    entriesForSelectedPeriod
).filter((product) => product.quantity > 0);

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
              //onEditProduct={editProductRow}
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