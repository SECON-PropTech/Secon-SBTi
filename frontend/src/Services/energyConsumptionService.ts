//Communicates with Scope311Controller in backend API
import type { EnergyConsumptionResponse } from "../types/energyConsumption";

//Gets the energy consumption for one year from backend
export async function getEnergyConsumption(year: number): Promise<EnergyConsumptionResponse> {
    const response = await fetch("/api/scope311?year=" + year);

    //respnse.ok is false if the backed answered with an error
    if (!response.ok) {
        throw new Error("could not fetch data: " + response.status);
    }
    return await response.json();
}