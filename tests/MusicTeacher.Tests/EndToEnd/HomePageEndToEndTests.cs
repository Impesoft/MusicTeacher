using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace MusicTeacher.Tests.EndToEnd;

public sealed class HomePageEndToEndTests : IAsyncLifetime
{
    private readonly int port = Random.Shared.Next(5100, 5900);
    private IPlaywright? playwright;
    private IBrowser? browser;
    private IPage? page;
    private Process? server;
    private string baseUrl = string.Empty;

    [E2EFact]
    public async Task LearnerCanNameTheFirstTrebleClefNote()
    {
        await page!.GotoAsync(baseUrl);
        await page.EvaluateAsync("localStorage.clear()");
        await page.ReloadAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Treble Clef Start" })).ToBeVisibleAsync();
        await page.Locator("button.answer-button", new() { HasTextString = "do" }).ClickAsync();

        await Assertions.Expect(page.GetByText("1 correct")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("1 tries")).ToBeVisibleAsync();
    }

    [E2EFact]
    public async Task LearnerCanOpenPlacementModeAndChooseAStaffPosition()
    {
        await page!.GotoAsync(baseUrl);
        await page.GetByRole(AriaRole.Button, new() { Name = "Place" }).ClickAsync();

        await Assertions.Expect(page.GetByText("Pick a line or space.")).ToBeVisibleAsync();
        await page.Locator(".staff-hit-button").First.ClickAsync();

        await Assertions.Expect(page.Locator(".feedback")).ToContainTextAsync(new Regex("That fits.|Try the next one."));
    }

    public async Task InitializeAsync()
    {
        baseUrl = $"http://127.0.0.1:{port}";
        server = StartServer();
        await WaitForServerAsync();

        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        page = await browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (browser is not null)
        {
            await browser.DisposeAsync();
        }

        playwright?.Dispose();

        if (server is not null && !server.HasExited)
        {
            server.Kill(entireProcessTree: true);
            await server.WaitForExitAsync();
        }
    }

    private Process StartServer()
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "MusicTeacher.WebAssembly",
            "MusicTeacher.WebAssembly.csproj"));

        return Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --no-launch-profile --urls {baseUrl}",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("Could not start Blazor dev server.");
    }

    private async Task WaitForServerAsync()
    {
        using var client = new HttpClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));

        while (!timeout.IsCancellationRequested)
        {
            try
            {
                using var response = await client.GetAsync(baseUrl, timeout.Token);

                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                await Task.Delay(500, CancellationToken.None);
            }
        }

        throw new TimeoutException($"Blazor dev server did not start at {baseUrl}.");
    }
}
