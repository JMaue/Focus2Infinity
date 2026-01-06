using Focus2Infinity.Components;
using Focus2Infinity.Data;
using Focus2Infinity.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// read the current directory
var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "account");
var settings = File.ReadAllLines(file); // Replace with actual path

// Add services to the container.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<F2IDataService>();
builder.Services.AddSingleton<ICommentValidationService>(sp => new CommentValidationService(settings[0]));
builder.Services.AddSingleton<IEMailService, EMailService>(sp => new EMailService(settings[1], settings[2], settings[3], settings[4]));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
  app.UseHsts();
}

app.UseHttpsRedirection();

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
    new CookieRequestCultureProvider(),
    new AcceptLanguageHeaderRequestCultureProvider()
  }
};

// Place this early so culture is set before components render
app.UseRequestLocalization(localizationOptions);

app.UseStaticFiles();
app.UseAntiforgery();

// Minimal endpoint to switch language via cookie
app.MapGet("/set-language", (string culture, string? returnUrl, HttpContext http) =>
{
  if (culture is not ("en" or "de" or "nl" or "fr"))
  {
    return Results.BadRequest("Unsupported culture");
  }

  var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
  http.Response.Cookies.Append(
    CookieRequestCultureProvider.DefaultCookieName,
    cookieValue,
    new CookieOptions
    {
      Expires = DateTimeOffset.UtcNow.AddYears(1),
      IsEssential = true,
      HttpOnly = false,
      // Allow cookie over HTTP in dev so the switch works locally
      Secure = http.Request.IsHttps,
      SameSite = SameSiteMode.Lax,
      Path = "/"
    });

  return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
});

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();
