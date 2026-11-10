import type { LoginUser, CurrentUser, ApiError } from "../types/auth";

//Communicates with AuthController in backend API

// Sends login credentials to the backend API and handles the response
export async function login(
    email: string,
    password: string
): Promise<LoginUser> {
    const response = await fetch("/api/auth/login", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        credentials: "include",
        body: JSON.stringify({
            email,
            password,
        }),
    });

    const data: unknown = await response.json();

    if (!response.ok) {
        const errorData = data as ApiError;
        throw new Error(
            errorData.message || "Inloggningen misslyckades.");
    }

    return data as LoginUser;
}

// Retrieves information about the currently authenticated user
export async function getCurrentUser(): Promise<CurrentUser | null> {
    const response = await fetch("/api/auth/me", {
        method: "GET",
        credentials: "include",
    });

    if (response.status === 401) {
        return null;
    }

    if (!response.ok) {
        throw new Error("Kunde inte kontrollera inloggningsstatus.");
    }

    return await response.json() as CurrentUser;
}