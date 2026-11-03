//small function for displaying numbers in swedish format.
//located in utils/ so that all components + other scopes can use them.
// 814710 -> 814 710
export function formatNumber(number: number): string {
    return number.toLocaleString("sv-SE");
}

// like formatNumber but shows a dash when the value is missing
export function formatOptional(number: number | null): string {
    if (number === null) {
        return "-";
    }
    return formatNumber(number);
}


