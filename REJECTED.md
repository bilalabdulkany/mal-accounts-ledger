<<<<<<< HEAD
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
=======
# REJECTED.md

The following acceptance criteria are intentionally rejected because they contradict other non-negotiable requirements or arithmetic.

## Rejected criterion 1 — Day-6 reversal restores all balances and fees to pre-E7 values

**REJECTED.**

E7 books -AED 620.00 and causes the specified AED 25.00 Day-2 overdraft fee. E9 books a +AED 620.00 reversal. The transaction effect of E7 is cancelled, but the fee is a separate booked ledger entry. The ledger is append-only and no event record may be mutated or deleted.

Therefore E7 + E9 net to zero, while the fee remains. The criterion would require deleting or reversing the fee without providing such a rule.

## Rejected criterion 2 — all three BHD installments are BHD 3.334

**REJECTED.**

BHD 10.000 / 3 = 3.333333.... At three decimal places, three installments of 3.334 total BHD 10.002. That violates the source amount. The implementation instead allocates the rounding remainder deterministically: 3.333 + 3.333 + 3.334 = 10.000.

The specification does not state which installment receives the remainder, so that allocation is documented as an implementation policy in AMBIGUITIES.md.

## Rejected criterion 3 — discard an interest rounding remainder

**REJECTED.**

The specification simultaneously requires daily accruals to be rounded and requires those rounded daily accruals to sum exactly to the capitalized total. Discarding a difference would violate the exact-sum requirement. The implementation capitalizes the sum of the rounded daily accruals as one Day-6 credit.

## Approaches abandoned during the build

### Mutating E7 when E9 arrives

Abandoned because the ledger is append-only. E9 is represented as a new entry referencing E7.

### Using `double`

Abandoned because the domain requires deterministic decimal currency precision. `decimal` plus `Money` was chosen.

### Treating authorization holds as ledger entries

Abandoned because a hold affects available balance but is not an actual posted debit/credit.

### Equal rounded BHD installments

Abandoned because three equal rounded amounts cannot reconcile exactly to BHD 10.000.

### Assessing a new fee for every later day made negative by E7

Abandoned because the supplied acceptance criterion says E7 causes exactly one fee, on Day 2. The implementation therefore ties the assessment to E7's value day and documents this interpretation.
>>>>>>> origin/main
