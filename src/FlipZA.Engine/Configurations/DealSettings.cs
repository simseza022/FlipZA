using FlipZA.Core.Deals;

namespace FlipZA.Engine.Configurations;

public class DealSettings
{
    public const string SectionName = "Deals";

    public FeeSchedule Fees { get; set; } = new();
    public ResellerProfile Reseller { get; set; } = new();
    public DealGate Gate { get; set; } = new();
}
