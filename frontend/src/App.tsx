import { useState } from "react";
import type { ComponentType } from "react";
import type { PageId } from "./types/navigation";
import Sidebar from "./components/Sidebar"; 
import Scope31 from "./pages/Scope31";
import Scope311 from "./pages/Scope311";
import Scope312 from "./pages/Scope312";

//Which component to show for each page.
//Record<PageId, ComponentType> = every PageId in types/navigation.ts
//must be listed here.

const pageComponents: Record<PageId, ComponentType> = {
  "scope-3.1": Scope31,
  "scope-3.11": Scope311,
  "scope-3.12": Scope312,

};

function App() {
  //null = start page empty
  const [activePage, setActivePage] = useState<PageId | null>(null);

  //Look up the component for the active page. null = nothing selected
  let Page: ComponentType | null = null;
  if (activePage !== null) {
    Page = pageComponents[activePage];
  }

  return (
    <div className="app-layout">
      <Sidebar activePage={activePage} onSelect={setActivePage}/>
      <main className="app-main">{Page && <Page/>}</main>
    </div>
  );

}
export default App
