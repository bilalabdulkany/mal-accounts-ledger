# AMBIGUITIES.md

This file records interpretations made where the specification does not fully define an operational rule.

## 1. What does "that day's" mean for overdraft assessment after a late value-dated event?

E7 is booked on Day 5 but has value date Day 2. It makes Day 2 negative, but because ledger balance is cumulative, it also makes later historical days negative until E9 arrives.

**Resolution:** assess the overdraft fee for the triggering event's value day only. This is required to reconcile the prose with the explicit criterion that E7 causes exactly one fee, on Day 2. Do not independently create Day-3/Day-4/Day-5 fees from the same late event.

## 2. Is the Day-2 fee calculated before or after the fee itself?

The specification says the fee is assessed when the day's closing ledger balance is negative. A fee cannot be used to decide whether the same fee should exist.

**Resolution:** calculate the balance immediately after the triggering ledger entry and before the fee entry. E7 therefore sees -370.00 and causes a 25.00 fee.

## 3. Does E9 reverse an overdraft fee caused by E7?

The specification says E9 reverses E7, not the fee. It also says the ledger is append-only.

**Resolution:** E9 reverses E7's ledger entry only. The fee remains. A separate explicit fee-reversal event would be required to remove its economic effect.

## 4. Does a settlement consume the hold amount or book the settlement amount?

E5 has a 200.00 hold but settles for 185.00.

**Resolution:** book the actual settlement amount, 185.00, and mark the authorization settled. The 200.00 hold is a reservation, not a ledger debit.

## 5. What happens to a settlement with an unknown authorization?

E6 references Auth-Z, which has no preceding authorization.

**Resolution:** reject the settlement, record an error against E6, and append no ledger debit. Auth-Z is not created as an authorization merely because a settlement referenced its identifier; the error is the observable rejection state. This follows the acceptance criterion that funds must not leave the account.

## 6. Which BHD installment receives the rounding remainder?

10.000 / 3 cannot be represented as three equal 3-decimal values.

**Resolution:** use remainder-to-last: 3.333, 3.333, 3.334. Any deterministic allocation preserving the total could be defensible; this choice is simple and easy to explain.

## 7. What balance is used for authorization?

The specification defines available balance as ledger balance minus active holds and requires it to remain non-negative after the new hold.

**Resolution:** use the ledger balance at the authorization's value day and subtract all currently active approved holds for that account, including the new hold. A settled/rejected authorization contributes no active hold.

## 8. Can ordinary debits make the ledger negative?

The specification does not say that ordinary debits are rejected when funds are insufficient. It only explicitly constrains authorization approval.

**Resolution:** allow the E7 debit to post even though it creates a negative historical balance. Overdraft handling is represented separately by the fee rule.

## 9. When is interest calculated relative to Day-6 capitalization?

The specification says daily interest accrues and capitalizes as a single credit at the end of Day 6.

**Resolution:** calculate the six daily accruals from closing ledger balances before the capitalization entry, then append one Day-6 interest credit equal to the sum of the rounded daily accruals.

## 10. What does the daily report's closing balance include on Day 6?

Because capitalization occurs at the end of Day 6 and is a ledger credit with value date Day 6, the final Day-6 closing ledger balance includes the capitalized interest.

**Resolution:** daily snapshots are built after capitalization, so Day 6 includes the interest credit.

## 11. What does "rounded daily accruals must sum exactly" mean if the raw total differs after rounding?

**Resolution:** the capitalized amount is defined as the sum of the rounded daily accruals. No residual is discarded. This directly satisfies the exact-sum requirement.

## 12. What does "posted as three equal instalments" mean when equal stored amounts are impossible?

**Resolution:** interpret "equal" as equal allocation before currency rounding, then allocate the unavoidable rounding remainder to one installment so that the stored entries reconcile exactly. This is more important than forcing three equal stored values that do not add to the source amount.

## 13. Should a rejected authorization be retained as an authorization state?

**Resolution:** yes. Rejection is a business outcome and should remain visible in the replay result, but it does not create an active hold or ledger entry.
