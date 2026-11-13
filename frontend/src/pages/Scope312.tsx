import { useEffect, useState } from "react";

import { getSoldProducts, getAllProductMaterials } from "../Services/soldProductsService";

import type { SoldProducts, ProductMaterials } from "../types/soldProducts";

import Table from "../components/Scope312/Table";
import YearPicker from "../components/Scope311/YearPicker";
import "../components/Scope312/Scope312.css";

const months = [
    { value: "all", label: "Hela året" },
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

function Scope312HomePage() {
  const [year, setYear] = useState(new Date().getFullYear());

  const [selectedMonth, setSelectedMonth] = useState(
      String(new Date().getMonth() + 1).padStart(2, "0")
  );

  const [products, setProducts] = useState<SoldProducts[]>([]);

  const [productMaterials, setProductMaterials] = useState<ProductMaterials[]>([]);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
    
  useEffect(() => {
      let active = true;

      async function loadData() {
          try {
              setLoading(true);
              setError(null);

              const [fetchedProducts, fetchedMaterials] = await Promise.all([
                      getSoldProducts(),
                      getAllProductMaterials(),
              ]);

              if (!active) return;

              setProducts(fetchedProducts);
              setProductMaterials(fetchedMaterials);
            } catch {
                if (active) {
                    setError("Kunde inte hämta produktdata. Kontrollera backend och databasen.");
                }
            } finally {
                if (active) setLoading(false);
            }
        }

      loadData();

      return () => { active = false;
      };
  }, []);

  function handleYearChange(newYear: number) {
      setYear(newYear);
  }

    let content;

    if (loading) {
        content = <section className="energy-box">Hämtar data...</section>;
    } else if (error) {
        content = <div className="alert alert-danger">{error}</div>;
    } else if (products.length === 0) {
        content = <section className="energy-box">Inga produkter hittades.</section>;
    } else {
        content = (
            <section className="energy-box">
                <Table
                    products={products}
                    productMaterials={productMaterials}
                    year={year}
                    selectedMonth={selectedMonth}
                />
            </section>
        );
    }

  return (
      <div className="energy-page">
          <div className="d-flex justify-content-between align-items-start mb-4">
              <div>
                  <p className="text-uppercase small text-secondary mb-1">Produktrapport</p>
                  <h1 className="fs-2 my-1">Slutbehandling av sålda produkter</h1>
                  <p>En översikt över sålda produkter, deras material och hantering vid livslängdens slut.</p>
              </div>

              <div>
                  <label htmlFor="year-filter" className="form-label">År</label>
                  <YearPicker year={year} onChange={handleYearChange} />
              </div>
          </div>

          <div className="mb-3">
              <label htmlFor="month-filter" className="form-label">Visa period</label>

              <select id="month-filter" className="form-select" value={selectedMonth} onChange={(event) =>
                      setSelectedMonth(event.target.value)
                  }
              >
                  {months.map((month) => (
                      <option key={month.value} value={month.value}>
                          {month.label}
                      </option>
                  ))}
              </select>
          </div>

          {content}
      </div>
  );
}

export default Scope312HomePage;    