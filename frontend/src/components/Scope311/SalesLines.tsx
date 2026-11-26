import { useEffect, useState } from "react";
import { PackageCheck } from "lucide-react";
import type { MaterialLine, MaterialLineDraft } from "../../types/materialLine";
import { getProductPeriod } from "../../Services/soldProductsService";
import SalesLineRow from "./SalesLineRow";
import { createMaterialLine, updateMaterialLine } from "../../Services/materialLinesService";
import { createLineDraft, createNewDraft, findLineErrors, isLineDraftChanged, lineDraftToRequest } from "../../utils/materialLineDraft";
import SalesLineForm from "./SalesLineForm";
import AddButton from "./AddButton";

type Props = {
    productId: string;
    year: number;
    onChanged: () => void;
};

// The sales behind the quantity column, one line per date.
// Quantity/Antal in the table above is the sum of these lines.
function SalesLines({ productId, year, onChanged }: Props) {
    const[lines, setLines] = useState<MaterialLine[] | null>(null);
    const[loadError, setLoadError] = useState<string | null>(null);

    //new while adding, the line id while editing, null when nothing is edited
    const [editingId, setEditingId] = useState<string | null>(null);
    const [draft, setDraft] = useState<MaterialLineDraft | null>(null);
    const [saveError, setSaveError] = useState<string | null>(null);

    //true while a save is on its way to backend
    const [isSaving, setIsSaving] = useState(false);
    const [reloadCount, setReloadCount] = useState(0);

    //runs when the row is opened and again when the year changes
    useEffect(() => {
        getProductPeriod(productId, year).then((data) => {
            setLines(data);
            setLoadError(null);
        })
        .catch(() => {
            setLoadError("Kunde inte hämta försäljningen.");
        });
    }, [productId, year, reloadCount]);

    //the line being changed. null while a new one is added
    const editingLine = lines?.find((line) => line.id === editingId) ?? null;
    const errors = draft ? findLineErrors(draft) : {};
    const canSave = !isSaving && draft !== null && Object.keys(errors).length === 0 &&
    (editingLine === null || isLineDraftChanged(draft, editingLine));

    //Starts a new empty line at the bottom of the list
    function startAdd() {
        setEditingId("new");
        setDraft(createNewDraft(year));
        setSaveError(null);
    }

    //Starts changing a line that already exists
    function startEdit(line: MaterialLine) {
        setEditingId(line.id);
        setDraft(createLineDraft(line));
        setSaveError(null);
    }

    function closeEdit() {
        setEditingId(null);
        setDraft(null);
        setSaveError(null);
    }

    function changeDraft(changes: Partial<MaterialLineDraft>) {
        if (draft !== null) {
            setDraft({ ...draft, ...changes });
        }
    }

    //saves a new line or the changes to an existing one
    function save() {
        if (draft === null || !canSave) {
            return;
        }
        const body = lineDraftToRequest(draft, productId);

        setIsSaving(true);

        const request = editingLine === null
            ? createMaterialLine(body)
            : updateMaterialLine(editingLine.id, body);

        request.then(() => {
            closeEdit();
            setReloadCount(reloadCount + 1);
            //the page fetches the report again, so Antal and the total come from the backend
            onChanged();
        })
        .catch((error: Error) => setSaveError(error.message))
        .finally(() => setIsSaving(false));
    }    

    let content;
    if(loadError !== null) {
        content = <div className="alert alert-danger py-2">
            {loadError}
        </div>;
    } else if (lines === null) {
        content = <p className="text-secondary small mb-0">Hämtar försäljning ..</p>;
    }  else if (lines.length === 0 && editingId !== "new") {
        content = <p className="text-secondary small mb-0">Ingen försäljning registrerad för {year}.</p>;     
    } else {
        content = (
            <table className="energy-sales-table">
                <thead>
                    <tr>
                        <th>Datum</th>
                        <th className="text-end">Antal</th>
                        <th>Datakvalitet</th>
                        <th>Kommentar</th>
                        <th></th>
                    </tr>
                </thead>

                <tbody>
                    {lines.map((line) =>(
                        editingId === line.id && draft !== null ? (
                        <SalesLineForm key={line.id} draft={draft} errors={errors}
                        canSave={canSave} onChange={changeDraft} onSave={save} onCancel={closeEdit}/> ) :
                        (
                        <SalesLineRow key={line.id} line={line} onEdit={() => startEdit(line)}/>)
                    ))}

                    {/*new line stays at the bottom until it is saved */}
                    {editingId === "new" && draft !== null && (
                        <SalesLineForm draft={draft} errors={errors} canSave={canSave}
                        onChange={changeDraft} onSave={save} onCancel={closeEdit}/>
                    )}
                </tbody>
            </table>
        );
    }

    return (
        //stopPropagation so a click in the list doesn't close the row
        <div className="energy-sales" onClick={(event) => event.stopPropagation()}
        onKeyDown={(event) => event.stopPropagation()}>

            <div className="energy-sales-header">
                <PackageCheck size={16}/>
                <h4 className="energy-sales-title">Försäljning {year}</h4>
                <span className="small border-start ps-2">Underlag för totalt antal</span>
            </div>

            {saveError !== null && <div className="alert alert-danger py-2">{saveError}</div>}

            {content}

            <div className="energy-sales-footer">
                <AddButton text="Lägg till försäljning" onClick={startAdd}/>
            </div>
        </div>
    )
}

export default SalesLines;