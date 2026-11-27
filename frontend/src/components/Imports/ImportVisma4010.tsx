import { useState, useRef } from "react";
import { ArrowLeft, Upload, FileSpreadsheet, Trash2 } from "lucide-react";
import type { ChangeEvent } from "react";
import type { Visma4010ImportOutcome, Visma4010ImportResponse } from "../../types/visma4010Import";
import { importVisma4010, previewVisma4010 } from "../../Services/visma4010ImportService";
import { formatNumber } from "../../utils/format";

const blockerTexts: Record<string, string> = {
  FileAlreadyImported: "Filen har redan importerats.",
  ReconciliationFailed: "Summorna i filen stämmer inte, se avvikelserna nedan.",
  DuplicateInvoiceNumber: "Samma fakturanummer (LevFktnr) finns på flera rader i filen.",
  NothingToImport: "Filen innehåller inga leverantörsfakturor som inte redan är importerade.",
};

const warningTexts: Record<string, string> = {
  PreliminaryReport: "Rapporten är preliminär. Belopp som ändras i Visma efteråt uppdateras inte av en ny import.",
  RowsWithoutSupplier: "Filen har rader utan leverantör. De sparas inte.",
};

const issueTexts: Record<string, string> = {
  BalanceMismatch: "Saldot stämmer inte med raderna ovanför",
  TurnoverMismatch: "Summan av debet och kredit stämmer inte med omslutningen",
  TurnoverMissing: "Raden Omslutning saknas",
  ClosingBalanceMismatch: "Utgående saldo stämmer inte med transaktionerna",
  ClosingBalanceMissing: "Raden Utgående saldo saknas",
  DuplicateInvoiceNumber: "Fakturanumret finns på en tidigare rad",
};

function formatSek(amount: number): string {
  return amount.toLocaleString("sv-SE", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + " kr";
}

type Props = {
  onBack: () => void;
};

function ImportVisma4010({ onBack }: Props) {
  const [file, setFile] = useState<File | null>(null);
  const [outcome, setOutcome] = useState<Visma4010ImportOutcome | null>(null);
  const [isImported, setIsImported] = useState(false);
  const [isWorking, setIsWorking] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    setFile(event.target.files?.[0] ?? null);
    setOutcome(null);
    setIsImported(false);
  }

// Clear the selected file and previous import results.
function handleRemoveFile() {
  setFile(null);
  setOutcome(null);
  setIsImported(false);

  if (fileInputRef.current) {
    fileInputRef.current.value = "";
  }
}

  async function handlePreview() {
    if (file === null) return;

    setIsWorking(true);
    setOutcome(await previewVisma4010(file));
    setIsImported(false);
    setIsWorking(false);
  }

  async function handleImport() {
    if (file === null) return;

    setIsWorking(true);
    const result = await importVisma4010(file);
    setOutcome(result);
    setIsImported(result.kind === "ok");
    setIsWorking(false);
  }

  const canImport = !isWorking && !isImported && outcome?.kind === "ok" && outcome.data.canImport;

  return (
    <div className="imports-page">
      <div className="mb-4">
        <div className="imports-header">
          <h1 className="fs-2 my-1">Import från Visma, konto 4010</h1>
          <button type="button" className="imports-back" onClick={onBack} disabled={isWorking}>
            <ArrowLeft size={18} />
            Tillbaka till startsidan
          </button>
      </div>
      <p>
        Välj en huvudbok exporterad från Visma som Excel-fil (.xlsx eller .xls, max 10 MB).
        Leverantörsfakturorna sparas som köpta varor i scope 3.1.
      </p>
    </div>

    {/* File upload */}
    <div className="imports-box mb-4">
      <h2 className="fs-4 mb-3">Ladda upp underlag</h2>
      
        <input ref={fileInputRef} type="file" accept=".xlsx,.xls" onChange={handleFileChange} disabled={isWorking} hidden/>
        {file === null ? (
          <button type="button" className="imports-upload" onClick={() => fileInputRef.current?.click()} disabled={isWorking}>
            <Upload size={32} />

            <span className="fw-semibold">Klicka för att välja en fil</span>

            <span className="small text-secondary">Excel (.xlsx eller .xls) · Max 10 MB</span>
          </button>
        ) : (

          <div className="imports-selected-file">
            <div className="imports-icon">
              <FileSpreadsheet size={24} />
            </div>

            <div className="imports-source">
              <p className="fw-semibold mb-1">{file.name}</p>
              <p className="small text-secondary mb-0">{(file.size / 1024).toFixed(1)} KB</p>
            </div>

            <button type="button" className="imports-outline-button" onClick={handleRemoveFile} disabled={isWorking}>
              <Trash2 size={16} />
              Ta bort
            </button>
          </div>
        )}

        {/* Import actions */}
        <div className="imports-actions">
          <button type="button" className="imports-outline-button" onClick={onBack} disabled={isWorking}>
            {isImported ? "Tillbaka" : "Avbryt"}
          </button>

          <div className="imports-action-buttons">
            <button type="button" className="imports-button" onClick={handlePreview} disabled={file === null || isWorking || isImported}>
              Förhandsgranska
            </button>

            <button type="button" className="imports-button" onClick={handleImport} disabled={!canImport}>
              Importera
            </button>
          </div>
        </div>
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
  result: Visma4010ImportResponse;
  isImported: boolean;
};

function Result({ result, isImported }: ResultProps) {
  return (
    <div>
      <h2 className="fs-4">{isImported ? "Resultat" : "Förhandsgranskning"}</h2>

      <table className="table table-sm w-auto">
        <tbody>
          <tr><th>Fil</th><td>{result.fileName}</td></tr>
          <tr><th>Företag</th><td>{result.company ?? "–"}</td></tr>
          <tr><th>Period</th><td>{result.periodFrom ?? "?"} – {result.periodTo ?? "?"}</td></tr>
          <tr><th>Konton</th><td>{result.accounts.map((account) => account.number + " " + account.name).join(", ")}</td></tr>
          <tr><th>Preliminär</th><td>{result.isPreliminary ? "Ja" : "Nej"}</td></tr>
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
          <tr><th></th><th className="text-end">Rader</th><th className="text-end">Netto</th></tr>
        </thead>
        <tbody>
          <SummaryRow label={isImported ? "Importerade" : "Importeras"} summary={result.toImport} />
          <SummaryRow label="Redan importerade (hoppas över)" summary={result.alreadyImported} />
          <SummaryRow label="Utan leverantör (sparas inte)" summary={result.withoutSupplier} />
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

      {result.newSuppliers.length > 0 && (
        <>
          <h3 className="fs-5">
            {isImported ? "Nya leverantörer som skapades" : "Nya leverantörer som skapas"} ({result.newSuppliers.length})
          </h3>
          <ul>
            {result.newSuppliers.map((name) => <li key={name}>{name}</li>)}
          </ul>
        </>
      )}

      {result.rowsWithoutSupplier.length > 0 && (
        <>
          <h3 className="fs-5">Rader utan leverantör ({result.rowsWithoutSupplier.length})</h3>
          <table className="table table-sm">
            <thead>
              <tr>
                <th>Rad</th><th>Konto</th><th>Vernr</th><th>Datum</th><th>Text</th><th className="text-end">Netto</th>
              </tr>
            </thead>
            <tbody>
              {result.rowsWithoutSupplier.map((row) => (
                <tr key={row.rowNumber}>
                  <td>{row.rowNumber}</td>
                  <td>{row.account}</td>
                  <td>{row.voucher}</td>
                  <td>{row.date}</td>
                  <td>{row.text}</td>
                  <td className="text-end">{formatSek(row.netSek)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}
    </div>
  );
}

type SummaryRowProps = {
  label: string;
  summary: { count: number; netSek: number };
};

function SummaryRow({ label, summary }: SummaryRowProps) {
  return (
    <tr>
      <th>{label}</th>
      <td className="text-end">{formatNumber(summary.count)}</td>
      <td className="text-end">{formatSek(summary.netSek)}</td>
    </tr>
  );
}

export default ImportVisma4010;
