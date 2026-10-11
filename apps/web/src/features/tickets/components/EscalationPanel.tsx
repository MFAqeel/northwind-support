import { useEffect, useState, type FormEvent } from 'react';
import { ticketsApi } from '../../../api/tickets';
import { toErrorMessage } from '../../../api/client';
import type { TicketDetail, TicketEscalation } from '../../../types/api';
import { formatDateTime } from '../../../lib/format';

interface Props { ticket: TicketDetail; onUpdated: (ticket: TicketDetail) => void }
export function EscalationPanel({ ticket, onUpdated }: Props) {
  const [history, setHistory] = useState<TicketEscalation[]>([]);
  const [reason, setReason] = useState('');
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string>();
  const [historyError, setHistoryError] = useState<string>();
  const disabled = ticket.priority === 'Critical' || ticket.status === 'Resolved' || ticket.status === 'Closed';
  useEffect(() => {
    let active = true;
    void ticketsApi.getEscalations(ticket.id).then((rows) => { if (active) setHistory(rows); })
      .catch((e: unknown) => { if (active) setHistoryError(toErrorMessage(e, 'Unable to load history')); });
    return () => { active = false; };
  }, [ticket.id]);
  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (disabled || pending) return;
    if (reason.trim().length < 5 || reason.trim().length > 500) {
      setError('Reason must be 5–500 characters.');
      return;
    }
    setPending(true); setError(undefined);
    try {
      const result = await ticketsApi.escalate(ticket.id, reason.trim());
      onUpdated(result.ticket);
      setHistory((rows) => [result.escalation, ...rows]);
      setReason('');
    } catch (e) { setError(toErrorMessage(e, 'Escalation failed')); }
    finally { setPending(false); }
  };
  return <div className="card">
    <h2>Escalation</h2>
    {disabled ? <p>This ticket cannot be escalated.</p> : <form onSubmit={(e) => void submit(e)}>
      <div className="field"><label htmlFor="escalation-reason">Reason (5–500 characters)</label>
        <textarea id="escalation-reason" value={reason} maxLength={500} minLength={5} required
          onChange={(e) => setReason(e.target.value)} /></div>
      <button className="button button--primary" type="submit" disabled={pending}>{pending ? 'Escalating…' : 'Escalate one level'}</button>
    </form>}
    {error && <p role="alert">{error}</p>}
    <h3>Escalation history</h3>
    {historyError && <p role="alert">{historyError}</p>}
    {history.length === 0 && !historyError && <p>No escalations yet.</p>}
    <ol>{history.map((entry) => <li key={entry.id}>
      <strong>{entry.fromPriority} → {entry.toPriority}</strong> — {entry.reason}<p>By {entry.escalatedBy} on {formatDateTime(entry.escalatedAtUtc)}</p>
      <p>Agent: {entry.fromAgentId ?? 'Unassigned'} → {entry.toAgentId ?? 'Unassigned'}</p>
      <p>Due: {formatDateTime(entry.fromDueAtUtc)} → {formatDateTime(entry.toDueAtUtc)}</p>
    </li>)}</ol>
  </div>;
}
