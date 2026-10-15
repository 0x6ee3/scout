using Scout.Web.Domain;
using Scout.Web.Features;
using Scout.Web.Tests.Data;

namespace Scout.Web.Tests.Positions;

public sealed class CatalogTests
{
    [Fact]
    public async Task CompanySearchIsLiteralUnicodeAwareAndCombinedWithArchive()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var a = await db.Companies.CreateAsync(new()
        {
            Name = "zebra",
            Location = "münchen",
            Description = "Not searchable"
        });
        var b = await db.Companies.CreateAsync(new()
        {
            Name = "Alpha%_",
            Location = "Berlin"
        });
        var c = await db.Companies.CreateAsync(new()
        {
            Name = "alpha",
            Location = "München"
        });
        await db.Companies.SetArchivedAsync(c, true);
        Assert.Equal(new[] { b, a }, (await db.Companies.ListAsync()).Select(x => x.Id));
        Assert.Equal(new[] { c, b, a }, (await db.Companies.ListAsync(ArchiveFilter.All)).Select(x => x.Id));
        Assert.Equal(a, Assert.Single(await db.Companies.ListAsync(search: " MÜNCHEN ")).Id);
        Assert.Equal(c, Assert.Single(await db.Companies.ListAsync(ArchiveFilter.Archived, "MÜNCHEN")).Id);
        Assert.Equal(b, Assert.Single(await db.Companies.ListAsync(search: "%_")).Id);
        Assert.Empty(await db.Companies.ListAsync(search: "Not searchable"));
        Assert.Empty(await db.Companies.ListAsync(search: "Munchen"));
        Assert.Empty(await db.Companies.ListAsync(ArchiveFilter.Archived, "Berlin"));
    }

    [Fact]
    public async Task PositionSearchCombinesAllFiltersAndUsesStableDiscoveryOrder()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var a = await db.Companies.CreateAsync(new()
        {
            Name = "München GmbH"
        });
        var b = await db.Companies.CreateAsync(new()
        {
            Name = "Other"
        });
        var older = await db.Positions.CreateAsync(new()
        {
            CompanyId = a,
            Title = "alpha",
            DiscoveredOn = new(2026, 1, 1)
        });
        var first = await db.Positions.CreateAsync(new()
        {
            CompanyId = a,
            Title = "Alpha%_",
            Status = PositionStatus.Interested,
            DiscoveredOn = new(2026, 2, 1),
            JobDescriptionSnapshot = "Not searchable"
        });
        var second = await db.Positions.CreateAsync(new()
        {
            CompanyId = a,
            Title = "alpha%_",
            Status = PositionStatus.Interested,
            DiscoveredOn = new(2026, 2, 1)
        });
        var archived = await db.Positions.CreateAsync(new()
        {
            CompanyId = a,
            Title = "zebra",
            Status = PositionStatus.Expired,
            DiscoveredOn = new(2026, 2, 1)
        });
        var other = await db.Positions.CreateAsync(new()
        {
            CompanyId = b,
            Title = "Other",
            Location = "münchen",
            Status = PositionStatus.Interested,
            DiscoveredOn = new(2026, 3, 1)
        });
        await db.Positions.SetArchivedAsync(archived, true);
        await db.Companies.SetArchivedAsync(a, true);
        Assert.Equal(new[] { other, first, second, older }, (await db.Positions.ListAsync()).Select(x => x.Id));
        Assert.Equal(new[] { first, second, older }, (await db.Positions.ListAsync(a, search: "MÜNCHEN")).Select(x => x.Id));
        Assert.Equal(new[] { first, second }, (await db.Positions.ListAsync(a, ArchiveFilter.Active, PositionStatus.Interested, "%_")).Select(x => x.Id));
        Assert.Equal(archived, Assert.Single(await db.Positions.ListAsync(a, ArchiveFilter.Archived, PositionStatus.Expired, "zebra")).Id);
        Assert.Equal(5, (await db.Positions.ListAsync(archive: ArchiveFilter.All, search: "MÜNCHEN")).Count);
        Assert.Empty(await db.Positions.ListAsync(a, ArchiveFilter.Active, PositionStatus.Expired));
        Assert.Empty(await db.Positions.ListAsync(search: "Not searchable"));
        Assert.Empty(await db.Positions.ListAsync(999));
        Assert.Empty(await db.Positions.ListAsync(search: "no match"));
    }

    [Fact]
    public async Task EmptyCatalogsReturnEmptyResults()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        Assert.Empty(await db.Companies.ListAsync(ArchiveFilter.All, "%"));
        Assert.Empty(await db.Positions.ListAsync(archive: ArchiveFilter.All));
    }

    [Theory]
    [InlineData("https://example.com", "/positions")]
    [InlineData("//example.com", "/positions")]
    [InlineData("?q=M%C3%9CNCHEN&archive=All", "/positions?q=M%C3%9CNCHEN&archive=All")]
    public void ReturnLinksStayWithinTheirCatalog(string query, string expected) =>
        Assert.Equal(expected, CatalogLinks.List("/positions", query));
}
