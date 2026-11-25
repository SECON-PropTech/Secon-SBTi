import { CircleCheckBig, EqualApproximately } from "lucide-react";
import type { MaterialLine } from "../../types/materialLine";
import { formatDate, formatNumber } from "../../utils/format";

type Props = {
    line: MaterialLine;
};

//One sales line in the list under a product
function SalesLineRow({ line }: Props) {
    const isMeasured = line.measurementQuality === "measured";

    return (
        <tr>
            <td>{formatDate(line.date)}</td>
            <td className="text-end fw-semibold">{formatNumber(line.quantity)}</td>

            {/* Quality of the quantity. Plain text*/}
            <td>
                <span className="energy-sales-quality">
                    {isMeasured ? <CircleCheckBig size={14}/> : <EqualApproximately size={14}/>}
                    {isMeasured ? "Uppmätt" : "Uppskattad"}
                </span>
            </td>

            <td className={line.comment === null ? "text-secondary" : ""}>
                {line.comment ?? <span className="energy-no-comment">Ingen kommentar</span>}
            </td>
        </tr>
    );
}

export default SalesLineRow;