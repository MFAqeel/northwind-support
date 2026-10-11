import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { getSession } from '../api/session';
export function RequireAuth() { const location = useLocation(); return getSession() ? <Outlet /> : <Navigate to={`/login?next=${encodeURIComponent(location.pathname + location.search)}`} replace />; }
