//small function for displaying numbers in swedish format.
//located in utils/ so that all components + other scopes can use them.

// 814710 -> 814 710
export function formatNumber(number: number): string {
    return number.toLocaleString("sv-SE");
}

// 2025-04-01 -> 1 apr 2025
export function formatDate(date: string): string {
    return new Date(date).toLocaleDateString("sv-SE", {
        day: "numeric",
        month: "short",
        year: "numeric",
    });
}



