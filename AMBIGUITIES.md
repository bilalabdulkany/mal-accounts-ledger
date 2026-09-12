# AMBIGUITIES

## 1. Late value-dated events and historical balances

**Ambiguity:** E7 is booked Day 5 but has value Day 2, and E9 is booked Day 6 but also has value Day 2.

**Resolution:** Process events in the supplied stream order. Ledger balance for day D includes every ledger entry with `ValueDay <= D` from the events processed so far; after the full stream is replayed, final historical snapshots include both E7 and E9. This preserves event-order effects such as E7 causing the fee before E9 arrives.

## 2. Fee assessment timing

**Ambiguity:** The rule describes a daily closing condition, but the stream contains late value-dated events.

**Resolution:** After every ledger-affecting event, check the affected value day. If that day's current ledger balance is negative and no fee has yet been assessed for `(account, day)`, append one fee. E7 therefore creates the Day-2 fee when E7 is processed. A later E9 cannot remove it.

## 3. Whether a reversal reverses a fee

**Ambiguity:** E9 reverses E7, while E7 caused a fee.

**Resolution:** E9 reverses only the referenced ledger transaction. The fee is a separate ledger entry and remains because the ledger is append-only. This is also why acceptance criterion #6 is rejected.

## 4. BHD installment remainder allocation

**Ambiguity:** 10.000 / 3 cannot be represented as three equal BHD amounts.

**Resolution:** Allocate the rounding remainder deterministically to the final installment: 3.333, 3.333, 3.334. Other deterministic allocation policies would preserve the total, but the specification does not select one.

## 5. Interest calculation with late events

**Ambiguity:** The prompt does not state whether interest is calculated incrementally as events arrive or after all late value-dated events are known.

**Resolution:** Calculate all six historical balances after the supplied stream has been fully replayed, then calculate the six daily accruals before appending Day-6 capitalization. The `InterestAccrual.ClosingBalance` field makes the pre-capitalization interest base explicit.

## 6. Interest rounding reconciliation

**Ambiguity:** Rounding each daily accrual can differ from rounding an unrounded multi-day total.

**Resolution:** Daily rounded accruals are authoritative for capitalization. Capitalization is their exact sum, and the engine verifies that the two values reconcile. No remainder is discarded.

## 7. Authorization snapshot timing

**Ambiguity:** An authorization has a final state after later events, but daily output needs historical state.

**Resolution:** Authorization state is determined by booking day. Auth-A is APPROVED on Days 2-3 and SETTLED from Day 4. Auth-B is REJECTED from Day 5.

## 8. Overdraft fee currency

**Ambiguity:** The supplied fee is explicitly AED 25.00 while ACC-002 is BHD.

**Resolution:** The scenario never creates a BHD overdraft. The engine represents the fee using the affected account currency so it remains internally currency-consistent if extended, but this behavior is outside the supplied scenario and is not relied upon by the acceptance tests.

## 9. Invalid events

**Ambiguity:** The prompt explicitly requires E6 to be rejected, but does not define whether all invalid events should abort the replay.

**Resolution:** Expected business-rule failures are recorded as errors and do not create ledger movements. Structural validation failures in the current replay are also recorded as errors rather than crashing the entire run, except for programmer/configuration invariants such as a second replay on the same engine.
