// Calculates the total kWh for one product as the user is typing before anything is saved.
// This will probably be the only place the code is repeated in the frontend.
// After saving we fetch the data again, so the value the user ends up seeing
// always comes from the backend.
// Same as in backend/Calculations/EnergyConsumptionCalculator.cs

export function calculateActivityKwh(
    quantity: number,
    kwhPerYear: number | null,
    lifetimeYears: number | null):
   number | null {
        if (kwhPerYear === null) {
            return null;
        }
        if (kwhPerYear === 0) {
            return 0;
        }
        if (lifetimeYears === null) {
            return null;
        }
        return quantity * kwhPerYear * lifetimeYears;
    }