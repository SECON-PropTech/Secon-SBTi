import type { PageId } from "../types/navigation";
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
]

//ActivePage = the page that is open
//onSelect = called with the page id when a menu item is clikced
type Props = {
    activePage: PageId | null;
    onSelect: (page: PageId) => void;
};

function Sidebar({activePage, onSelect}: Props){
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

            <p className="sidebar-footer">SBTi-rapportering</p>
        </aside>
    )
}

export default Sidebar;