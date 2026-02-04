using Focus2Infinity.Components;
using Focus2Infinity.Data;
using Focus2Infinity.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Serilog;
using Serilog.Events;

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

try
{
    Log.Information("Starting Focus2Infinity application");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog to the application
    builder.Host.UseSerilog();

    // read the current directory
    var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "account");
    var settings = File.ReadAllLines(file);

    // con figure 
    builder.Services.Configure<CookiePolicyOptions>(options =>
    {
      options.ConsentCookie.Name = "ConsentCookie";
      options.CheckConsentNeeded = context => true;
        options.MinimumSameSitePolicy = SameSiteMode.Lax;
        options.Secure = CookieSecurePolicy.Always;
    });
    
    // Add services to the container.
    //builder.Services.AddControllers(); // Add MVC controllers
    //builder.Services.AddHttpClient(); // Add HttpClient for API calls
    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
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

    // Minimal endpoint to switch language via cookie
    //app.MapGet("/set-language", (string culture, string? returnUrl, HttpContext http) =>
    //{
    //    if (culture is not ("en" or "de" or "nl" or "fr"))
    //    {
    //        return Results.BadRequest("Unsupported culture");
    //    }

    //    var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
    //    http.Response.Cookies.Append(
    //        CookieRequestCultureProvider.DefaultCookieName,
    //        cookieValue,
    //        new CookieOptions
    //        {
    //            Expires = DateTimeOffset.UtcNow.AddYears(1),
    //            IsEssential = true,
    //            HttpOnly = false,
    //            // Allow cookie over HTTP in dev so the switch works locally
    //            Secure = http.Request.IsHttps,
    //            SameSite = SameSiteMode.Lax,
    //            Path = "/"
    //        });

    //    return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    //});

    app.MapRazorComponents<App>()
       .AddInteractiveServerRenderMode();

    Log.Information("Application configured successfully");
  
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
