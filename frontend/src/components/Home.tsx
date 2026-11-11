import { useState } from "react";
import type { ComponentType } from "react";
import type { PageId } from "../types/navigation";

import Sidebar from "../components/Sidebar";
import Scope31 from "../pages/Scope31";
import Scope311 from "../pages/Scope311";
import SoldProductManagement from "../pages/SoldProductsManagement";

const pageComponents: Record<PageId, ComponentType> = {
  "scope-3.1": Scope31,
  "scope-3.11": Scope311,
  "scope-3.12": SoldProductManagement,
};

function Home() {
  const [activePage, setActivePage] = useState<PageId>("scope-3.1");

  const Page = pageComponents[activePage];

  return (
    <div className="app-layout">
      <Sidebar
        activePage={activePage}
        onSelect={setActivePage}
      />

      <main className="app-main">
        <Page />
      </main>
    </div>
  );
}

export default Home;