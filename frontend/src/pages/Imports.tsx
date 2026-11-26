import { useState } from "react";
import { CalendarDays, FileSpreadsheet, Upload, History, ChevronRight } from "lucide-react";
import ImportHistory from "../components/Imports/ImportHistory";
import ImportVisma4010 from "../components/Imports/ImportVisma4010";

import "../components/Imports/Import.css"

type ImportView = "menu" | "visma-4010" | "history";

// Available and upcoming import sources.
const importSources = [
    {
        id: "visma-4010",
        name: "Visma",
        description: "Köpta varor",
        available: true,
    },
    {
        id: "visma-services",
        name: "Visma",
        description: "Köpta tjänster",
        available: false,
    },
    {
        id: "mobigo",
        name: "Mobigo",
        description: "Sålda produkter",
        available: false,
    },
    {
        id: "okq8",
        name: "OKQ8",
        description: "Drivmedel",
        available: false,
    },
    {
        id: "autoplan",
        name: "Autoplan",
        description: "Drivmedel",
        available: false,
    },
];

function Imports() {
  const [view, setView] = useState<ImportView>("menu");

  const currentMonth = new Date().toLocaleDateString("sv-SE", {
      month: "long",
      year: "numeric",
  });

  if (view === "visma-4010") {
    return <ImportVisma4010 onBack={() => setView("menu")} />;
  }

  if (view === "history") {
    return <ImportHistory onBack={() => setView("menu")} />;
  }

  return (
    <div className="imports-page">
        {/* Page heading */}
        <div className="mb-4">
            <p className="text-uppercase small text-secondary mb-1">SBTi-rapportering</p>
            <h1 className="fs-2 my-1">Månadsöversikt</h1>
            <p>Hantera månadens importer och följ rapporteringens status.</p>
        </div>

        {/* Current reporting month */}
        <section className="imports-box mb-4">
            <div className="imports-month">
                <div className="imports-icon">
                    <CalendarDays size={24} />
                </div>

                <div>
                    <p className="small text-secondary mb-1">Rapporteringsmånad</p>
                    <h2 className="fs-4 mb-0 imports-month-title">{currentMonth}</h2>
                </div>
            </div>
        </section>

        {/* Import sources */}
        <section className="imports-box mb-4">
            <h2 className="fs-4 mb-3">Importera underlag</h2>

            {importSources.map((source) => (
                <div className="imports-row" key={source.id}>
                    <div className="imports-icon">
                        <FileSpreadsheet size={22} />
                    </div>

                    <div className="imports-source">
                        <h3 className="fs-6 mb-1">{source.name}</h3>
                        <p className="text-secondary small mb-0">{source.description}</p>

                        {!source.available && (
                            <p className="small text-secondary mb-0">Kommer snart</p>
                        )}
                    </div>

                    <button
                        type="button"
                        className="imports-button"
                        disabled={!source.available}
                        onClick={() => {
                            if (source.id === "visma-4010") {
                                setView("visma-4010");
                            }
                        }}
                    >
                        <Upload size={16} />
                        Importera
                    </button>
                </div>
            ))}
        </section>

        {/* Import history */}
        <button
            type="button"
            className="imports-box imports-history"
            onClick={() => setView("history")}
        >
            <div className="imports-icon"><History size={24} /></div>

            <div className="imports-source">
                <span className="fw-semibold">Tidigare importer</span>
                <p className="small text-secondary mb-0">Visa och hantera importhistorik.</p>
            </div>

            <ChevronRight size={20} />
        </button>
    </div>
  );
}

export default Imports;
