<<<<<<< HEAD
# NUMBERS

| Constant | Value | Reason |
|---|---:|---|
| Window | Days 1-6 | Supplied exercise window. |
| Overdraft fee | AED 25.00 | Supplied rule; not halved because no alternative is specified. |
| Daily interest rate | 0.0004 | 0.04% expressed as a decimal rate. |
| AED precision | 2 | Supplied currency precision. |
| BHD precision | 3 | Supplied currency precision. |
| ACC-001 opening balance | AED 0.00 | Supplied. |
| ACC-002 opening balance | BHD 0.000 | Supplied. |
| Auth-A hold | AED 200.00 | Supplied. |
| Auth-A settlement | AED 185.00 | Supplied. |
| Auth-Z settlement | AED 180.00 | Supplied invalid settlement. |
| E7 debit | AED 620.00 | Supplied. |
| Auth-B hold | AED 90.00 | Supplied. |
| E10 total | BHD 10.000 | Supplied. |
| E10 installments | 3 | Supplied. |

## Derived values

E7 creates a transient Day-2 pre-fee balance of `250.00 - 620.00 = -370.00`, so the fee is AED 25.00.

E9 adds back AED 620.00 at value Day 2, but the fee remains append-only. Final Day-2 balance is `250.00 - 620.00 - 25.00 + 620.00 = 225.00`.

AED daily interest bases are 250.00, 225.00, 625.00, 440.00, 440.00, 440.00. Rounded accruals are 0.10, 0.09, 0.25, 0.18, 0.18, 0.18; total 0.98.

BHD E10 is allocated as 3.333, 3.333, 3.334 so the stored installments sum exactly to 10.000. BHD daily interest is 0.004 on Days 5 and 6, total 0.008.
=======
# NUMBERS.md

Every non-trivial constant used by the scenario is recorded here.

| Constant | Value | Reason |
|---|---:|---|
| Scenario days | 6 | The supplied window is Day 1 through Day 6. |
| AED scale | 2 | Required by the specification. |
| BHD scale | 3 | Required by the specification. |
| Overdraft fee | AED 25.00 | Supplied by the specification; not inferred. |
| Daily interest rate | 0.0004 | 0.04% expressed as a decimal: 0.04 / 100. |
| ACC-001 opening balance | AED 0.00 | Supplied by the specification. |
| ACC-002 opening balance | BHD 0.000 | Supplied by the specification. |
| E1 credit | AED 1,200.00 | Supplied event. |
| E2 debit | AED 950.00 | Supplied event. |
| Auth-A hold | AED 200.00 | Supplied event. |
| E4 credit | AED 400.00 | Supplied event. |
| Auth-A settlement | AED 185.00 | Supplied event. |
| Auth-Z settlement | AED 180.00 | Supplied event; rejected because no auth exists. |
| E7 debit | AED 620.00 | Supplied event. |
| Auth-B hold | AED 90.00 | Supplied event. |
| E10 total | BHD 10.000 | Supplied event. |
| E10 installment count | 3 | Supplied event. |

## Why not half the value?

The constants above are either directly prescribed by the exercise or mathematically derived from prescribed units. There is no discretionary alternative that should be silently chosen. For example, the interest rate is 0.04%, so using half (0.02%) would simply violate the supplied rule.

The installment quantum is BHD 0.001, determined by BHD's three-decimal scale. Because 10.000 / 3 is repeating, one BHD 0.001 remainder must be allocated deterministically rather than discarded.
>>>>>>> origin/main
