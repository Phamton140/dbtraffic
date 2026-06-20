using Microsoft.Playwright;

namespace DbTraffic.E2ETests;

[Collection("E2E collection")]
public class HomePageTests
{
    private readonly DbTrafficWebApplicationFactory _factory;

    public HomePageTests(DbTrafficWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HomePage_Loads_And_Shows_Title()
    {
        if (!_factory.IsAvailable)
        {
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();

        await page.GotoAsync(_factory.BaseUrl);
        await page.WaitForSelectorAsync("h1");

        var title = await page.InnerTextAsync("h1");
        Assert.Contains("DbTraffic", title);
    }

    [Fact]
    public async Task Navigation_Links_Work()
    {
        if (!_factory.IsAvailable)
        {
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();

        await page.GotoAsync(_factory.BaseUrl);
        await page.ClickAsync("a[href='instances']");
        await page.WaitForURLAsync("**/instances");

        var heading = await page.InnerTextAsync("h1");
        Assert.Contains("Instancias", heading);
    }
}
