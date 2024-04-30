using Microsoft.Playwright;

// Just save this in the same folder as the executable for now
const string statePath = "state.json";

using var playwright = await Playwright.CreateAsync();

await using var browser = await playwright.Chromium.LaunchAsync();

IBrowserContext ctx;

if (!File.Exists(statePath))
{
    ctx = await browser.NewContextAsync();

    var login = await ctx.NewPageAsync();

    await login.GotoAsync("https://en.boardgamearena.com/account?warn&redirect=gameinprogress");

    await login.Locator("#normalconnect_content [name=email]").FillAsync(args[0]);
    await login.Locator("#normalconnect_content [name=password]").FillAsync(args[1]);

    await login.Locator("#normalconnect_content #submit_login_button").ClickAsync();

    await login.WaitForURLAsync("https://boardgamearena.com/gameinprogress");

    await ctx.StorageStateAsync(new() { Path = statePath });

    await login.CloseAsync();

    Console.WriteLine("Logged in, new session created");
}
else
{
    ctx = await browser.NewContextAsync(new() { StorageStatePath = statePath });

    Console.WriteLine("Using existing session");
}

var page = await ctx.NewPageAsync();

try
{
    await page.GotoAsync("https://boardgamearena.com/gameinprogress");
}
catch (Exception ex)
{
    Console.WriteLine($"Couldn't navigate to game progress dashboard: {ex.Message}");
}

try
{
    var waitingSection = await page.Locator("#section-waiting h1 span[slot=title] + span").TextContentAsync(new() { Timeout = 250 });

    if (!int.TryParse(waitingSection?.Trim('(', ')'), out var waitingGames))
    {
        Console.WriteLine("Couldn't parse count of games waiting");

        return 1;
    }

    Console.WriteLine($"{waitingGames} waiting for you");

    return 0;
}
catch (TimeoutException)
{
    Console.WriteLine("Couldn't find count of games waiting");

    return 1;
}
