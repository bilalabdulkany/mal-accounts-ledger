# mal-accounts-ledger

<<<<<<< HEAD
In-memory account-ledger core for the six-day MAL exercise.
=======
An in-memory account-ledger core for the MAL career exercise.
>>>>>>> origin/main

## Requirements

- .NET 8 SDK
<<<<<<< HEAD
- No database, persistence, web layer, or UI

## Run

```bash
dotnet restore
dotnet build
dotnet test
```

The test suite contains one deliberately failing test. The remaining tests are expected to pass. It encodes rejected acceptance criterion #6 and is intentionally expected to fail. All other tests should pass.

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
=======
- No database, web server, UI, or persistence service is required.

## Run

From the repository root:

```bash
dotnet restore
dotnet test

dotnet run --project src/Mal.Accounts.Ledger.Runner
```

There is one deliberately failing xUnit test. It encodes the rejected requirement that E9 should restore fees to the pre-E7 state. The implementation is expected to fail that test because the fee is a separately booked append-only entry.

To verify the functional suite excluding the intentional failure:

```bash
dotnet test --filter "FullyQualifiedName!~INTENTIONAL_FAILURE"
```

## Output

The runner prints, for each account and each day:

- closing ledger balance;
- fee assessments for that assessment day;
- authorization states known by that day;
- processing errors booked on that day;
- rounded daily interest accrual;
- total Day-6 interest capitalization.

It also prints the complete append-only ledger history so E7 and its E9 reversal can be inspected directly.

## Important interpretation

The event stream is replayed in booking order. Ledger balances use value dates. E7 is booked on Day 5 but has value date Day 2, so it changes the Day-2 historical balance. The supplied criterion explicitly says that this causes one Day-2 overdraft fee. E9 later reverses E7 but does not delete the already-booked fee.

## Expected key facts

- Day-2 balance immediately after E7 but before its fee: AED -370.00.
- Day-2 fee: AED 25.00 exactly once.
- Auth-A: APPROVED, then SETTLED for AED 185.00.
- Auth-Z settlement: rejected; no money leaves the account.
- Auth-B: REJECTED because the hold would make available balance negative.
- E9: +AED 620.00 reversal of E7; E7 remains in history.
- BHD E10 installments: 3.333, 3.333, 3.334.
- AED daily interest: 0.10, 0.09, 0.25, 0.18, 0.18, 0.42 = 1.22.
- BHD daily interest: 0.000, 0.000, 0.000, 0.000, 0.004, 0.004 = 0.008.
>>>>>>> origin/main
