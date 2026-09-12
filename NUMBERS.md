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
