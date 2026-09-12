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
