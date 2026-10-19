using FlipZA.Infrastructure.Http;

namespace FlipZA.Tests.Http;

public class RobotsTxtRulesTests
{
    private static readonly RobotsTxtRules TakealotApi =
        RobotsTxtRules.Parse(Fixture.Read("Takealot", "robots-api.txt"), "FlipZA");

    private static readonly RobotsTxtRules TakealotWww =
        RobotsTxtRules.Parse(Fixture.Read("Takealot", "robots-www.txt"), "FlipZA");

    [Theory]
    [InlineData("/rest/v-1-16-0/product-details/PLID98945485?platform=desktop", true)]
    [InlineData("/rest/v-1-16-0/searches/products?qsearch=air%20fryer", true)]
    [InlineData("/rest/v-1-16-0/sponsored-products/abc", false)]
    [InlineData("/rest/v-1-16-0/collect?x=1", false)]
    [InlineData("/rest/v-1-16-0/popups/home", false)]
    public void TakealotApi_AllowsProductDataButNotRestrictedEndpoints(string path, bool expected) =>
        Assert.Equal(expected, TakealotApi.IsAllowed(path));

    [Theory]
    [InlineData("/russell-hobbs-digital-9l-air-fryer-rhaf09s/PLID98945485", true)]
    [InlineData("/account/orders", false)]
    [InlineData("/all?qsearch=air+fryer", false)]
    [InlineData("/all?filter=Price:100-200", false)]
    [InlineData("/all?filter=Brand:Russell+Hobbs", true)]
    public void TakealotWww_FollowsWildcardRules(string path, bool expected) =>
        Assert.Equal(expected, TakealotWww.IsAllowed(path));

    [Fact]
    public void LongestMatchWins_AndTiesGoToAllow()
    {
        var rules = RobotsTxtRules.Parse(
            """
            User-agent: *
            Disallow: /shop/
            Allow: /shop/public/
            Disallow: /tie
            Allow: /tie
            Disallow: /*.pdf$
            """, "FlipZA");

        Assert.False(rules.IsAllowed("/shop/cart"));
        Assert.True(rules.IsAllowed("/shop/public/item"));
        Assert.True(rules.IsAllowed("/tie"));
        Assert.False(rules.IsAllowed("/docs/file.pdf"));
        Assert.True(rules.IsAllowed("/docs/file.pdf?download=1"));
    }

    [Fact]
    public void SpecificAgentGroup_OverridesWildcardGroup()
    {
        var rules = RobotsTxtRules.Parse(
            """
            User-agent: *
            Disallow: /

            User-agent: FlipZA
            Disallow: /private/
            """, "flipza");

        Assert.True(rules.IsAllowed("/products"));
        Assert.False(rules.IsAllowed("/private/x"));
    }
}
