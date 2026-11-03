// a drop-down list for choosing the reporting year.
//the component stores nothing. The year comes from the page via props
//and when the user picks a new year, onChange is called.

type Props = {
    year: number;
    onChange: (year: number) => void;
};

function YearPicker({ year, onChange }: Props) {
    //The years in the list: this year and back to 2020, newest first
   const years: number[] = [];
   const thisYear = new Date().getFullYear();
   for (let y = thisYear; y >= 2020; y--) {
    years.push(y);
   }

   return (
    <div className="d-flex align-items-center gap-2">
        <label htmlFor="report-year" className="small text-secondary"> År </label>
        <select
            id="report-year"
            className="form-select form-select-sm"
            value={year}
            //e.target.value is text, Number() turns it into a number
            onChange={(e) => onChange(Number(e.target.value))}>
            
            {years.map((y) => (
                <option key={y} value={y}>
                    {y}
                </option>
            ))}
            </select>
    </div>
   );
}

export default YearPicker;