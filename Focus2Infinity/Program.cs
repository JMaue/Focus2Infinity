using Focus2Infinity.Components;
using Focus2Infinity.Data;
using Focus2Infinity.Options;
using Focus2Infinity.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Serilog;
using Serilog.Events;

// ---------------------------------------------------------------------------
// Bootstrap diagnostics: write to file before any other setup. We write to
// BOTH the app directory and the system temp directory so you get a trace
// even if the app folder is not writable or the process fails before Main.
// - App dir:  BaseDirectory + "startup-bootstrap.log"
// - Temp dir: GetTempPath() + "Focus2Infinity-bootstrap.log" (same file used by ModuleInitializer)
// If neither file appears, the failure is before our assembly loads (runtime/host/dependency).
// ---------------------------------------------------------------------------
static void BootstrapLog(string message, Exception? ex = null)
{
    var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z {message}";
    if (ex != null) line += $"{Environment.NewLine}{ex}";
    line += Environment.NewLine;

    try
    {
        var appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup-bootstrap.log");
        File.AppendAllText(appPath, line);
    }
    catch { /* app dir not writable */ }

    try
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "Focus2Infinity-bootstrap.log");
        File.AppendAllText(tempPath, line);
    }
    catch { /* ignore */ }
}

BootstrapLog("Process started");
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    BootstrapLog("UnhandledException", (Exception)e.ExceptionObject);
};

// Configure Serilog before building the application
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/focus2infinity-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

BootstrapLog("Serilog CreateLogger done");

try
{
    Log.Information("Starting Focus2Infinity application");
    BootstrapLog("Serilog ready, creating WebApplicationBuilder");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog to the application
    builder.Host.UseSerilog();

    // read the current directory
    var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "account");
    BootstrapLog($"Reading account file: {file}");
    if (!File.Exists(file))
    {
        BootstrapLog($"FAIL: account file not found. BaseDirectory={AppDomain.CurrentDomain.BaseDirectory}");
        throw new FileNotFoundException("Account file is required for startup. Deploy the 'account' file next to the application.", file);
    }
    var settings = File.ReadAllLines(file);
    if (settings.Length < 6)
    {
        BootstrapLog($"FAIL: account file must have at least 6 lines, has {settings.Length}");
        throw new InvalidOperationException($"Account file must have at least 6 lines (has {settings.Length}). Check deployment.");
    }
    BootstrapLog("Account file read OK");

    // con figure 
    builder.Services.Configure<CookiePolicyOptions>(options =>
    {
      options.ConsentCookie.Name = "ConsentCookie";
      options.CheckConsentNeeded = context => true;
        options.MinimumSameSitePolicy = SameSiteMode.Lax;
        options.Secure = CookieSecurePolicy.Always;
    });
    
    // Add services to the container.
    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
    builder.Services.Configure<OverlayEditorOptions>(builder.Configuration.GetSection(OverlayEditorOptions.SectionName));

    // Persist Data Protection keys so antiforgery tokens work across restarts/redeploys (avoids "key was not found in the key ring")
    var keyPath = builder.Configuration["DataProtection:KeyPath"];
    var dir = string.IsNullOrEmpty(keyPath)
        ? Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys")
        : keyPath;
    builder.Services.AddDataProtection()
        .SetApplicationName("Focus2Infinity")
        .PersistKeysToFileSystem(new DirectoryInfo(dir));

    builder.Services.AddRazorComponents().AddInteractiveServerComponents();
    builder.Services.AddSingleton<F2IDataService>();
    builder.Services.AddSingleton<ICommentValidationService>(sp =>
    {
      var logger = sp.GetRequiredService<ILogger<CommentValidationService>>();
      return new CommentValidationService(settings[0], settings[5], logger);
    });
    builder.Services.AddSingleton<IEMailService, EMailService>(sp => new EMailService(settings[1], settings[2], settings[3], settings[4]));

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
        };
    });

    app.UseHttpsRedirection();

    app.UseCookiePolicy();

    // Request localization (English/German/Dutch/French)
    var supportedCultures = new[] { "en", "de", "nl", "fr" }
        .Select(c => new CultureInfo(c))
        .ToList();

    var localizationOptions = new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture("en"),
        SupportedCultures = supportedCultures,
        SupportedUICultures = supportedCultures,
        RequestCultureProviders = new List<IRequestCultureProvider>
        {
           // new QueryStringRequestCultureProvider { QueryStringKey = "culture", UIQueryStringKey = "ui-culture" }, // Allow culture via query string (for non-consented users)
            new CookieRequestCultureProvider(),
            new AcceptLanguageHeaderRequestCultureProvider()
        }
    };

    // Place this early so culture is set before components render
    app.UseRequestLocalization(localizationOptions);

    app.UseStaticFiles();
    app.UseAntiforgery();

    app.MapRazorComponents<App>()
       .AddInteractiveServerRenderMode();

    Log.Information("Application configured successfully");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);

    app.Run();
    //app.Start();
    //var addresses = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
    //    .Features.Get<IServerAddressesFeature>()?.Addresses;
    //Log.Information("Listening on: {Urls}", addresses is null ? "<not available>" : string.Join(";", addresses));
  
    //app.WaitForShutdown();
}
catch (Exception ex)
{
    BootstrapLog("Fatal exception", ex);
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    BootstrapLog("Shutting down (finally)");
    Log.CloseAndFlush();
}
