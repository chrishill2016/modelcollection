using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ModelCollection;
using ModelCollection.Features.Collection;
using ModelCollection.Models;
using ModelCollection.Services;
using MudBlazor;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
});

// Firebase web config comes from wwwroot/appsettings*.json, written by CI from repo variables.
var firebaseOptions = builder.Configuration.GetSection("Firebase").Get<FirebaseOptions>() ?? new FirebaseOptions();
builder.Services.AddSingleton(firebaseOptions);
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSingleton<BrowserInterop>();
builder.Services.AddSingleton<FirebaseInterop>();
builder.Services.AddSingleton<IIndexedDb, JsIndexedDb>();
builder.Services.AddSingleton<ILocalStore, LocalStore>();
builder.Services.AddSingleton<IConnectivityService, BrowserConnectivityService>();
builder.Services.AddSingleton<IAuthService, FirebaseAuthService>();
builder.Services.AddSingleton<IRemoteStore, FirestoreRemoteStore>();
builder.Services.AddSingleton<ICollectionService, CollectionService>();
builder.Services.AddSingleton<ISyncService, SyncService>();
builder.Services.AddSingleton<IPhotoService, PhotoService>();
builder.Services.AddSingleton<IBackupService, BackupService>();
builder.Services.AddSingleton<IUpdateService, UpdateService>();
builder.Services.AddSingleton<AppNavigation>();
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddSingleton<IThemeService>(sp => sp.GetRequiredService<ThemeService>());
builder.Services.AddSingleton<CollectionViewState>();

var host = builder.Build();
// Load the saved theme before the first render so the app never starts in the wrong theme.
await host.Services.GetRequiredService<ThemeService>().InitializeAsync();
await host.RunAsync();
