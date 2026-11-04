//Communicates with AuthController in backend API

// Sends login credentials to the backend API and handles the response
export async function login(email: string, password: string) {
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

    const data = await response.json();

    if (!response.ok) {
        throw new Error(data.message || "Inloggningen misslyckades.");
    }

    return data;
}

// Retrieves information about the currently authenticated user
export async function getCurrentUser() {
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

    return await response.json();
}