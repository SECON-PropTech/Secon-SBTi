// The pages that can be opened in the app.
//  a PageId can only be one of these strings.

// To add a new page:
// 1. Add its id here eg: | "scope-1"
// 2. Add the page component to pageComponents in App.tsx
// 3. Add a menu item to menuItems in Sidebar.tsx (if it should be shown in the menu)
export type PageId = "scope-3.1" | "scope-3.11" | "scope-3.12";