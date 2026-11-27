import { useState } from "react";
import ImportHistory from "../components/Imports/ImportHistory";
import ImportMobigo from "../components/Imports/ImportMobigo";
import ImportVisma4010 from "../components/Imports/ImportVisma4010";

type ImportView = "menu" | "visma-4010" | "mobigo" | "history";

const comingImports = [
  "Visma - köpta tjänster",
  "OKQ8 - drivmedel",
  "Autoplan - drivmedel",
];

function Imports() {
  const [view, setView] = useState<ImportView>("menu");

  if (view === "visma-4010") {
    return <ImportVisma4010 onBack={() => setView("menu")} />;
  }

  if (view === "mobigo") {
    return <ImportMobigo onBack={() => setView("menu")} />;
  }

  if (view === "history") {
    return <ImportHistory onBack={() => setView("menu")} />;
  }

  return (
    <div className="p-4">
      <h1 className="fs-2">Importering</h1>

      <h2 className="fs-4 mt-4">Importera en fil</h2>
      <div className="d-flex flex-column align-items-start gap-2">
        <button type="button" className="btn btn-primary" onClick={() => setView("visma-4010")}>
          Visma - köpta varor
        </button>

        <button type="button" className="btn btn-primary" onClick={() => setView("mobigo")}>
          Mobigo - sålda produkter
        </button>

        {comingImports.map((label) => (
          <button key={label} type="button" className="btn btn-outline-secondary" disabled>
            {label}
          </button>
        ))}
      </div>

      <h2 className="fs-4 mt-4">Tidigare importer</h2>
      <div className="d-flex flex-column align-items-start gap-2">
        <button type="button" className="btn btn-primary" onClick={() => setView("history")}>
          Visa tidigare importer
        </button>
      </div>
    </div>
  );
}

export default Imports;
