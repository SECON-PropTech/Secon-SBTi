import type { EnergyProductRow } from "../../types/energyConsumption";
import { formatNumber } from "../../utils/format";
import { Calculator, Hourglass, Zap } from "lucide-react";
import AddButton from "./AddButton";
import Tooltip from "./Tooltip";

type Props = {
    product: EnergyProductRow;

};
//The product details-part that shows when a row is opened
function ProductDetails({ product }: Props) {
    //A product that uses 0 kWh doesnt need a lifetime
    const lifetimeNotNeeded = product.kwhPerYear === 0 && product.lifetimeYears === null;
    
    // kWh per year: the value or a button when it's missing
    let kwhValue;
    if (product.kwhPerYear === null) {
        kwhValue = <AddButton text="Lägg till" warning/>;
    } else {
        kwhValue = formatNumber(product.kwhPerYear) + " kWh";
    }

    //lifetime: the value, "Ej aktuellt" for 0 kwh or a button when missing
    let lifetimeValue;
    if (product.lifetimeYears !== null) {
        lifetimeValue = formatNumber(product.lifetimeYears) + " år";
    } else if (lifetimeNotNeeded) {
        lifetimeValue = <span className="text-secondary">Ej aktuellt</span>;
    } else {
        lifetimeValue = <AddButton text ="Lägg till" warning/>;
    }

    return (
        <div>
            {/*dl = description list, dt = the label, dd = the value */}

            <dl className="energy-details-grid">
                <div>
                    <dt>
                        kWh per år{" "}
                        <Tooltip text="Används i beräkningen av totalen">
                            <Zap size={14}/>
                        </Tooltip>
                    </dt>
                    <dd>{kwhValue}</dd>
                </div>
                <div>
                    <dt>
                        Livslängd{" "}
                        <Tooltip text="Används i beräkningen av totalen">
                            <Hourglass size={14}/>
                        </Tooltip>
                    </dt>
                    <dd>{lifetimeValue}</dd>
                </div>
                <div>
                    <dt>Mätkvalitet</dt>
                    <dd>
                        {product.measurementQuality === "measured" ? (
                            <span className="energy-pill energy-pill-green">Uppmätt</span>) : (
                                <span className="energy-pill">Uppskattad</span>
                            )
                        }
                    </dd>
                </div>
                <div>
                    <dt>Kommentar</dt>
                    <dd>{product.comment ?? "Tomt"}</dd>
                </div>
            </dl>

            {/*The calculation. A missing value is shown as ? in orange */}
            <div className="energy-calculation">
                <Calculator size={16}/>
                <span>{formatNumber(product.quantity)} st</span>
                <span> × </span>
                {product.kwhPerYear === null ? (
                    <span className="energy-missing-text">? kWh</span>) : (
                      <span>{formatNumber(product.kwhPerYear)} kWh</span>  
                    )}

                    {/* Skip lifetime when it isnt need (0 kwh)*/}
                    {!lifetimeNotNeeded && (
                        <>
                        <span> × </span>
                        {product.lifetimeYears === null ? (
                            <span className="energy-missing-text">? år</span>) : (
                                <span>{formatNumber(product.lifetimeYears)} år</span>
                            )}
                        </>
                    )}
                    <span> = </span>
                    {product.activityKwh === null ? (
                        <span className="energy-missing-text"> Räknas inte</span>) : (
                            <strong>{formatNumber(product.activityKwh)} kWh</strong>
                    )}
            </div>
        </div>
    );
}
export default ProductDetails;