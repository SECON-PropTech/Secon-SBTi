import { useEffect, useState } from "react";
import type { ImportBatch, ImportBatchesOutcome } from "../../types/importBatch";
import { getImportBatches, revertImportBatch } from "../../Services/importBatchService";
import { formatNumber } from "../../utils/format";

// The values of import_batch.source in the database.
const sourceLabels: Record<string, string> = {
  visma_4010: "Visma - köpta varor",
  visma_services: "Visma - köpta tjänster",
  mobigo: "Mobigo - sålda produkter",
  okq8: "OKQ8 - drivmedel",
  autoplan: "Autoplan - drivmedel",
};

type RevertMessage = {
  kind: "success" | "error";
  text: string;
};

function formatFiles(batch: ImportBatch): string {
  return batch.fileNames.length > 0 ? batch.fileNames.join(", ") : "–";
}

type Props = {
  onBack: () => void;
};

function ImportHistory({ onBack }: Props) {
  const [list, setList] = useState<ImportBatchesOutcome | null>(null);
  const [revertMessage, setRevertMessage] = useState<RevertMessage | null>(null);
  const [isWorking, setIsWorking] = useState(false);

  useEffect(() => {
    getImportBatches().then(setList);
  }, []);

  async function handleRevert(batch: ImportBatch) {
    const confirmed = window.confirm(
      `Ångra importen av ${formatFiles(batch)}?\n` +
      `${formatNumber(batch.rowCount)} rader från importen tas bort. `
    );

    if (!confirmed) return;

    setIsWorking(true);
    setRevertMessage(null);

    const result = await revertImportBatch(batch.id);
    setRevertMessage(result.kind === "ok"
      ? { kind: "success", text: `Importen av ${formatFiles(batch)} är ångrad.` }
      : { kind: "error", text: result.message });

    // Also after an error, since someone else may have reverted the import already.
    setList(await getImportBatches());
    setIsWorking(false);
  }

  return (
    <div className="p-4">
      <button type="button" className="btn btn-link p-0 mb-3" onClick={onBack} disabled={isWorking}>
        ← Tillbaka till importer
      </button>

      <h1 className="fs-2">Tidigare importer</h1>

      {revertMessage !== null && (
        <div className={revertMessage.kind === "success" ? "alert alert-success" : "alert alert-danger"}>
          {revertMessage.text}
        </div>
      )}

      {list === null && <p>Hämtar importer ..</p>}

      {list?.kind === "error" && <div className="alert alert-danger">{list.message}</div>}

      {list?.kind === "ok" && list.data.length === 0 && <p>Inga importer ännu.</p>}

      {list?.kind === "ok" && list.data.length > 0 && (
        <table className="table table-sm w-auto">
          <thead>
            <tr>
              <th>Importerad</th>
              <th>Källa</th>
              <th>Fil</th>
              <th>Importerad av</th>
              <th className="text-end">Rader</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {list.data.map((batch) => (
              <tr key={batch.id}>
                <td>{new Date(batch.importedAt).toLocaleString("sv-SE")}</td>
                <td>{sourceLabels[batch.source] ?? batch.source}</td>
                <td>{formatFiles(batch)}</td>
                <td>{batch.importedBy}</td>
                <td className="text-end">{formatNumber(batch.rowCount)}</td>
                <td>
                  <button
                    type="button"
                    className="btn btn-sm btn-outline-danger"
                    onClick={() => handleRevert(batch)}
                    disabled={isWorking || !batch.canRevert}
                    title={batch.canRevert ? undefined : "Ångra först den senare importen från Mobigo."}
                  >
                    Ångra
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

export default ImportHistory;
