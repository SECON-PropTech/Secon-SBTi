import { useEffect, useState } from "react";
import { PackageCheck } from "lucide-react";
import type { MaterialLine } from "../../types/materialLine";
import { getProductPeriod } from "../../Services/soldProductsService";
import SalesLineRow from "./SalesLineRow";

type Props = {
    productId: string;
    year: number;
};

// The sales behind the quantity column, one line per date.
// Quantity/Antal in the table above is the sum of these lines.
function SalesLines({ productId, year }: Props) {
    const[lines, setLines] = useState<MaterialLine[] | null>(null);
    const[loadError, setLoadError] = useState<string | null>(null);

    //runs when the row is opened and again when the year changes
    useEffect(() => {
        getProductPeriod(productId, year).then((data) => {
            setLines(data);
            setLoadError(null);
        })
        .catch(() => {
            setLoadError("Kunde inte hämta försäljningen.");
        });
    }, [productId, year]);

    let content;
    if(loadError !== null) {
        content = <div className="alert alert-danger py-2">
            {loadError}
        </div>;
    } else if (lines === null) {
        content = <p className="text-secondary small mb-0">Hämtar försäljning ..</p>;
    }  else if (lines.length === 0) {
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
                    </tr>
                </thead>

                <tbody>
                    {lines.map((line) =>(
                        <SalesLineRow key={line.id} line={line}/>
                    ))}
                </tbody>
            </table>
        );
    }

    return (
        //stopPropagation so a click in the list doesn't close the row
        <div className="energy-sales" onClick={(event) => event.stopPropagation()}>
            <div className="energy-sales-header">
                <PackageCheck size={16}/>
                <h4 className="energy-sales-title">Försäljning {year}</h4>
                <span className="small border-start ps-2">Underlag för antal</span>
            </div>

            {content}

        </div>
    )
}

export default SalesLines;