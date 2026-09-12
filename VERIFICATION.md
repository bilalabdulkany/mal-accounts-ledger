# Verification

## Supplied scenario

The scenario arithmetic was independently checked with `scripts/verify_scenario.py`.

### Acceptance criteria

| # | Result | Verification |
|---|---|---|
| 1 | PASS | When E7 is processed, Day-2 pre-fee balance is AED -370.00. |
| 2 | PASS | Exactly one Day-2 overdraft fee of AED 25.00 is appended. |
| 3 | PASS | E5 settles Auth-A for AED 185.00 and creates a debit entry. |
| 4 | PASS | E6 creates `UNKNOWN_AUTHORIZATION` and no E6 ledger entry. |
| 5 | PASS | Auth-B is rejected because its AED 90.00 hold would make available balance negative; a hold does not alter ledger balance. |
| 6 | REJECTED BY DESIGN | E9 reverses E7 with a new credit; the separately booked fee remains because the ledger is append-only. |
| 7 | REJECTED BY DESIGN | Three BHD 3.334 installments would total BHD 10.002. The implementation uses 3.333, 3.333, 3.334. |
| 8 | REJECTED BY DESIGN | Interest capitalization is exactly the sum of rounded daily accruals; no remainder is discarded. |

## Expected complete-replay balances

ACC-001 pre-capitalization: AED 250.00, 225.00, 625.00, 440.00, 440.00, 440.00.

AED rounded daily interest: AED 0.10, 0.09, 0.25, 0.18, 0.18, 0.18 = AED 0.98.

Day-6 final ACC-001 balance after capitalization: AED 440.98.

ACC-002 pre-capitalization: BHD 0.000, 0.000, 0.000, 0.000, 10.000, 10.000.

BHD rounded daily interest: BHD 0.000, 0.000, 0.000, 0.000, 0.004, 0.004 = BHD 0.008.

Day-6 final ACC-002 balance after capitalization: BHD 10.008.

## Test-suite expectation

The suite contains 16 tests: 15 should pass and one test is deliberately failing because it encodes rejected acceptance criterion 6. This deliberate failure is required by the assessment's intellectual-honesty requirement. There should be no other failing tests.

## Local execution

Run:

```bash
dotnet restore
dotnet test
python scripts/verify_scenario.py
```

The Python reference check must print `REFERENCE SCENARIO CHECK: PASS`.
