namespace FlipZA.Core.Deals;

/// <summary>
/// Per-unit profit maths for reselling on a marketplace.
/// <para>
/// Not VAT-registered: fees cost their VAT-inclusive amount, stock and shipping cost what was paid,
/// and there is no output VAT. VAT-registered: output VAT (rate/(1+rate) of the price) is owed on each
/// sale, while VAT on fees, inbound shipping and (if the supplier charges it) stock is claimed back.
/// </para>
/// Amounts are kept at full precision and rounded to cents only in the returned results.
/// </summary>
public static class DealCalculator
{
    public static SaleProceeds CalculateProceeds(
        decimal sellPrice, string? department, int? leafCategoryId, FeeSchedule fees, ResellerProfile reseller)
    {
        var rate = fees.SuccessFeeRateFor(department, leafCategoryId);
        var successFee = sellPrice * rate;
        var feesExVat = successFee + fees.FulfilmentFee;
        var feeVat = feesExVat * fees.VatRate;

        var outputVat = reseller.IsVatRegistered ? VatPortion(sellPrice, fees.VatRate) : 0m;
        var feeCost = reseller.IsVatRegistered ? feesExVat : feesExVat + feeVat;

        return new SaleProceeds(
            SellPrice: Money(sellPrice),
            SuccessFeeRate: rate,
            SuccessFee: Money(successFee),
            FulfilmentFee: Money(fees.FulfilmentFee),
            FeeVat: Money(feeVat),
            OutputVat: Money(outputVat),
            NetProceeds: Money(sellPrice - outputVat - feeCost));
    }

    public static DealEvaluation Evaluate(
        decimal sellPrice, decimal unitCost, string? department, int? leafCategoryId,
        FeeSchedule fees, ResellerProfile reseller, DealGate gate)
    {
        var proceeds = CalculateProceeds(sellPrice, department, leafCategoryId, fees, reseller);
        var netProceeds = NetProceedsExact(sellPrice, department, leafCategoryId, fees, reseller);
        var landedCost = EffectiveUnitCost(unitCost, fees, reseller) + EffectiveShipping(fees, reseller);

        var netProfit = netProceeds - landedCost;
        decimal? roi = landedCost > 0 ? netProfit / landedCost : null;
        var passes = netProfit >= gate.MinNetProfit && roi >= gate.MinRoi;

        return new DealEvaluation(
            proceeds,
            LandedCost: Money(landedCost),
            NetProfit: Money(netProfit),
            Roi: roi is null ? null : Math.Round(roi.Value, 4, MidpointRounding.AwayFromZero),
            PassesGate: passes);
    }

    /// <summary>
    /// Highest unit cost (as the supplier quotes it) at which a deal still passes the gate,
    /// or null when no positive cost would. Rounded down to the cent so it never overshoots.
    /// </summary>
    public static decimal? MaxBuyPrice(
        decimal sellPrice, string? department, int? leafCategoryId,
        FeeSchedule fees, ResellerProfile reseller, DealGate gate)
    {
        var netProceeds = NetProceedsExact(sellPrice, department, leafCategoryId, fees, reseller);

        // Profit = N - L must satisfy both N - L >= MinNetProfit and N - L >= MinRoi * L.
        var maxLandedCost = Math.Min(netProceeds - gate.MinNetProfit, netProceeds / (1 + gate.MinRoi));
        var maxEffectiveCost = maxLandedCost - EffectiveShipping(fees, reseller);
        if (maxEffectiveCost <= 0)
            return null;

        var maxUnitCost = ClaimsStockVat(reseller) ? maxEffectiveCost * (1 + fees.VatRate) : maxEffectiveCost;
        return Math.Floor(maxUnitCost * 100) / 100;
    }

    private static decimal NetProceedsExact(
        decimal sellPrice, string? department, int? leafCategoryId, FeeSchedule fees, ResellerProfile reseller)
    {
        var feesExVat = sellPrice * fees.SuccessFeeRateFor(department, leafCategoryId) + fees.FulfilmentFee;
        return reseller.IsVatRegistered
            ? sellPrice - VatPortion(sellPrice, fees.VatRate) - feesExVat
            : sellPrice - feesExVat * (1 + fees.VatRate);
    }

    private static decimal EffectiveUnitCost(decimal unitCost, FeeSchedule fees, ResellerProfile reseller) =>
        ClaimsStockVat(reseller) ? unitCost - VatPortion(unitCost, fees.VatRate) : unitCost;

    private static decimal EffectiveShipping(FeeSchedule fees, ResellerProfile reseller) =>
        reseller.IsVatRegistered
            ? reseller.InboundShippingPerUnit - VatPortion(reseller.InboundShippingPerUnit, fees.VatRate)
            : reseller.InboundShippingPerUnit;

    private static bool ClaimsStockVat(ResellerProfile reseller) =>
        reseller.IsVatRegistered && reseller.SupplierChargesVat;

    /// <summary>The VAT contained in a VAT-inclusive amount.</summary>
    private static decimal VatPortion(decimal inclusiveAmount, decimal vatRate) =>
        inclusiveAmount * vatRate / (1 + vatRate);

    private static decimal Money(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
