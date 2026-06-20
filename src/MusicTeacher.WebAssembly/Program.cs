using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MusicTeacher.WebAssembly;
using MusicTeacher.WebAssembly.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<AppLocalizer>();
builder.Services.AddScoped<IProgressStore, BrowserProgressStore>();
builder.Services.AddScoped<MusicAudioService>();

var host = builder.Build();
await host.Services.GetRequiredService<AppLocalizer>().InitializeAsync();
await host.RunAsync();


/// how to build:
/// docker login
/// from the src folder run the following command to build the docker image:
/// docker build -t wardimpe/musicteacher:latest -f .\MusicTeacher.WebAssembly\Dockerfile .
/// now you can push the image to docker hub:
/// docker push wardimpe/musicteacher:latest


