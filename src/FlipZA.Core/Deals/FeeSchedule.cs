namespace FlipZA.Core.Deals;

/// <summary>
/// What the marketplace charges per unit sold. Amounts exclude VAT; VAT is added on top.
/// </summary>
public class FeeSchedule
{
    /// <summary>Success fee (commission) as a fraction of the selling price, when no override matches.</summary>
    public decimal DefaultSuccessFeeRate { get; set; } = 0.15m;

    /// <summary>Overrides keyed by department name, e.g. "Computers &amp; Tablets".</summary>
    public Dictionary<string, decimal> SuccessFeeRatesByDepartment { get; set; } = new();

    /// <summary>Overrides keyed by leaf category id; take precedence over department rates.</summary>
    public Dictionary<int, decimal> SuccessFeeRatesByCategory { get; set; } = new();

    /// <summary>Flat per-unit fulfilment fee, excluding VAT.</summary>
    public decimal FulfilmentFee { get; set; } = 38m;

    public decimal VatRate { get; set; } = 0.15m;

    public decimal SuccessFeeRateFor(string? department, int? leafCategoryId)
    {
        if (leafCategoryId is { } category && SuccessFeeRatesByCategory.TryGetValue(category, out var categoryRate))
            return categoryRate;

        if (department is not null && SuccessFeeRatesByDepartment.TryGetValue(department, out var departmentRate))
            return departmentRate;

        return DefaultSuccessFeeRate;
    }
}
