//import { Navigate, Outlet } from "react-router-dom";
import { Outlet } from "react-router-dom";

function ProtectedRoute() {
    return <Outlet />;
    //const isAuthenticated = false; // Ersätt med riktig auth-kontroll

    //return isAuthenticated
    //    ? <Outlet />
    //    : <Navigate to="/login" replace />;
}
export default ProtectedRoute;