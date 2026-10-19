namespace FlipZA.Core.Deals;

/// <summary>
/// What selling one unit at a price earns after fees and VAT. Independent of what the stock cost.
/// </summary>
/// <param name="NetProceeds">What the reseller keeps from the sale after fees and VAT, before stock costs.</param>
public sealed record SaleProceeds(
    decimal SellPrice,
    decimal SuccessFeeRate,
    decimal SuccessFee,
    decimal FulfilmentFee,
    decimal FeeVat,
    decimal OutputVat,
    decimal NetProceeds);

/// <summary>Full profit breakdown for buying one unit at a cost and selling it at a price.</summary>
/// <param name="LandedCost">Unit cost plus inbound shipping, net of any VAT the reseller can claim back.</param>
/// <param name="Roi">Net profit as a fraction of landed cost; null when landed cost is zero.</param>
public sealed record DealEvaluation(
    SaleProceeds Proceeds,
    decimal LandedCost,
    decimal NetProfit,
    decimal? Roi,
    bool PassesGate);
