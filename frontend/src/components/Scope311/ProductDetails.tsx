import type { EnergyProductRow } from "../../types/energyConsumption";
import { formatNumber, formatOptional } from "../../utils/format";
import MissingBadges from "./MissingBadges";

type Props = {
    product: EnergyProductRow;

};

function ProductDetails({ product }: Props) {
    let lifetimeText = "-";
    if (product.lifetimeYears !== null) {
        lifetimeText = formatNumber(product.lifetimeYears) + " år";
    }

    let totalText = "-";
    if (product.activityKwh !== null) {
        totalText = formatNumber(product.activityKwh) + " kWh";
    }

    return (
        <div>
            <h4 className="fs-6 fw-semibold mb-3">Produktdetaljer</h4>
            {/*dl = description list, dt = the label, dd = the value */}

            <dl className="energy-details-grid">
                <div>
                    <dt>Artikelnummer</dt>
                    <dd>{product.articleNo ?? "-"}</dd>
                </div>
                <div>
                    <dt>kWh per år</dt>
                    <dd>{formatOptional(product.kwhPerYear)}</dd>
                </div>
                <div>
                    <dt>Livslängd</dt>
                    <dd>{lifetimeText}</dd>
                </div>
                <div>
                    <dt>Mätkvalitet</dt>
                    <dd>{product.measurementQuality === "measured" ? "Uppmätt" : "Uppskattad"}</dd>
                </div>
                <div>
                    <dt>Kommentar</dt>
                    <dd>{product.comment ?? "-"}</dd>
                </div>
                <div>
                    <dt>Total energiförbrukning</dt>
                    <dd>{totalText}</dd>
                </div>

                {/*Only shown when something is missing*/}
                {product.missing.length > 0 && (
                    <div className="energy-details-full">
                        <dt>Saknas</dt>
                        <dd><MissingBadges missing={product.missing}/></dd>
                    </div>
                )}
            </dl>
        </div>
    )
}
export default ProductDetails;