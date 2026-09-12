# Expected scenario output

The runner's important values after complete replay are:

| Account | Day | Closing ledger balance | Fee | Daily interest | Authorization states | Errors |
|---|---:|---:|---:|---:|---|---|
| ACC-001 | 1 | AED 250.00 | AED 0.00 | AED 0.10 | - | - |
| ACC-001 | 2 | AED 225.00 | AED 25.00 | AED 0.09 | Auth-A=Approved | - |
| ACC-001 | 3 | AED 625.00 | AED 0.00 | AED 0.25 | Auth-A=Approved | - |
| ACC-001 | 4 | AED 440.00 | AED 0.00 | AED 0.18 | Auth-A=Settled | E6 UNKNOWN_AUTHORIZATION |
| ACC-001 | 5 | AED 440.00 | AED 0.00 | AED 0.18 | Auth-A=Settled, Auth-B=Rejected | E8 AUTHORIZATION_DECLINED |
| ACC-001 | 6 | AED 440.98 | AED 0.00 | AED 0.18 | Auth-A=Settled, Auth-B=Rejected | - |
| ACC-002 | 1 | BHD 0.000 | BHD 0.000 | BHD 0.000 | - | - |
| ACC-002 | 2 | BHD 0.000 | BHD 0.000 | BHD 0.000 | - | - |
| ACC-002 | 3 | BHD 0.000 | BHD 0.000 | BHD 0.000 | - | - |
| ACC-002 | 4 | BHD 0.000 | BHD 0.000 | BHD 0.000 | - | - |
| ACC-002 | 5 | BHD 10.000 | BHD 0.000 | BHD 0.004 | - | - |
| ACC-002 | 6 | BHD 10.008 | BHD 0.000 | BHD 0.004 | - | - |

The Day-2 `AED -370.00` acceptance criterion is the **transient pre-fee balance when E7 is processed**, not the final post-replay Day-2 snapshot. E7 arrives on Day 5 but has Day-2 value date; E9 later adds AED 620 back at Day 2 value date.

The Day-6 AED closing balance includes the single AED 0.98 interest-capitalization entry. The daily interest base for Day 6 is AED 440.00 before that capitalization.
