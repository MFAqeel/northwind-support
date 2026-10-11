import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ticketsApi } from '../../../api/tickets';
import type { TicketDetail } from '../../../types/api';
import { EscalationPanel } from './EscalationPanel';

vi.mock('../../../api/tickets', () => ({ ticketsApi: {
  getEscalations: vi.fn().mockResolvedValue([]), escalate: vi.fn(),
} }));
const ticket = {
  id: 1, priority: 'Medium', status: 'Open',
} as TicketDetail;

describe('EscalationPanel', () => {
  beforeEach(() => { vi.clearAllMocks(); });
  it('does not submit a short reason', async () => {
    render(<EscalationPanel ticket={ticket} onUpdated={vi.fn()} />);
    fireEvent.change(screen.getByLabelText(/Reason/), { target: { value: 'no' } });
    fireEvent.click(screen.getByRole('button', { name: /Escalate one level/ }));
    expect(ticketsApi.escalate).not.toHaveBeenCalled();
  });
  it('shows 409 errors without updating the ticket', async () => {
    vi.mocked(ticketsApi.escalate).mockRejectedValueOnce(new Error('Critical tickets cannot be escalated.'));
    const onUpdated = vi.fn();
    render(<EscalationPanel ticket={ticket} onUpdated={onUpdated} />);
    fireEvent.change(screen.getByLabelText(/Reason/), { target: { value: 'Need urgent attention' } });
    fireEvent.click(screen.getByRole('button', { name: /Escalate one level/ }));
    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Critical tickets cannot be escalated.'));
    expect(onUpdated).not.toHaveBeenCalled();
  });
});
