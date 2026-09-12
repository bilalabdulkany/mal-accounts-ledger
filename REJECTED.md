# REJECTED

## Acceptance criterion 6: E9 restores all balances and fees to pre-E7 values

**REJECTED.** E9 reverses E7's AED 620 ledger effect by appending a new AED 620 credit. The Day-2 AED 25 overdraft fee is a separate booked ledger entry. An append-only ledger cannot erase or mutate it. Final Day-2 balance is AED 225.00, not the pre-E7 balance of AED 250.00.

## Acceptance criterion 7: all three BHD installments are BHD 3.334

**REJECTED.** Three BHD 3.334 installments total BHD 10.002, while E10's total is BHD 10.000. The implementation uses 3.333, 3.333, 3.334 so the installments reconcile exactly.

## Acceptance criterion 8: discard an interest remainder

**REJECTED.** This contradicts the requirement that rounded daily accruals sum exactly to the capitalized total. The implementation sums the rounded daily accruals and capitalizes exactly that sum.

## Approaches abandoned during implementation

- **Primary-constructor Account plus explicit constructor:** abandoned because it generates duplicate constructor `CS0111`. The final `Account` is a conventional immutable record with one explicit constructor.
- **Mutating E7 during E9:** abandoned because it violates append-only history.
- **Using final authorization state for every daily snapshot:** abandoned because it makes Day-2 Auth-A appear settled before Day 4.
- **Inferring error ownership only from ledger entries:** abandoned because rejected events have no ledger entry. `ProcessingError` now carries `AccountId`.
- **Relying only on a capitalization total test:** abandoned because it would not prove individual daily rounding. Tests now verify each daily accrual, its balance base, the sum, and the single capitalization entry.
