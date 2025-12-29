-- Create triage_notes table for triage records
CREATE TABLE IF NOT EXISTS triage_notes (
    triage_note_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id) ON DELETE CASCADE,
    jc_code VARCHAR(20) REFERENCES judgement_cards(jc_code),
    current_hypothesis TEXT,
    next_action TEXT,
    confidence INT NOT NULL,
    escalation_required BOOLEAN DEFAULT FALSE,
    escalated_to UUID REFERENCES users(id),
    note TEXT,
    created_by UUID NOT NULL REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_triage_notes_ticket_id ON triage_notes(ticket_id);
CREATE INDEX IF NOT EXISTS idx_triage_notes_jc_code ON triage_notes(jc_code);
