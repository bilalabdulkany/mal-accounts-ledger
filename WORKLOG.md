# Work Log

All times are Gulf Standard Time (UTC+04:00).

## 2026-09-12

**09:XX** - Reviewed the supplied event stream. - Identified three
contradictory acceptance criteria. - Chose C#/.NET for the
implementation. - Began domain model design.

**12:52--13:40** - Created the PowerPoint slide. - Reviewed the event
stream and scenario requirements.

**14:36** - Reviewed the supplied event stream, with particular
attention to the E7/E9 reversal criteria. - Created the project
structure.

**14:39** - Updated the work log and recorded the implementation
progress.

**14:41** - Used ChatGPT to generate initial implementation scaffolding
and assist with source-code generation.

**During the initial implementation** - Encountered a C# compile-time
error in the `Account` domain model caused by a duplicate constructor
definition. - Regenerated/corrected the `Account` class so that it has a
single valid constructor while retaining the required domain
validation. - Continued with the domain and ledger implementation.

**15:20** - Reviewed the coding design and implementation approach.

**17:29** - Continued reviewing the coding design and implementation
decisions. - Checked the handling of ledger entries, authorizations,
reversals, fees and value dates.

**19:20--19:50** - Re-ran the program and checked the output against the
assessment criteria. - Reviewed calculated balances, fees, authorization
states, errors and interest. - Committed the implementation changes to
the repository.

**21:30--22:00** - Ran the xUnit test suite. - The test run reported
**16 tests: 10 passed and 6 failed**. - Investigated the failures rather
than treating the test output as final. -
`Day2_pre_fee_balance_is_negative_370_when_E7_is_applied_by_value_date`
failed because the assertion was including the AED 25 overdraft fee,
producing `-395.00` instead of the required pre-fee `-370.00`. -
`Ledger_is_append_only_and_E7_is_not_mutated_by_E9` failed with
`Sequence contains no matching element` because the reversal handling
incorrectly conflated the original E7 event with the fee generated from
E7. - `E5_accepts_AuthA_settlement_for_185` failed with an actual
balance of `-180.00` instead of `440.00`, as a consequence of E9 not
being processed correctly. -
`E9_reverses_E7_but_the_fee_remains_and_final_Day2_balance_is_225`
failed because the expected reversal entry was not produced while the
reversal was being incorrectly rejected. -
`Interest_uses_rounded_daily_accruals_and_capitalizes_their_exact_sum`
failed as a cascading consequence of E9 not being processed correctly,
causing later historical balances and interest accruals to be wrong. -
`Intentionally_failing_test_documents_rejected_criterion_6` failed
deliberately: the test expects the fee to disappear after E9, but the
append-only design correctly retains the separately booked overdraft
fee. This failure documents the rejected acceptance criterion rather
than a defect. - Corrected the reversal model by distinguishing
`ReversalOfEntryId` from `SourceEventId`, so E9 explicitly reverses E7
without reversing the fee caused by E7. - Corrected the Day-2 pre-fee
test to evaluate the balance before the fee entry. - Rechecked the
affected settlement, reversal and interest calculations.

**23:30--23:59** - Created the Architecture & Trade-offs document. -
Corrected and updated supporting Markdown documentation. - Added the
documented ambiguity concerning the ordering of overdraft fees and
interest calculation. - Updated the work log and other repository
documentation. - Pushed the latest changes to the repository.

## Test/debugging record

The first complete local xUnit run during the correction cycle produced
the following result:

``` text
Total tests: 16
Passed: 10
Failed: 6
```

The six failures were investigated individually as recorded above. Five
represented implementation/test defects that were corrected; the sixth
was the deliberately failing test documenting the rejected criterion
concerning reversal of the overdraft fee.

The scenario was subsequently checked independently against the expected
accounting arithmetic, including the E7 transient Day-2 balance, the
retained overdraft fee, the E9 reversal, BHD installment allocation, and
rounded daily interest reconciliation.
