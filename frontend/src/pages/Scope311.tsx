import { useEffect, useState } from "react";
import type { EnergyConsumptionResponse } from "../types/energyConsumption";
//CSS for the page
import "../components/Scope311/EnergyConsumption.css"
//fetching from backend
import { getEnergyConsumption } from "../Services/energyConsumptionService";
//components for this page
import YearPicker from "../components/Scope311/YearPicker";
import ProductEnergyTable from "../components/Scope311/ProductEnergyTable";

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
      setError("Kunde inte hämta data. Försök igen");
    });
  }, [year]);
  
  const isLoading = error === null && (report === null || report.year !== year);

  return (
    <div className="energy-page">
      {/*Heading on the left, year picker on the right */}
      <div className="d-flex justify-content-between align-items-start mb-4">
        <div>
          <p className="text-uppercase small text-secondary mb-1">ProduktRapport</p>
          <h1 className="fs-2 my-1">Energiförbrukning per produkt</h1>
          <p>En översikt över sålda produkter och hur mycket energi de förbrukar under sin livslängd.</p>
        </div>
        <YearPicker year={year} onChange={setYear}/>
      </div>

      {/*Show one of four things: error, loading, no products or the table*/}
      {error !== null ? (
      <div className="alert alert-danger">{error}</div> ) : isLoading || report === null ? (
        <section className="energy-box">
          <p className="text-secondary mb-0">Hämtar data ..</p>
        </section> ) : report.products.length === 0 ? (
          <section className="energy-box">
            <p className="text-secondary mb-0">Inga sålda produkter för {year}.</p>
          </section> ) : (
            <section className="energy-box">
              <ProductEnergyTable products={report.products} total={report.totalActivityKwh}/>
            </section>
          )}
    </div>
  )
}

export default Scope311;