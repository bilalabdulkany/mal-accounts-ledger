from decimal import Decimal, ROUND_HALF_UP

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
