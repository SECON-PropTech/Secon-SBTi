type Props = {
    label: string;
    unit: string;
    value: string;
    onChange: (value: string) => void;
    autoFocus?: boolean;
    error?: string;
    showMissing?: boolean; 
};

//A small field for kwh per år and livslängd
// Empty fields means the value is still missing.
function NumberField({ label, unit, value, onChange, autoFocus = false,
    error, showMissing = true }: Props) {
        let className = "form-control form-control-sm energy-field energy-number-field";
        
        if (showMissing && value === "") {
            className += " energy-field-missing";
        }
        if (error) {
            className += " energy-field-error";
        }

        return (
            <div>
                <div className = "d-flex align-items-center gap-1">
                    <input 
                        type="text"
                        inputMode="decimal"
                        aria-label={label}
                        className={className}
                        value={value}
                        placeholder="Saknas"
                        autoFocus={autoFocus}
                        onFocus={(event) => event.target.select()}
                        onChange={(event) => onChange(event.target.value)}/>
                    <span className="small text-secondary">{unit}</span>    
                </div>
                {error && <div className="small text-danger mt-1">{error}</div>}
            </div>
        );
}

export default NumberField;