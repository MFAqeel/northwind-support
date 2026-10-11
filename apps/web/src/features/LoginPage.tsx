import { useState, type FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { request } from '../api/client';
import { saveSession, type LoginResponse } from '../api/session';
export function LoginPage() {
  const [email, setEmail] = useState(''); const [password, setPassword] = useState('');
  const [error, setError] = useState(''); const [pending, setPending] = useState(false);
  const navigate = useNavigate(); const [params] = useSearchParams();
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); if (!email.trim() || !password) { setError('Enter email and password.'); return; }
    setPending(true); setError('');
    try { const result = await request<LoginResponse>('/auth/login', { method: 'POST', body: JSON.stringify({ email: email.trim(), password }) });
      saveSession(result); const next = params.get('next'); navigate(next?.startsWith('/') && !next.startsWith('//') ? next : '/tickets', { replace: true });
    } catch { setError('Invalid email or password.'); } finally { setPending(false); }
  }
  return <main className="app__main"><section className="card" style={{ maxWidth: 420, margin: '4rem auto' }}>
    <h1>Agent sign in</h1><form onSubmit={(e) => void submit(e)}>
    <div className="field"><label htmlFor="email">Email</label><input id="email" type="email" autoComplete="username" required value={email} onChange={e => setEmail(e.target.value)} /></div>
    <div className="field"><label htmlFor="password">Password</label><input id="password" type="password" autoComplete="current-password" required value={password} onChange={e => setPassword(e.target.value)} /></div>
    {error && <p role="alert">{error}</p>}<button className="button button--primary" disabled={pending}>{pending ? 'Signing in…' : 'Sign in'}</button>
    </form></section></main>;
}
