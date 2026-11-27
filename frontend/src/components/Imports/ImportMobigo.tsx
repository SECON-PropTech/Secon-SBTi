import { useState } from "react";
import type { ChangeEvent } from "react";
import type { MobigoImportOutcome, MobigoImportResponse, MobigoLinesSummary } from "../../types/mobigoImport";
import { importMobigo, previewMobigo } from "../../Services/mobigoImportService";
import { formatNumber } from "../../utils/format";

const blockerTexts: Record<string, string> = {
  FileAlreadyImported: "Filen har redan importerats.",
  ReconciliationFailed: "Summorna i filen stämmer inte, se avvikelserna nedan.",
  InvalidRows: "Några rader kan inte sparas, se avvikelserna nedan.",
  NothingToImport: "Filen innehåller inga rader som inte redan är importerade.",
};

const warningTexts: Record<string, string> = {
  RowsSkipped: "Filen har rader som inte sparas, t.ex. arbete, hyra och resor utan artikelnummer. Se listan längst ned.",
  ChangedSinceImport: "Några rader har importerats tidigare med ett annat antal. De ändras inte, se listan nedan.",
};

const issueTexts: Record<string, string> = {
  TotalMissing: "Raden SUMMA: saknas",
  TotalMismatch: "Summan av kolumnen Summa stämmer inte med raden SUMMA:",
  AmountMismatch: "Antal × Á-pris med rabatt stämmer inte med Summa",
  WorkOrderMissing: "Uppdrag saknas",
  DeliveryDateMissing: "Leveransdatum saknas",
  QuantityMissing: "Antal saknas",
  QuantityNegative: "Antal är negativt",
};

const skipReasonTexts: Record<string, string> = {
  WithoutArticleNumber: "Inget artikelnummer",
  NotFinished: "Uppdraget är inte klart",
  OtherUnit: "Annan enhet än st",
};

function formatSek(amount: number): string {
  return amount.toLocaleString("sv-SE", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + " kr";
}

type Props = {
  onBack: () => void;
};

function ImportMobigo({ onBack }: Props) {
  const [file, setFile] = useState<File | null>(null);
  const [outcome, setOutcome] = useState<MobigoImportOutcome | null>(null);
  const [isImported, setIsImported] = useState(false);
  const [isWorking, setIsWorking] = useState(false);

  function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    setFile(event.target.files?.[0] ?? null);
    setOutcome(null);
    setIsImported(false);
  }

  async function handlePreview() {
    if (file === null) return;

    setIsWorking(true);
    setOutcome(await previewMobigo(file));
    setIsImported(false);
    setIsWorking(false);
  }

  async function handleImport() {
    if (file === null) return;

    setIsWorking(true);
    const result = await importMobigo(file);
    setOutcome(result);
    setIsImported(result.kind === "ok");
    setIsWorking(false);
  }

  const canImport = !isWorking && !isImported && outcome?.kind === "ok" && outcome.data.canImport;

  return (
    <div className="p-4">
      <button type="button" className="btn btn-link p-0 mb-3" onClick={onBack} disabled={isWorking}>
        ← Tillbaka till importer
      </button>

      <h1 className="fs-2">Import från Mobigo</h1>
      <p>
        Välj en lista med uppdragsrader exporterad från Mobigo som Excel-fil (.xlsx eller .xls, max 10 MB).
        Rader med artikelnummer från klara uppdrag sparas som sålda produkter i scope 3.11 och 3.12.
        Rader som redan är importerade från en annan fil hoppas över.
      </p>

      <input type="file" accept=".xlsx,.xls" onChange={handleFileChange} disabled={isWorking} />

      <div className="my-3">
        <button type="button" className="btn btn-secondary me-2" onClick={handlePreview} disabled={file === null || isWorking}>
          Förhandsgranska
        </button>
        <button type="button" className="btn btn-primary" onClick={handleImport} disabled={!canImport}>
          Importera
        </button>
      </div>

      {isWorking && <p>Läser filen ..</p>}

      {outcome?.kind === "error" && <div className="alert alert-danger">{outcome.message}</div>}

      {outcome?.kind === "blocked" && (
        <div className="alert alert-danger">Importen stoppades. Inget har sparats.</div>
      )}

      {isImported && <div className="alert alert-success">Importen är sparad.</div>}

      {outcome !== null && outcome.kind !== "error" && <Result result={outcome.data} isImported={isImported} />}
    </div>
  );
}

type ResultProps = {
  result: MobigoImportResponse;
  isImported: boolean;
};

function Result({ result, isImported }: ResultProps) {
  return (
    <div>
      <h2 className="fs-4">{isImported ? "Resultat" : "Förhandsgranskning"}</h2>

      <table className="table table-sm w-auto">
        <tbody>
          <tr><th>Fil</th><td>{result.fileName}</td></tr>
          <tr><th>Leveransdatum</th><td>{result.deliveryDateFrom ?? "–"} – {result.deliveryDateTo ?? "–"}</td></tr>
          <tr><th>Summa i filen</th><td>{formatSek(result.total)}</td></tr>
          <tr><th>Raden SUMMA:</th><td>{result.fileTotal === null ? "saknas" : formatSek(result.fileTotal)}</td></tr>
          {result.importBatchId && <tr><th>Import-id</th><td>{result.importBatchId}</td></tr>}
        </tbody>
      </table>

      {result.blockers.map((code) => (
        <div key={code} className="alert alert-danger">
          {blockerTexts[code] ?? code}
          {code === "FileAlreadyImported" && result.previousImport && (
            <> Importerad {new Date(result.previousImport.importedAt).toLocaleString("sv-SE")} av {result.previousImport.importedBy}.</>
          )}
        </div>
      ))}

      {result.warnings.map((code) => (
        <div key={code} className="alert alert-warning">{warningTexts[code] ?? code}</div>
      ))}

      <table className="table table-sm w-auto">
        <thead>
          <tr><th></th><th className="text-end">Rader i filen</th><th className="text-end">Rader som sparas</th><th className="text-end">Antal</th></tr>
        </thead>
        <tbody>
          <SummaryRow label={isImported ? "Importerade" : "Importeras"} summary={result.toImport} />
          <SummaryRow label="Redan importerade (hoppas över)" summary={result.alreadyImported} />
          <tr>
            <th>Sparas inte</th>
            <td className="text-end">{formatNumber(result.skippedRows.length)}</td>
            <td></td>
            <td></td>
          </tr>
        </tbody>
      </table>

      {result.issues.length > 0 && (
        <>
          <h3 className="fs-5">Avvikelser i filen</h3>
          <ul>
            {result.issues.map((issue) => (
              <li key={issue.rowNumber + issue.code}>
                Rad {issue.rowNumber}: {issueTexts[issue.code] ?? issue.code}
              </li>
            ))}
          </ul>
        </>
      )}

      {result.changedLines.length > 0 && (
        <>
          <h3 className="fs-5">Ändrade sedan tidigare import, ändras inte ({result.changedLines.length})</h3>
          <table className="table table-sm">
            <thead>
              <tr>
                <th>Rad</th><th>Uppdrag</th><th>Artikelnr</th><th>Produkt</th><th>Leveransdatum</th>
                <th className="text-end">Antal i filen</th><th className="text-end">Antal sparat</th>
              </tr>
            </thead>
            <tbody>
              {result.changedLines.map((line) => (
                <tr key={line.rowNumbers.join("+")}>
                  <td>{line.rowNumbers.join(", ")}</td>
                  <td>{line.workOrder}</td>
                  <td>{line.articleNumber}</td>
                  <td>{line.productName}</td>
                  <td>{line.deliveryDate}</td>
                  <td className="text-end">{formatNumber(line.quantity)}</td>
                  <td className="text-end">{formatNumber(line.importedQuantity)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}

      {result.newProducts.length > 0 && (
        <>
          <h3 className="fs-5">
            {isImported ? "Nya produkter som skapades" : "Nya produkter som skapas"} ({result.newProducts.length})
          </h3>
          <ul>
            {result.newProducts.map((product) => (
              <li key={product.articleNumber}>{product.articleNumber} {product.name}</li>
            ))}
          </ul>
        </>
      )}

      {result.linesToImport.length > 0 && (
        <>
          <h3 className="fs-5">{isImported ? "Sparade rader" : "Rader som sparas"} ({result.linesToImport.length})</h3>
          <table className="table table-sm">
            <thead>
              <tr>
                <th>Rad</th><th>Uppdrag</th><th>Artikelnr</th><th>Produkt</th><th>Leveransdatum</th>
                <th className="text-end">Antal</th><th>Ny produkt</th>
              </tr>
            </thead>
            <tbody>
              {result.linesToImport.map((line) => (
                <tr key={line.rowNumbers.join("+")}>
                  <td>{line.rowNumbers.join(", ")}</td>
                  <td>{line.workOrder}</td>
                  <td>{line.articleNumber}</td>
                  <td>{line.productName}</td>
                  <td>{line.deliveryDate}</td>
                  <td className="text-end">{formatNumber(line.quantity)}</td>
                  <td>{line.isNewProduct ? "Ja" : ""}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}

      {result.skippedRows.length > 0 && (
        <details>
          <summary className="fs-5">Rader som inte sparas ({result.skippedRows.length})</summary>
          <table className="table table-sm">
            <thead>
              <tr>
                <th>Rad</th><th>Orsak</th><th>Uppdrag</th><th>Status</th><th>Artikelnr</th><th>Namn</th>
                <th>Leveransdatum</th><th className="text-end">Antal</th><th>Enhet</th>
              </tr>
            </thead>
            <tbody>
              {result.skippedRows.map((row) => (
                <tr key={row.rowNumber}>
                  <td>{row.rowNumber}</td>
                  <td>{skipReasonTexts[row.reason] ?? row.reason}</td>
                  <td>{row.workOrder}</td>
                  <td>{row.status ?? ""}</td>
                  <td>{row.articleNumber ?? ""}</td>
                  <td>{row.name ?? ""}</td>
                  <td>{row.deliveryDate ?? ""}</td>
                  <td className="text-end">{row.quantity === null ? "" : formatNumber(row.quantity)}</td>
                  <td>{row.unit ?? ""}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </details>
      )}
    </div>
  );
}

type SummaryRowProps = {
  label: string;
  summary: MobigoLinesSummary;
};

function SummaryRow({ label, summary }: SummaryRowProps) {
  return (
    <tr>
      <th>{label}</th>
      <td className="text-end">{formatNumber(summary.rows)}</td>
      <td className="text-end">{formatNumber(summary.lines)}</td>
      <td className="text-end">{formatNumber(summary.quantity)}</td>
    </tr>
  );
}

export default ImportMobigo;
