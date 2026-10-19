# Deal calculator

The deal calculator answers two questions for a product selling on Takealot:

1. **If I buy one unit at price X and sell it at Takealot's current price, what do I make?** (net profit and ROI)
2. **What is the most I can pay a supplier and still make a worthwhile deal?** (max buy price)

The code is in `src/FlipZA.Core/Deals/`. It is pure maths with no database or network access, and every formula below is covered by tests in `tests/FlipZA.Tests/Deals/DealCalculatorTests.cs`.

---

## The short version

```text
Net proceeds = Sell price − Takealot fees − VAT effects        (what you keep from the sale)
Landed cost  = Unit cost + Inbound shipping − VAT you claim back (what the unit really cost you)
Net profit   = Net proceeds − Landed cost
ROI          = Net profit ÷ Landed cost

A deal passes the gate when  Net profit ≥ R80  AND  ROI ≥ 25%
Max buy price = the highest supplier price that still passes the gate
```

The only tricky part is VAT, which works differently depending on whether the reseller is VAT-registered. Both cases are explained below.

---

## Inputs

| Input | Symbol | Default | Where it comes from |
|---|---|---|---|
| Sell price | **P** | — | Takealot buy-box price (VAT-inclusive, what the customer pays) |
| Success fee rate | **r** | 15% | `Deals:Fees`; can differ per department or category |
| Fulfilment fee | **F** | R38.00 | `Deals:Fees:FulfilmentFee`, excluding VAT |
| VAT rate | **v** | 15% | `Deals:Fees:VatRate` |
| Unit cost | **C** | — | What the supplier charges per unit, as quoted |
| Inbound shipping | **S** | R10.00 | Cost per unit to get stock into Takealot's warehouse, as paid |
| VAT-registered? | | yes | `Deals:Reseller:IsVatRegistered` |
| Supplier charges VAT? | | yes | `Deals:Reseller:SupplierChargesVat` |
| Minimum profit | | R80.00 | `Deals:Gate:MinNetProfit` |
| Minimum ROI | | 25% | `Deals:Gate:MinRoi` |

### Two helper ideas

**Takealot's fees (excluding VAT):**
```text
Success fee   = P × r
Fees ex VAT   = P × r + F
VAT on fees   = (P × r + F) × v
```

**The VAT inside a VAT-inclusive amount** is `amount × v ÷ (1 + v)`, which at 15% is **`amount × 15/115`**. Removing it leaves `amount ÷ 1.15`. For example, R115 includes R15 VAT and R100 before VAT.

---

## Case 1: reseller is NOT VAT-registered

A business that is not registered for VAT (in South Africa, generally turnover under R1 million a year) does not charge VAT on its sales and **cannot claim back** VAT it pays. So every cost is simply what was paid.

```text
Net proceeds  N = P − (P × r + F) × (1 + v)        fees cost their VAT-inclusive amount
Landed cost   L = C + S                            costs are what was paid
```

## Case 2: reseller IS VAT-registered

A VAT-registered business must pay SARS the VAT contained in each sale (**output VAT**), but **claims back** the VAT it paid on its own business costs (**input VAT**): Takealot's fees, inbound shipping and, if the supplier charged VAT, the stock itself.

```text
Output VAT      = P × v ÷ (1 + v)                  e.g. R499 × 15/115 = R65.09
Net proceeds  N = P − Output VAT − (P × r + F)     fees cost their ex-VAT amount (VAT is claimed back)
Landed cost   L = C' + S ÷ (1 + v)                 shipping VAT is claimed back

where  C' = C ÷ (1 + v)   if the supplier charges VAT (its VAT is claimed back)
       C' = C             if the supplier does not charge VAT (nothing to claim)
```

Inbound shipping is assumed to come from a VAT-registered courier, so its VAT is always claimable for a registered reseller.

## Profit, ROI and the gate (both cases)

```text
Net profit = N − L
ROI        = Net profit ÷ L         (empty if L is zero)
Passes     = Net profit ≥ MinNetProfit  AND  ROI ≥ MinRoi
```

ROI is measured against **landed cost** (stock plus inbound shipping, after VAT claims), because that is the cash actually tied up per unit.

---

## Max buy price

The max buy price is the highest supplier price **C** at which the deal still passes the gate. It does not depend on what you actually pay, so FlipZA can compute it for every Takealot listing before any supplier is known.

Both gate conditions limit the landed cost:

```text
Profit rule:  N − L ≥ MinNetProfit      →   L ≤ N − MinNetProfit
ROI rule:     N − L ≥ MinRoi × L        →   L ≤ N ÷ (1 + MinRoi)

Max landed cost  L_max = min( N − 80 ,  N ÷ 1.25 )        with default gate
```

Then convert the landed cost back into a supplier quote:

```text
Max effective unit cost = L_max − effective shipping
                          (S for unregistered, S ÷ 1.15 for registered)

Max buy price = Max effective unit cost × 1.15   if registered and supplier charges VAT
              = Max effective unit cost          otherwise
```

The result is **rounded down** to the cent, so paying exactly the max buy price always passes the gate. If the result would be zero or negative, there is **no** viable supplier price and the field is left empty.

### Which rule limits you?

`N − 80` and `N ÷ 1.25` are equal when **N = R400**:

- **Net proceeds under R400:** the **R80 profit floor** is the limit. Cheap items need a very low buy price.
- **Net proceeds over R400:** the **25% ROI rule** is the limit. You may pay up to 80% of net proceeds.

### The minimum viable selling price

Below a certain selling price, the fees alone leave less than R80, so **no** supplier price can work:

| Reseller | Inbound shipping | Lowest workable sell price |
|---|---|---|
| Not VAT-registered | R0 | **R149.49** |
| VAT-registered (default settings) | R10 | **R176.08** |

This is why discovery ignores products under R200.

---

## Worked examples

Default fees in all examples: 15% success fee, R38 fulfilment, 15% VAT; gate R80 profit and 25% ROI.

### Example 1: R499 sale, R250 cost, not VAT-registered, no shipping

| Line | Calculation | Amount |
|---|---|---|
| Success fee | 499 × 15% | R74.85 |
| Fulfilment fee | | R38.00 |
| VAT on fees | 112.85 × 15% | R16.93 |
| **Net proceeds** | 499 − 129.78 | **R369.22** |
| Landed cost | 250 + 0 | R250.00 |
| **Net profit** | 369.22 − 250 | **R119.22** |
| **ROI** | 119.22 ÷ 250 | **47.69%** |
| Gate | ≥ R80 and ≥ 25% | **Pass** |
| **Max buy price** | min(369.22 − 80, 369.22 ÷ 1.25) = 289.22 | **R289.22** |

### Example 2: same deal, VAT-registered, supplier charges VAT

| Line | Calculation | Amount |
|---|---|---|
| Output VAT | 499 × 15/115 | R65.09 |
| Fees ex VAT | 74.85 + 38 (fee VAT claimed back) | R112.85 |
| **Net proceeds** | 499 − 65.09 − 112.85 | **R321.06** |
| Landed cost | 250 ÷ 1.15 (stock VAT claimed back) | R217.39 |
| **Net profit** | 321.06 − 217.39 | **R103.67** |
| **ROI** | 103.67 ÷ 217.39 | **47.69%** |
| Gate | | **Pass** |
| **Max buy price** | min(241.06, 256.85) = 241.06, × 1.15 | **R277.22** |

The ROI is identical to Example 1. When VAT flows through the whole chain (supplier charges it, reseller claims it), every amount is simply divided by 1.15, so profit shrinks but ROI does not.

### Example 3: VAT-registered, supplier does NOT charge VAT

| Line | Amount |
|---|---|
| Net proceeds (as Example 2) | R321.06 |
| Landed cost (no VAT to claim back) | R250.00 |
| **Net profit** | **R71.06** |
| **ROI** | 28.43% |
| Gate | **Fail**: profit is under R80 |
| Max buy price | R241.06 |

This is the case where VAT registration hurts: output VAT is owed on the sale but there is no input VAT on the stock to offset it. Typical sources: informal or small suppliers and some auction lots.

### Example 4: default settings (VAT-registered, R10 inbound shipping)

| Line | Amount |
|---|---|
| Net proceeds | R321.06 |
| Landed cost (250 ÷ 1.15 + 10 ÷ 1.15) | R226.09 |
| **Net profit** | **R94.98** |
| **ROI** | 42.01% |
| Gate | **Pass** |
| Max buy price | **R267.22** |

### Example 5: cheap gadget, R249 Bluetooth headphones (default settings)

| Line | Amount |
|---|---|
| Net proceeds | R141.17 |
| Max landed cost: min(141.17 − 80, 141.17 ÷ 1.25) | R61.17 (profit floor limits) |
| **Max buy price** | **R60.34** |
| At a R60.00 supplier price: profit / ROI | R80.30 / 131.93%, **Pass** |

Cheap items can pass, but only with a very low buy price, because the R80 floor is a large share of a small sale.

### Example 6: R1,099 air fryer (default settings)

| Line | Amount |
|---|---|
| Net proceeds | R752.80 |
| Max landed cost: min(752.80 − 80, 752.80 ÷ 1.25) | R602.24 (ROI rule limits) |
| **Max buy price** | **R682.57** |
| At a R600.00 supplier price: profit / ROI | R222.37 / 41.92%, **Pass** |

---

## Success fee lookup

Takealot charges different success fees per category. FlipZA picks the rate in this order:

1. `SuccessFeeRatesByCategory[leafCategoryId]`: the most specific category (e.g. `25752` = Air Fryers)
2. `SuccessFeeRatesByDepartment[department]`: e.g. `"Computers & Tablets"`
3. `DefaultSuccessFeeRate` (15%)

Example configuration:

```json
"Fees": {
  "DefaultSuccessFeeRate": 0.15,
  "SuccessFeeRatesByDepartment": { "Computers & Tablets": 0.08 },
  "SuccessFeeRatesByCategory": { "25752": 0.12 }
}
```

Department names and leaf category ids come from each product's `Department` and `LeafCategoryId` columns.

---

## Rounding

- All arithmetic runs at full `decimal` precision.
- Money results are rounded to **2 decimals** (half away from zero) only when returned.
- ROI is rounded to **4 decimals** (e.g. `0.4769` = 47.69%).
- Max buy price is rounded **down** to the cent, so it never overshoots the gate.

---

## Where the results are stored

On every scrape, `ListingPricer` (in the Engine) runs the calculator with the configured `Deals` settings and stores two values on the snapshot:

| Column | Meaning |
|---|---|
| `EstimatedNetProceeds` | Net proceeds **N** at that snapshot's price |
| `MaxBuyPrice` | Max buy price at that snapshot's price; empty if no supplier price can work |

These values are calculated with the settings in force **when the snapshot was taken**. Changing settings later (e.g. turning off VAT registration) affects new snapshots only; old ones are not recalculated.

---

## Assumptions and limitations

Treat results as **estimates**. The calculator does not yet account for:

| Not included | Effect |
|---|---|
| Size- and weight-based fulfilment fees | Takealot charges more for larger items; the flat R38 fits small gadgets best |
| Category-specific success fees (until configured) | The 15% default may over- or under-state fees; check Takealot's current seller fee schedule |
| Storage fees, returns, damaged stock, advertising | Real profit will be lower |
| Winning the buy box | The calculation assumes you sell at today's buy-box price; with many competing offers you may need to undercut |
| Income tax | Profit is before tax |
| Import duties and cross-border shipping | The planned landed-cost model for global suppliers (USD→ZAR plus duties) is not built yet |
| Price changes after you buy | Use the price history to judge how stable a price is |
