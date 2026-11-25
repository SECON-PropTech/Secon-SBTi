import { useState } from "react";
import { useNavigate } from "react-router-dom";
import type { PageId } from "../types/navigation";
import { logout } from "../Services/authService";
import seconLogo from "../assets/secon_logo.svg";
import "./Sidebar.css";

//One menu item: which page it opens, 
// the scope number and the full name

type MenuItem = {
    id: PageId;
    number: string;
    label: string;
};

const menuItems: MenuItem[] = [
    {id: "scope-3.1", number: "3.1", label: "Köpta varor och tjänster"},
    {id: "scope-3.11", number: "3.11", label: "Användning av sålda produkter"},
    {id: "scope-3.12", number: "3.12", label: "Slutbehandling av sålda produkter"},
    {id: "imports", number: "IMPORT", label: "Importering"},
]

//ActivePage = the page that is open
//onSelect = called with the page id when a menu item is clikced
type Props = {
    activePage: PageId | null;
    onSelect: (page: PageId) => void;
};

function Sidebar({activePage, onSelect}: Props){
    const navigate = useNavigate();

    const [isLoggingOut, setIsLoggingOut] = useState(false);
    const [logoutError, setLogoutError] = useState("");

    // Controls the visibility of the logout confirmation modal
    const [showLogoutModal, setShowLogoutModal] = useState<boolean>(false);

    // Logs out the user and redirects to the login page
    async function handleLogout() {
        setIsLoggingOut(true);
        setLogoutError("");

        try {
            await logout();
            navigate("/login", {replace: true, state: { loggedOut: true }});
        } catch (error) {
            setLogoutError(
                error instanceof Error
                    ? error.message
                    : "Något gick fel vid utloggningen."
            );
        } finally {
            setIsLoggingOut(false);
        }
    }
    return (
        <aside className="sidebar">
            <img src={seconLogo} alt="SECON" className="sidebar-logo"/>

            <p className="sidebar-heading">Scopes</p>

            <nav className="sidebar-nav">
              {menuItems.map((item) => (

                <button
                    key={item.id}
                    type="button"
                    className={item.id === activePage ? "sidebar-link active":"sidebar-link"}
                    onClick={() => onSelect(item.id)}>

                    <span className="sidebar-number">{item.number}</span>
                    <span>{item.label}</span>
                </button>
            ))}
            </nav>
            <div className="sidebar-bottom">
                <button
                    type="button"
                    className="sidebar-logout"
                    onClick={() => {setLogoutError("");
                    setShowLogoutModal(true);
}}
                    disabled={isLoggingOut}
                >
                    <span aria-hidden="true">↪</span>
                    {isLoggingOut ? "Loggar ut..." : "Logga ut"}
                </button>

                <p className="sidebar-footer">SBTi-rapportering</p>
            </div>

            {/* Logout confirmation modal */}
            {showLogoutModal && (
                <div className="logout-modal-overlay">
                    <div
                        className="logout-modal"
                        role="dialog"
                        aria-modal="true"
                        aria-labelledby="logout-modal-title"
                    >
                        <h5 id="logout-modal-title">Logga ut</h5>

                        <p>Är du säker på att du vill logga ut?</p>

                        {logoutError && (<p className="sidebar-error" role="alert">{logoutError}</p>)}

                        <div className="logout-modal-actions">
                            <button
                                type="button"
                                className="btn btn-outline-secondary"
                                onClick={() => setShowLogoutModal(false)}
                                disabled={isLoggingOut}
                            >
                                Avbryt
                            </button>

                            <button
                                type="button"
                                className="btn logout-modal-confirm"
                                onClick={handleLogout}
                                disabled={isLoggingOut}
                            >
                                {isLoggingOut ? "Loggar ut..." : "Logga ut"}
                            </button>
                        </div>
                    </div>
                </div>
            )}

        </aside>
    )
}


export default Sidebar;