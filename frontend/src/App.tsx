import { Routes, Route, Navigate } from "react-router-dom";
import Login from "./components/Authentication/Login"
import Home from "./components/Home";
import ProtectedRoute from "./components/Authentication/ProtectedRoute";
import PublicRoute from "./components/Authentication/PublicRoute";

function App() {
  return (
      <Routes>
          {/* Routes for users who are not logged in */}
          <Route element={<PublicRoute />}>
              <Route index element={<Navigate to="/login" replace />} />
              <Route path="/login" element={<Login />} />
          </Route>

          {/* Routes for authenticated users */}
          <Route element={<ProtectedRoute />}>
              <Route path="/home" element={<Home />} />
          </Route>
      </Routes>
  );
}

export default App;