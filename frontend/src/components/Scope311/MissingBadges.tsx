import type { MissingField } from "../../types/energyConsumption";

//Turns a missing field into swedish text
function missingLabel(field: MissingField): string {
    if (field === "kwh") {
        return "Saknar kWh";
    }
    if (field === "lifetime") {
        return "Saknar Livslängd";
    }
    return "Saknar grupp";
}

//shows what a product lacks as a small yellow label
//shows nothing when the product is complete
type Props = {
    missing: MissingField[];
};

function MissingBadges({ missing }: Props) {
    //if nothing is missing, show nothing
    if (missing.length === 0 ) {
        return null;
    }

    return (
        <div className="d-flex flex-wrap gap-1 mt-1">
            {missing.map((field) => (
                <span key={field} className="badge text-bg-warning">
                    {missingLabel(field)}
                </span>
            ))}
        </div>
    );
}

export default MissingBadges;