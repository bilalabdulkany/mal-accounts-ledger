from decimal import Decimal, ROUND_HALF_UP

<<<<<<< HEAD
AED = Decimal("0.01")
BHD = Decimal("0.001")
RATE = Decimal("0.0004")

# Complete value-date replay before interest capitalization.
aed = {
    1: Decimal("250.00"),
    2: Decimal("225.00"),
    3: Decimal("625.00"),
    4: Decimal("440.00"),
    5: Decimal("440.00"),
    6: Decimal("440.00"),
}

expected_aed = [Decimal(x) for x in ("0.10", "0.09", "0.25", "0.18", "0.18", "0.18")]
actual_aed = [(aed[d] * RATE).quantize(AED, rounding=ROUND_HALF_UP) for d in range(1, 7)]
assert actual_aed == expected_aed
assert sum(actual_aed, Decimal("0")) == Decimal("0.98")

bhd = {1: Decimal("0"), 2: Decimal("0"), 3: Decimal("0"), 4: Decimal("0"), 5: Decimal("10.000"), 6: Decimal("10.000")}
expected_bhd = [Decimal(x) for x in ("0.000", "0.000", "0.000", "0.000", "0.004", "0.004")]
actual_bhd = [(bhd[d] * RATE).quantize(BHD, rounding=ROUND_HALF_UP) for d in range(1, 7)]
assert actual_bhd == expected_bhd
assert sum(actual_bhd, Decimal("0")) == Decimal("0.008")

assert Decimal("250.00") - Decimal("620.00") == Decimal("-370.00")
assert Decimal("-370.00") - Decimal("25.00") + Decimal("620.00") == Decimal("225.00")
assert Decimal("3.333") + Decimal("3.333") + Decimal("3.334") == Decimal("10.000")

print("REFERENCE SCENARIO CHECK: PASS")
print("AED daily accruals:", ", ".join(f"{x:.2f}" for x in actual_aed), "total=0.98")
print("BHD daily accruals:", ", ".join(f"{x:.3f}" for x in actual_bhd), "total=0.008")
print("BHD installments: 3.333, 3.333, 3.334 total=10.000")
print("E7 transient Day-2 balance before fee: -370.00")
print("Final Day-2 balance after fee and E9: 225.00")
=======
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
>>>>>>> origin/main
