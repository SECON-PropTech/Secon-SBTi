//small function for displaying numbers in swedish format.
//located in utils/ so that all components + other scopes can use them.
// 814710 -> 814 710
export function formatNumber(number: number): string {
    return number.toLocaleString("sv-SE");
}




