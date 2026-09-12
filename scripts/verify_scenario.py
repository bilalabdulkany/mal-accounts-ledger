from decimal import Decimal, ROUND_HALF_UP

Q2 = Decimal("0.01")
Q3 = Decimal("0.001")
RATE = Decimal("0.0004")

# Independent arithmetic check of the supplied scenario.
# Day 1: E1 + E2.
day1 = Decimal("1200.00") - Decimal("950.00")
assert day1 == Decimal("250.00")

# E7 is booked on Day 5 but carries Day-2 value date.
day2_pre_fee = day1 - Decimal("620.00")
assert day2_pre_fee == Decimal("-370.00")

# The supplied criterion says exactly one Day-2 fee. E9 later reverses E7.
day2 = day2_pre_fee - Decimal("25.00") + Decimal("620.00")
assert day2 == Decimal("225.00")

# E4 is Day-3 value-dated; E5 is Day-4 value-dated; E9 is Day-2 value-dated.
final_balances = [
    day1,
    day2,
    day2 + Decimal("400.00"),
    day2 + Decimal("400.00") - Decimal("185.00"),
    day2 + Decimal("400.00") - Decimal("185.00"),
    day2 + Decimal("400.00") - Decimal("185.00") + Decimal("620.00"),
]
assert final_balances == [
    Decimal("250.00"), Decimal("225.00"), Decimal("625.00"),
    Decimal("440.00"), Decimal("440.00"), Decimal("1060.00")
]

# Interest is calculated from those final historical balances before Day-6 capitalization.
def interest(balance: Decimal, quantum: Decimal) -> Decimal:
    raw = balance * RATE if balance > 0 else Decimal("0")
    return raw.quantize(quantum, rounding=ROUND_HALF_UP)

aed_interest = [interest(x, Q2) for x in final_balances]
assert aed_interest == [
    Decimal("0.10"), Decimal("0.09"), Decimal("0.25"),
    Decimal("0.18"), Decimal("0.18"), Decimal("0.42")
]
assert sum(aed_interest, Decimal("0")) == Decimal("1.22")

bhd_installments = [Decimal("3.333"), Decimal("3.333"), Decimal("3.334")]
assert sum(bhd_installments, Decimal("0")) == Decimal("10.000")

bhd_balances = [Decimal("0.000"), Decimal("0.000"), Decimal("0.000"), Decimal("0.000"), Decimal("10.000"), Decimal("10.000")]
bhd_interest = [interest(x, Q3) for x in bhd_balances]
assert bhd_interest == [Decimal("0.000"), Decimal("0.000"), Decimal("0.000"), Decimal("0.000"), Decimal("0.004"), Decimal("0.004")]
assert sum(bhd_interest, Decimal("0")) == Decimal("0.008")

print("REFERENCE SCENARIO CHECK: PASS")
print("E7 Day-2 pre-fee balance: AED -370.00")
print("Final AED daily balances before Day-6 interest capitalization: 250.00, 225.00, 625.00, 440.00, 440.00, 1060.00")
print("AED daily interest: 0.10, 0.09, 0.25, 0.18, 0.18, 0.42; total 1.22")
print("BHD installments: 3.333, 3.333, 3.334; total 10.000")
print("BHD daily interest: 0.000, 0.000, 0.000, 0.000, 0.004, 0.004; total 0.008")
