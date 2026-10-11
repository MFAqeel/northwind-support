export interface AgentSession { id: number; fullName: string; email: string }
export interface LoginResponse { token: string; expiresAtUtc: string; agent: AgentSession }
const KEY = 'northwind-session';
export function getSession(): LoginResponse | null {
  try { const raw = sessionStorage.getItem(KEY); if (!raw) return null;
    const value = JSON.parse(raw) as LoginResponse;
    if (!value.token || new Date(value.expiresAtUtc).getTime() <= Date.now()) { clearSession(); return null; }
    return value;
  } catch { return null; }
}
export function saveSession(session: LoginResponse): void { sessionStorage.setItem(KEY, JSON.stringify(session)); window.dispatchEvent(new Event('auth-change')); }
export function clearSession(): void { sessionStorage.removeItem(KEY); window.dispatchEvent(new Event('auth-change')); }
