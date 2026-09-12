# mal-accounts-ledger

In-memory account-ledger core for the six-day MAL exercise.

## Requirements

- .NET 8 SDK
- No database, persistence, web layer, or UI

## Run

```bash
dotnet restore
dotnet build
dotnet test
```

The test suite contains one deliberately failing test. It encodes rejected acceptance criterion #6 and is intentionally expected to fail. All other tests should pass.

Run the executable:

```bash
dotnet run --project src/Mal.Accounts.Ledger.Runner
```

The runner replays E1-E10 in the supplied order and prints, for every account and day:

- closing ledger balance
- fee assessments
- daily interest accrual
- authorization state as of that day
- errors booked that day

It also prints the append-only ledger entries and the Day-6 capitalized interest.

## Key interpretation

`BookedDay` is when the event enters the stream. `ValueDay` determines which historical ledger balances the resulting ledger entry affects. E7 and E9 are both booked late but have Day-2 value dates.

The implementation processes events in stream order. Interest is calculated after the complete stream has been replayed and before interest capitalization is appended. Daily accruals are rounded to account currency precision; the capitalization amount is the sum of those rounded daily accruals.

## Verification

`scripts/verify_scenario.py` independently checks the scenario arithmetic, including the transient Day-2 -370 balance, the Day-2 fee, E9's final Day-2 balance, BHD installment allocation, and daily interest totals.
