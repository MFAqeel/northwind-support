import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { SlaStatus } from '../../../types/api';
import { SlaCell } from './SlaCell';

describe('SLA indicator', () => {
  it.each([
    ['NotApplicable', 'No SLA'],
    ['WithinSla', 'Within SLA'],
    ['AtRisk', 'At risk'],
    ['Breached', 'Breached'],
    ['Met', 'Met'],
  ] as [SlaStatus, string][])('labels %s as %s', (status, label) => {
    render(<SlaCell slaStatus={status} dueAtUtc={null} />);
    expect(screen.getByText(label)).toBeInTheDocument();
  });
});
