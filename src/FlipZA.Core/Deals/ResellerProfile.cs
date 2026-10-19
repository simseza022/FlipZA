namespace FlipZA.Core.Deals;

/// <summary>
/// The reseller's tax position and per-unit costs, which change what a deal is worth to them.
/// </summary>
public class ResellerProfile
{
    /// <summary>
    /// A VAT-registered reseller owes output VAT on each sale but claims back the VAT it pays on
    /// stock (when the supplier charges VAT), inbound shipping and marketplace fees.
    /// </summary>
    public bool IsVatRegistered { get; set; } = true;

    /// <summary>Whether supplier prices include VAT that a registered reseller can claim back.</summary>
    public bool SupplierChargesVat { get; set; } = true;

    /// <summary>Cost per unit to get stock into the marketplace's warehouse, as paid (incl. VAT).</summary>
    public decimal InboundShippingPerUnit { get; set; }
}
