import { useEffect, useState } from "react";
import type { EnergyConsumptionResponse } from "../types/energyConsumption";
//CSS for the page
import "../components/Scope311/EnergyConsumption.css"
//fetching from backend
import { getEnergyConsumption } from "../Services/energyConsumptionService";
//components for this page
import YearPicker from "../components/Scope311/YearPicker";
import ProductTable from "../components/Scope311/ProductTable";

// Scope 3.11: how much energy the products use during their lifetime.
// The page fetches the data and decides what to show. The table handles the rest.
function Scope311() {
  //the reporting year
  const [year, setYear] = useState(new Date().getFullYear());
  //The report from the backend
  const [report, setReport] = useState<EnergyConsumptionResponse | null>(null);
  //An error message if the fetch fails. not an error if null
  const [error, setError] = useState<string | null>(null);

  //runs when the page opens and again every time the year changes
  useEffect(() => {
    getEnergyConsumption(year).then((data) => {
      setReport(data);
      setError(null);
    })
    .catch(() => {
      setReport(null);
      setError("Kunde inte hämta data. Kontrollera att backend körs och att databasen är igång");
    });
  }, [year]);
  
  //Throws away the old data when a new year is selected
  // the page shows "Hämtar data .." until the new data comes
  function handleYearChange(newYear: number) {
    setYear(newYear);
    setReport(null);
    setError(null);
  }

  // Decide what to show under the heading
  let content;
  if (error !== null) {
    content = <div className="alert alert-danger">{error}</div>;
  } else if (report === null) {
    content = (
      <section className="energy-box">
        <p className="text-secondary mb-0">Hämtar data ..</p>
      </section>
    );
  } else if (report.products.length === 0) {
    content = (
      <section className="energy-box">
        <p className="text-secondary mb-0">Inga sålda produkter för {year}.</p>
      </section>
    );
  } else {
    content = (
      <section className="energy-box">
        <ProductTable products={report.products} total={report.totalActivityKwh} />
      </section>
    );
  }

  return (
    <div className="energy-page">
      <div className="d-flex justify-content-between align-items-start mb-4">
        <div>
          <p className="text-uppercase small text-secondary mb-1">Produktrapport</p>
          <h1 className="fs-2 my-1">Energiförbrukning per produkt</h1>
          <p>En översikt över sålda produkter och hur mycket energi de förbrukar under sin livslängd.</p>
        </div>
        <YearPicker year={year} onChange={handleYearChange} />
      </div>

      {content}
    </div>
  );
}

export default Scope311;