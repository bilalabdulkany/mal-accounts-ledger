# Architecture

## Shape

The solution has three small layers:

- `Domain`: immutable records and enums representing accounts, money, events, ledger entries, authorizations, errors, and results.
- `Application`: `LedgerEngine`, which validates and replays the ordered event stream and derives fees, interest, and daily snapshots.
- `Runner`: a console-only executable that supplies E1-E10 and prints the required report.

Tests exercise both domain invariants and the complete supplied scenario.

## Important design choices

### Append-only ledger

A reversal appends a new ledger entry referencing the original. E7 is never modified or deleted. This preserves an auditable history and is required by the specification.

### Booked day versus value date

Events are processed in `BookedDay` order, but ledger balances are calculated from entries whose `ValueDay` is on or before the requested day. This is necessary for E7/E9, which are booked on Days 5/6 but have Day-2 value dates.

### Authorization versus ledger

An authorization is a hold, not a ledger movement. Approval uses ledger balance minus active holds minus the proposed hold. Settlement creates the actual debit and transitions the authorization to `Settled`.

Authorization snapshots use booking-day history so Day 2 can show Auth-A as approved even though it is settled later on Day 4.

### Money

`Money` is an immutable value object backed by `decimal`. It rounds at construction using the currency's scale: AED 2 places and BHD 3 places. Currency mismatches are rejected by account/event validation.

### Interest

The complete event stream is replayed first. Each positive historical daily balance is multiplied by `0.0004` (0.04%) and rounded to the account currency precision. The single Day-6 capitalization amount is the sum of the rounded daily accruals. This makes the required reconciliation explicit.

### Errors

Rejected events remain part of the input history but do not create ledger movements. Errors carry the owning account, booking day, event ID, stable code, and message, allowing the runner to report errors per day and account.

## Trade-offs

The implementation intentionally favors deterministic, auditable behavior over a generic event-sourcing framework. There is no persistence, no event bus, and no infrastructure because those would add code without serving the exercise.

The engine is stateful and accepts one replay per instance. A fresh engine represents a fresh in-memory replay. This prevents accidental double-processing of the same stream.
