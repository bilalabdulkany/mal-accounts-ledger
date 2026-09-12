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
