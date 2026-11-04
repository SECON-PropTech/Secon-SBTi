//import { Navigate, Outlet } from "react-router-dom";
import { Outlet } from "react-router-dom";

//kontrollera inloggningsstatus via /api/auth/me
function ProtectedRoute() {
    return <Outlet />;
    //const isAuthenticated = false; // Ersätt med riktig auth-kontroll

    //return isAuthenticated
    //    ? <Outlet />
    //    : <Navigate to="/login" replace />;
}
export default ProtectedRoute;

