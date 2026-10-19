using FlipZA.Core.Deals;

namespace FlipZA.Tests.Deals;

public class DealCalculatorTests
{
    private static readonly FeeSchedule Fees = new(); // 15% success fee, R38 fulfilment, 15% VAT
    private static readonly DealGate Gate = new();     // R80 profit and 25% ROI

    private static readonly ResellerProfile NotRegistered = new() { IsVatRegistered = false };
    private static readonly ResellerProfile Registered = new() { IsVatRegistered = true, SupplierChargesVat = true };

    [Fact]
    public void Evaluate_NotVatRegistered_PaysVatInclusiveFees()
    {
        var deal = DealCalculator.Evaluate(499m, 250m, null, null, Fees, NotRegistered, Gate);

        Assert.Equal(74.85m, deal.Proceeds.SuccessFee);   // 15% of 499
        Assert.Equal(38m, deal.Proceeds.FulfilmentFee);
        Assert.Equal(16.93m, deal.Proceeds.FeeVat);       // 15% of 112.85
        Assert.Equal(0m, deal.Proceeds.OutputVat);
        Assert.Equal(369.22m, deal.Proceeds.NetProceeds); // 499 - 129.7775
        Assert.Equal(250m, deal.LandedCost);
        Assert.Equal(119.22m, deal.NetProfit);
        Assert.Equal(0.4769m, deal.Roi);
        Assert.True(deal.PassesGate);
    }

    [Fact]
    public void Evaluate_VatRegistered_OwesOutputVat_ButClaimsInputVat()
    {
        var deal = DealCalculator.Evaluate(499m, 250m, null, null, Fees, Registered, Gate);

        Assert.Equal(65.09m, deal.Proceeds.OutputVat);    // 499 x 15/115
        Assert.Equal(321.06m, deal.Proceeds.NetProceeds); // 499 - 65.087 - 112.85 (fee VAT claimed back)
        Assert.Equal(217.39m, deal.LandedCost);           // 250 / 1.15
        Assert.Equal(103.67m, deal.NetProfit);
        Assert.Equal(0.4769m, deal.Roi);                  // same ROI as unregistered when VAT flows through
        Assert.True(deal.PassesGate);
    }

    [Fact]
    public void Evaluate_VatRegistered_SupplierWithoutVat_HasNoInputVatToClaim()
    {
        var reseller = new ResellerProfile { IsVatRegistered = true, SupplierChargesVat = false };

        var deal = DealCalculator.Evaluate(499m, 250m, null, null, Fees, reseller, Gate);

        Assert.Equal(250m, deal.LandedCost);
        Assert.Equal(71.06m, deal.NetProfit); // 321.063 - 250
        Assert.False(deal.PassesGate);        // under the R80 minimum
    }

    [Fact]
    public void Evaluate_InboundShipping_CountsTowardsLandedCost()
    {
        var reseller = new ResellerProfile { IsVatRegistered = false, InboundShippingPerUnit = 20m };

        var deal = DealCalculator.Evaluate(499m, 250m, null, null, Fees, reseller, Gate);

        Assert.Equal(270m, deal.LandedCost);
        Assert.Equal(99.22m, deal.NetProfit);
        Assert.Equal(0.3675m, deal.Roi); // 99.2225 / 270
    }

    [Fact]
    public void Evaluate_FailsGate_WhenRoiTooLow_EvenWithEnoughProfit()
    {
        // R2,000 sale, R1,450 cost: R118 profit clears R80, but 8% ROI is far below 25%.
        var deal = DealCalculator.Evaluate(2000m, 1450m, null, null, Fees, NotRegistered, Gate);

        Assert.True(deal.NetProfit >= Gate.MinNetProfit);
        Assert.True(deal.Roi < Gate.MinRoi);
        Assert.False(deal.PassesGate);
    }

    [Fact]
    public void SuccessFeeRate_CategoryOverride_BeatsDepartment_BeatsDefault()
    {
        var fees = new FeeSchedule
        {
            SuccessFeeRatesByDepartment = { ["Computers & Tablets"] = 0.10m },
            SuccessFeeRatesByCategory = { [12345] = 0.08m }
        };

        Assert.Equal(0.08m, fees.SuccessFeeRateFor("Computers & Tablets", 12345));
        Assert.Equal(0.10m, fees.SuccessFeeRateFor("Computers & Tablets", 99999));
        Assert.Equal(0.15m, fees.SuccessFeeRateFor("Gaming", null));
    }

    [Theory]
    [InlineData(false, 289.22)] // min(369.2225 - 80, 369.2225 / 1.25) = 289.2225
    [InlineData(true, 277.22)]  // min(321.063 - 80, 321.063 / 1.25) = 241.063, x 1.15 back to a VAT-inclusive quote
    public void MaxBuyPrice_IsHighestCostThatStillPassesGate(bool vatRegistered, double expected)
    {
        var reseller = new ResellerProfile { IsVatRegistered = vatRegistered, SupplierChargesVat = true };

        var max = DealCalculator.MaxBuyPrice(499m, null, null, Fees, reseller, Gate);

        Assert.Equal((decimal)expected, max);
        Assert.True(DealCalculator.Evaluate(499m, max!.Value, null, null, Fees, reseller, Gate).PassesGate);
        Assert.False(DealCalculator.Evaluate(499m, max.Value + 0.05m, null, null, Fees, reseller, Gate).PassesGate);
    }

    [Fact]
    public void MaxBuyPrice_IsNull_WhenNoPositiveCostCanPass()
    {
        // A R140 sale nets R72.15 after fees (140 - 59 x 1.15): below the R80 minimum before any stock cost.
        Assert.Null(DealCalculator.MaxBuyPrice(140m, null, null, Fees, NotRegistered, Gate));
    }
}
