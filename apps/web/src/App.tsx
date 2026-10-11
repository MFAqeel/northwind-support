import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { clearSession, getSession } from './api/session';

/** Application shell: the header, the navigation, and wherever the router puts the page. */
export function App() {
  const navigate = useNavigate();
  const session = getSession();
  return (
    <div className="app">
      <header className="app__header">
        <Link className="app__brand" to="/tickets">
          Northwind Support
        </Link>

        <nav className="app__nav" aria-label="Main">
          <NavLink to="/tickets">Tickets</NavLink>
          <NavLink to="/customers">Customers</NavLink>
        </nav>
        <div>{session?.agent.fullName} <button type="button" onClick={() => { clearSession(); navigate('/login', { replace: true }); }}>Log out</button></div>
      </header>

      <main className="app__main">
        <Outlet />
      </main>
    </div>
  );
}
