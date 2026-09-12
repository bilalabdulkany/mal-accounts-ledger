<<<<<<< HEAD
# Architecture

## Shape

The solution has three small layers:

- `Domain`: immutable records and enums representing accounts, money, events, ledger entries, authorizations, errors, and results.
- `Application`: `LedgerEngine`, which validates and replays the ordered event stream and derives fees, interest, and daily snapshots.
- `Runner`: a console-only executable that supplies E1-E10 and prints the required report.

Tests exercise both domain invariants and the complete supplied scenario.

## Important design choices

### Append-only ledger

A reversal appends a new ledger entry with explicit `ReversalOfEntryId` metadata. E7 is never modified or deleted. `SourceEventId` is reserved for the event that caused an entry; this avoids confusing a fee caused by E7 with a reversal of E7.

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
=======
# Architecture and Trade-offs

## Goal

Build the smallest deterministic in-memory core that can replay the supplied event stream while preserving an append-only accounting history. There is deliberately no HTTP layer, database, UI, or framework dependency in the core.

## Components

- **Domain models**: `Account`, `Money`, ledger events, ledger entries, authorizations, errors, fees, and daily results.
- **LedgerEngine**: the application service that replays events in booking order, validates commands, appends ledger entries, manages authorization state, assesses the supplied overdraft rule, calculates interest, and produces daily snapshots.
- **Runner**: a console-only adapter that supplies the exact event stream and prints the six-day report.
- **Tests**: xUnit tests covering the scenario and important invariants.

## Why this shape

### In-memory rather than persistence

Persistence is explicitly out of scope. Keeping the ledger as an in-memory list makes append-only behavior visible and deterministic without introducing infrastructure that the exercise does not require.

### `decimal` rather than `double`

The problem is monetary and requires currency-specific decimal precision. `decimal` avoids binary floating-point representation for ordinary financial arithmetic. `Money` additionally normalizes every stored amount to the currency's scale.

### `Money` rather than bare `decimal`

A bare decimal cannot prevent accidental AED/BHD mixing. `Money` carries the currency and rejects arithmetic between different currencies.

### Record-based immutable facts

`Account`, events, ledger entries, authorization snapshots, errors, fees, and interest accruals are records. The engine does not mutate an existing ledger entry. A reversal is another entry referencing the original entry.

### Mutable authorization state, immutable ledger history

An authorization is operational state: APPROVED can become SETTLED. Ledger entries are financial history and remain unchanged. This separates a stateful workflow from immutable accounting facts.

### Value date versus booking day

Every event carries both. The stream is processed in booking order, while ledger balance is calculated from entries whose `ValueDay <= requested day`. This is necessary for E7/E9, which arrive later but carry Day-2 value dates.

### Fee assessment

The supplied acceptance criterion says E7 causes exactly one fee on Day 2. Therefore the implementation assesses a fee for the triggering event's value day when that event makes that day's historical balance negative. It does not independently create additional fees for later days that become negative because of the same late value-dated entry. This interpretation is documented as an ambiguity because the prose rule alone could otherwise be read more broadly.

### Authorization holds

Holds are not ledger entries. Available balance is derived as ledger balance minus active approved holds. A rejected authorization does not create a hold or ledger entry. A successful settlement creates the actual debit and transitions the authorization to SETTLED.

### Settlement policy

A settlement requires an existing APPROVED authorization, matching account/currency, and an amount no greater than the approved hold. The supplied E5 is 185 against a 200 hold, so it is accepted and only 185 is booked as the debit.

### Installment rounding

BHD has a 0.001 quantum. `10.000 / 3` cannot be represented equally. The implementation uses a deterministic remainder-to-last policy: 3.333, 3.333, 3.334. The exact total is preserved.

### Interest

Interest is calculated after replay has established the six historical closing balances and before Day-6 interest capitalization. Negative and zero balances earn no interest. Each day's raw accrual is rounded to the account currency's scale; the sum of those rounded daily accruals is capitalized as one Day-6 credit.

## Trade-offs

### One engine versus many domain services

A single `LedgerEngine` keeps the exercise small and makes replay ordering explicit. In a production system, fee, authorization, and interest policies could be separate strategies/services. Splitting them further here would increase code volume without materially improving the demonstrated behavior.

### Exceptions versus result objects

Invalid business events in the supplied stream are expected outcomes, not exceptional process failures. They become `ProcessingError` records and the replay continues. Programmer/configuration errors such as currency mismatches inside a constructed domain object remain exceptions because they violate domain invariants.

### Recomputing balances versus maintaining running balances

The implementation recomputes a balance by scanning the append-only list. That is O(number of entries) per balance query and is entirely appropriate for the tiny six-day exercise. A production ledger would likely maintain indexed projections or materialized daily balances while retaining the immutable source ledger.
>>>>>>> origin/main
