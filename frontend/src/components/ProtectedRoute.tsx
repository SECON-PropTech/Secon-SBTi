import { useEffect, useState } from "react";
import { Navigate, Outlet } from "react-router-dom";
import { getCurrentUser } from "../Services/authService";

function ProtectedRoute() {
    const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(null);
    const [hasError, setHasError] = useState(false);

    useEffect(() => {
        async function checkAuthentication() {
            try {
                const user = await getCurrentUser();
                setIsAuthenticated(user !== null);
            } catch {
                setHasError(true);
            }
        }

        checkAuthentication();
    }, []);

    if (isAuthenticated === null && !hasError) {
        return <p>Kontrollerar inloggning...</p>;
    }

    if (hasError) {
        return <p>Kunde inte kontrollera inloggningen. Försök igen senare.</p>;
    }

    if (isAuthenticated) {
        return <Outlet />;
    } else {
        return <Navigate to="/login" replace />;
    }
}

export default ProtectedRoute;