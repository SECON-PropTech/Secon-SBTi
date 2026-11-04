type Props = {
    year: number;
    onChange: (year: number) => void;
};

//a drop-down list for choosing the reporting year.
//it doesnt store the year itself, the page does that and gets told when it changes.
function YearPicker({ year, onChange }: Props) {
    //This year and back to 2020, newest first
   const years: number[] = [];
   const thisYear = new Date().getFullYear();
   for (let listYear = thisYear; listYear >= 2020; listYear--) {
    years.push(listYear);
   }

   return (
    <div className="d-flex align-items-center gap-2">
        <label htmlFor="report-year" className="small text-secondary"> År </label>
        <select
            id="report-year"
            className="form-select form-select-sm"
            value={year}
            //e.target.value is text, Number() turns it into a number
            onChange={(event) => onChange(Number(event.target.value))}>
            
            {years.map((listYear) => (
                <option key={listYear} value={listYear}>
                    {listYear}
                </option>
            ))}
            </select>
    </div>
   );
}

export default YearPicker;