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

AED daily interest bases are 250.00, 225.00, 625.00, 440.00, 440.00, 1060.00. Rounded accruals are 0.10, 0.09, 0.25, 0.18, 0.18, 0.42; total 1.22.

BHD E10 is allocated as 3.333, 3.333, 3.334 so the stored installments sum exactly to 10.000. BHD daily interest is 0.004 on Days 5 and 6, total 0.008.
