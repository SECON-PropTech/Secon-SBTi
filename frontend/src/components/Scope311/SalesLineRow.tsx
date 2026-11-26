import { CircleCheckBig, EqualApproximately, Pencil } from "lucide-react";
import type { MaterialLine } from "../../types/materialLine";
import { formatDate, formatNumber } from "../../utils/format";

type Props = {
    line: MaterialLine;
    onEdit: () => void;
};

//One sales line in the list under a product
function SalesLineRow({ line, onEdit }: Props) {
    const isMeasured = line.measurementQuality === "measured";

    return (
        <tr>
            <td>{formatDate(line.date)}</td>
            <td className="text-end fw-semibold">{formatNumber(line.quantity)}</td>

            {/* Quality of the quantity. Plain text*/}
            <td>
                <span className="energy-sales-quality">
                    {isMeasured ? <CircleCheckBig size={14}/> : <EqualApproximately size={14}/>}
                    {isMeasured ? "Verifierad" : "Uppskattad"}
                </span>
            </td>

            <td>
                {line.comment ?? <span className="energy-no-comment">Ingen kommentar</span>}
            </td>

            <td className="text-end">
                <button type="button" aria-label="Redigera försäljning" className="btn btn-sm energy-edit-button"
                onClick={onEdit}>
                    <Pencil size={16}/>
                </button>
            </td>
        </tr>
    );
}

export default SalesLineRow;