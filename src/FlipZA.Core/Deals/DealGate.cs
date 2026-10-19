namespace FlipZA.Core.Deals;

/// <summary>Minimum returns for a deal to be recommended. Both must be met.</summary>
public class DealGate
{
    public decimal MinNetProfit { get; set; } = 80m;

    /// <summary>Net profit as a fraction of landed cost, e.g. 0.25 = 25%.</summary>
    public decimal MinRoi { get; set; } = 0.25m;
}
