import type { EnergyProductRow } from "../../types/energyConsumption";
import { formatNumber } from "../../utils/format";
import { Calculator, Hourglass, Zap } from "lucide-react";
import AddButton from "./AddButton";
import Tooltip from "./Tooltip";
import NumberField from "./NumberField"
import { toNumber } from "../../utils/productDraft";
import { calculateActivityKwh } from "../../utils/calculateActivityKwh";
import type { DraftErrors, EditField, ProductDraft } from "../../types/product";
import SalesLines from "./SalesLines";

type Props = {
    product: EnergyProductRow;
    draft: ProductDraft | null;
    errors: DraftErrors;
    focusField: EditField;
    year: number;
    onEdit: (field: EditField) => void;
    onChange: (changes: Partial<ProductDraft>) => void;

};
//The product details-part that shows when a row is opened
// fields turn into inputs when the row is being edited.
function ProductDetails({ product, draft, errors, focusField, year, onEdit, onChange, }: Props) {
    const kwhPerYear = draft ? toNumber(draft.kwhPerYear) : product.kwhPerYear;

    const lifetimeYears = draft ? toNumber(draft.lifetimeYears) : product.lifetimeYears;

    const activityKwh = draft ? calculateActivityKwh(product.quantity, kwhPerYear, lifetimeYears) :
    product.activityKwh;

    
    // The backend decides if a lifetime is needed (not when kWh is 0)
    const lifetimeNotNeeded = draft ? kwhPerYear === 0 : 
    product.lifetimeYears === null && !product.missing.includes("lifetime");
    
    // kWh per year: an input, the value or a button when it's missing
    let kwhValue;
    if (draft) {
        kwhValue = (
            <NumberField 
            label="kWh per år"
            unit= "kWh"
            value={draft.kwhPerYear}
            autoFocus={focusField === "kwhPerYear"}
            error={errors.kwhPerYear}
            onChange={(value) => onChange({ kwhPerYear: value })}/>
        );

    } else if (product.kwhPerYear === null) {
        kwhValue = <AddButton text="Lägg till" warning onClick={() =>
            onEdit("kwhPerYear")}/>;
        } else {
          kwhValue = formatNumber(product.kwhPerYear) + " kWh";  
        }

    //lifetime: an input, the value, "Ej aktuellt" for 0 kwh or a button when missing
    let lifetimeValue;
    if (draft) {
        lifetimeValue = (
          <NumberField
            label="Livslängd"
            unit="år"
            value={draft.lifetimeYears}
            autoFocus={focusField === "lifetimeYears"}
            error={errors.lifetimeYears}
            showMissing={!lifetimeNotNeeded}
            onChange={(value) => onChange({ lifetimeYears: value })}/>
        );

    } else if (product.lifetimeYears !== null) {
        lifetimeValue = formatNumber(product.lifetimeYears) + " år";
    } else if (lifetimeNotNeeded) {
        lifetimeValue = <span className="text-secondary">Ej aktuellt</span>;
      } else {
        lifetimeValue = <AddButton text="Lägg till" warning onClick={() => 
            onEdit("lifetimeYears")}/>;
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
                        {draft ? (
                            //two buttons to switch between, measured or estimated
                            <div className="energy-quality-switch">
                                <button type="button"
                                className={draft.measurementQuality === "measured"
                                ? "energy-pill energy-pill-green" : "energy-pill energy-pill-off"}
                                onClick={() => onChange({ measurementQuality: "measured"})}>
                                    Uppmätt 
                                </button>

                                <button type="button"
                                className={draft.measurementQuality === "estimated"
                                 ? "energy-pill" : "energy-pill energy-pill-off"}
                                onClick={() => onChange({ measurementQuality: "estimated" })}>
                                  Uppskattad
                                </button>
                            </div> 

                            ) : product.measurementQuality === "measured" ? (
                            <span className="energy-pill energy-pill-green">Uppmätt</span>) : (
                                <span className="energy-pill">Uppskattad</span>
                            
                            )}
                    </dd>
                </div>
                <div>
                    <dt>Kommentar</dt>
                    <dd>
                        {draft ? (
                            <input
                             type="text"
                             aria-label="Kommentar"
                             className="form-control form-control-sm energy-field"
                             value={draft.comment}
                             placeholder="Valfritt"
                             autoFocus={focusField === "comment"}
                             onChange={(event) => onChange ({ comment: event.target.value })}/>
                          ) : ( 
                            product.comment ?? <span className="energy-no-comment">Ingen kommentar</span>
                          )}
                    </dd>
                </div>
            </dl>

            {/*The calculation. A missing value is shown as ? in orange */}
            <div className="energy-calculation">
                <Calculator size={16}/>
                <span>{formatNumber(product.quantity)} st</span>
                <span> × </span>
                {kwhPerYear === null ? (
                    <span className="energy-missing-text">? kWh</span>) : (
                      <span>{formatNumber(kwhPerYear)} kWh</span>  
                    )}

                    {/* Skip lifetime when it isnt need (0 kwh)*/}
                    {!lifetimeNotNeeded && (
                        <>
                        <span> × </span>
                        {lifetimeYears === null ? (
                            <span className="energy-missing-text">? år</span>) : (
                                <span>{formatNumber(lifetimeYears)} år</span>
                            )}
                        </>
                    )}

                    <span> = </span>
                    {activityKwh === null ? (
                        <span className="energy-missing-text"> Räknas inte</span>) : (
                            <strong>{formatNumber(activityKwh)} kWh</strong>
                    )}

                    {/*Reminder about shortcuts*/}
                    {draft && <span className="ms-auto small">
                        Enter (spara) / Esc (avbryt)
                        </span>}
            </div>
            {/*The sales behind the quantity column */}
            <SalesLines productId={product.productId} year={year}/>
        </div>
    );
}
export default ProductDetails;