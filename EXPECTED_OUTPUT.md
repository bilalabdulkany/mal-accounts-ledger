<<<<<<< HEAD
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


## Account opening balances

The runner prints the opening balance once in each account section:
- ACC-001: AED 0.00
- ACC-002: BHD 0.000
=======
# Expected Scenario Results

The following values are the expected results of the supplied stream under the documented interpretation.

| Account | Day | Closing ledger balance | Fees | Errors | Daily interest |
|---|---:|---:|---|---|---:|
| ACC-001 AED | 1 | 250.00 | none | none | 0.10 |
| ACC-001 AED | 2 | 225.00 | 25.00 (trigger E7) | none | 0.09 |
| ACC-001 AED | 3 | 625.00 | none | none | 0.25 |
| ACC-001 AED | 4 | 440.00 | none | E6 UNKNOWN_AUTHORIZATION | 0.18 |
| ACC-001 AED | 5 | 440.00 | none | E8 AUTHORIZATION_DECLINED | 0.18 |
| ACC-001 AED | 6 | 1061.22 | none | none | 0.42 |
| ACC-002 BHD | 1 | 0.000 | none | none | 0.000 |
| ACC-002 BHD | 2 | 0.000 | none | none | 0.000 |
| ACC-002 BHD | 3 | 0.000 | none | none | 0.000 |
| ACC-002 BHD | 4 | 0.000 | none | none | 0.000 |
| ACC-002 BHD | 5 | 10.000 | none | none | 0.004 |
| ACC-002 BHD | 6 | 10.008 | none | none | 0.004 |

## Authorization states

- Day 2: Auth-A APPROVED.
- Day 3: Auth-A APPROVED.
- Day 4: Auth-A SETTLED.
- Day 5: Auth-A SETTLED; Auth-B REJECTED.
- Day 6: Auth-A SETTLED; Auth-B REJECTED.
- Auth-Z is never an authorization; E6 is represented as an UNKNOWN_AUTHORIZATION error.

## Interest capitalization

- ACC-001: AED 1.22 on Day 6.
- ACC-002: BHD 0.008 on Day 6.
>>>>>>> origin/main
