# Focus2Infinity – Architecture Cheat Sheet & Copilot Instructions

## Project Overview
- **Tech Stack**: Blazor Server (.NET 8, C# 12) with Interactive Server Components
- **UI Framework**: Bootstrap 5, Font Awesome (via static includes)
- **Logging**: Serilog (Console + File with daily rolling, 30-day retention)
- **Localization**: Multi-language support (en, de, nl, fr) with cookie consent
- **AI Integration**: Anthropic Claude API for comment moderation
- **Email**: SMTP via custom `EMailService`
- **Purpose**: Astrophotography gallery site organized by topics (Galaxies, Nebulae, Clusters, Planets, Eclipses, Milkyway, Moon, Sun, Sunsets, Comets, Landscapes, StarTrails, Others)

## Entry Points
- **`Program.cs`**: Application startup, DI configuration, middleware pipeline, Serilog configuration
- **`Components/App.razor`**: Root component with `<HeadOutlet />` and `<Routes />`
- **Bootstrap JS**: Loaded in App.razor (`bootstrap/bootstrap.bundle.min.js`) for toast notifications and modals

---

## Solution Structure

### Components
```
Components/
??? Pages/                    # Routable pages (@page directive)
?   ??? Home.razor           # Main gallery (loads all images)
?   ??? ImageDetails.razor   # Detail view with comments
?   ??? Privacy.razor        # Privacy policy / cookie info
?   ??? {TopicName}.razor    # Topic pages (e.g., Galaxies.razor)
??? Layout/
?   ??? MainLayout.razor     # Main layout wrapper
?   ??? NavMenu.razor        # Navigation sidebar with language switcher
??? Cmp_*.razor              # Reusable components (naming convention)
?   ??? Cmp_Topic.razor      # Topic page wrapper
?   ??? Cmp_ImgHead.razor    # Image thumbnail with headline
?   ??? Cmp_ImageCard.razor  # Image card display
?   ??? Cmp_ImgStory.razor   # Image + story display
?   ??? Cmp_TextCard.razor   # Text metadata card
?   ??? Cmp_Commentator.razor # Comment submission form with validation
?   ??? Cmp_CookieConsent.razor # Cookie consent banner
??? LanguageSwitcher.razor   # Language dropdown selector
```

### Controllers (API Endpoints)
```
Controllers/
??? LanguageController.cs    # API controller for language switching
    ??? GET /api/language/set?culture={culture}&returnUrl={url}
```

### Services
```
Services/
??? F2IDataService.cs        # Main data access (images, JSON metadata)
??? CommentValidationService.cs # AI-powered comment moderation
??? EMailService.cs          # SMTP email sending
??? Interfaces/
    ??? ICommentValidationService.cs
    ??? IEMailService.cs
```

### Data Models
```
Data/
??? ImageStory.cs            # Represents image metadata JSON
??? ImageItem.cs             # (src, topic) tuple for gallery
??? CommentItem.cs           # User comment with validation result
??? DataHelper.cs            # Extension methods for ImageStory
```

### Static Assets
```
wwwroot/
??? img/
?   ??? {MainTopic}/         # e.g., Galaxies/, Nebulae/
?       ??? *.jpg|png|tif    # Image files
?       ??? *.jpg.json       # Neutral metadata (no language suffix)
?       ??? *.jpg.{lang}.json # Localized metadata (e.g., .de.json)
?       ??? *.jpg.comments.json # Approved comments
?       ??? *.jpg.denied.json   # Rejected comments
?       ??? ovl_*.jpg        # Overlay images (for annotations)
??? css/
?   ??? app.css              # Bootstrap 5 styles
?   ??? site.css             # Custom site styles
??? bootstrap/
?   ??? bootstrap.min.css
?   ??? bootstrap.bundle.min.js # Required for toasts!
??? flags/                   # Country flags for language selector
```

---

## Key Services & Responsibilities

### F2IDataService (Singleton)
**Purpose**: Centralized data access for images and metadata  
**Key Methods**:
```csharp
Task<List<string>> GetMainTopics()
  ? Returns: ["Galaxies", "Nebulae", "Clusters", ...]

Task<List<string>> GetSubTopicsSorted(string mainTopic)
  ? Returns image filenames in topic, sorted by date (desc) from JSON "Datum"
  ? Uses neutral JSON (.json) for date to ensure language-neutral sorting

Task<List<ImageItem>> GetAllTopics()
  ? Returns all images across all topics, sorted by date (desc)

Task<ImageStory> GetStoryText(string topic, string src)
  ? Loads metadata JSON with language fallback:
    1. {file}.{culture}.json (e.g., M31.jpg.de.json)
    2. {file}.{lang}.json (e.g., M31.jpg.de.json)
    3. {file}.json (neutral)

Task<(int width, int height)> GetImageFormat(string topic, string src)
  ? Reads image dimensions using System.Drawing.Image

Task<bool> OverlayExists(string topic, string src)
  ? Checks if ovl_{src} exists (for annotated images)

Task<List<CommentItem>> GetCommentHistory(string topic, string src)
  ? Loads approved comments from {file}.comments.json

Task AddComment(string topic, string src, CommentItem comment, bool isValid)
  ? Saves comment to .comments.json (if valid) or .denied.json (if invalid)

string Unwrap(string input)
  ? Expands custom markup:
    ###Text######Url### ? <a href="Url"><span style="...">Text</span></a>
    ##Url## ? <a href="Url"><span style="...">Url</span></a>
```

### CommentValidationService (Singleton)
**Purpose**: AI-powered content moderation using Anthropic Claude  
**Configuration**:
```csharp
// Registered in Program.cs with API key from settings[0]
builder.Services.AddSingleton<ICommentValidationService>(sp =>
{
  var logger = sp.GetRequiredService<ILogger<CommentValidationService>>();
  return new CommentValidationService(apiKey, modelId, logger);
});
```
**Key Method**:
```csharp
Task<(bool isValid, string reason)> IsValidComment(string comment)
  ? Sends comment to Claude API with moderation prompt
  ? Returns: (true, "Approved") or (false, "Reason for rejection")
  ? Handles markdown JSON response (```json...```)
  ? Max comment length: 500 characters
  ? Timeout: 120 seconds
```

### EMailService (Singleton)
**Purpose**: Send notification emails via SMTP  
**Configuration**:
```csharp
builder.Services.AddSingleton<IEMailService, EMailService>(sp => 
  new EMailService(host, port, username, password));
```
**Key Method**:
```csharp
Task SendAsync(string to, string subject, string body, ...)
  ? Uses System.Net.Mail.SmtpClient
  ? Supports SSL/TLS
```

---

## Localization Architecture

### Request Culture Resolution (Priority Order)
```csharp
// Configured in Program.cs
RequestCultureProviders:
1. QueryStringRequestCultureProvider   // ?culture=de&ui-culture=de
2. CookieRequestCultureProvider        // .AspNetCore.Culture cookie
3. AcceptLanguageHeaderRequestCultureProvider // HTTP Accept-Language header
```

### Language Switching Flow
**With Cookie Consent**:
```
1. User selects language in LanguageSwitcher dropdown
2. Calls: /api/language/set?culture=de
3. Controller checks cookie consent
4. Sets .AspNetCore.Culture cookie (expires in 1 year)
5. Redirects to original page
6. Middleware reads cookie ? Sets CurrentUICulture
7. Page renders in selected language (persists across sessions)
```

**Without Cookie Consent**:
```
1. User selects language
2. Calls: /api/language/set?culture=de
3. Controller detects NO consent
4. Redirects to: /page?culture=de&ui-culture=de
5. QueryStringRequestCultureProvider reads ?culture=de
6. Page renders in selected language (only current session)
7. Next navigation loses ?culture ? Back to default/browser language
```

### Reading Current Language
```csharp
// In Razor components
@inject IStringLocalizer<SharedResource> L
var text = L["Key"];  // Uses CultureInfo.CurrentUICulture automatically

// In code
var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName; // "en", "de", etc.
var culture = Thread.CurrentThread.CurrentUICulture; // Same thing

// In controllers
var feature = HttpContext.Features.Get<IRequestCultureFeature>();
var currentCulture = feature?.RequestCulture.Culture.TwoLetterISOLanguageName;
```

### Resource Files Location
```
Resources/
??? SharedResource.{culture}.resx
    ??? SharedResource.resx       # English (default)
    ??? SharedResource.de.resx    # German
    ??? SharedResource.nl.resx    # Dutch
    ??? SharedResource.fr.resx    # French
```

---

## Cookie Consent (GDPR Compliance)

### Components
- **`Cmp_CookieConsent.razor`**: Banner shown on first visit
- **Consent Storage**: 
  - LocalStorage: `cookieConsent` = "accepted" | "declined"
  - Cookie: `cookieConsent` = "accepted" | "declined" (for server-side checks)

### Behavior
```csharp
// Accept: Sets cookies, enables language persistence
await JSRuntime.InvokeVoidAsync("eval", @"
  document.cookie = 'cookieConsent=accepted; path=/; max-age=31536000; SameSite=Lax; Secure';
");

// Decline: No language cookie, clears existing cookies
// Language switching still works via query parameters
```

### Controller Check
```csharp
// In LanguageController.HasCookieConsent()
if (Request.Cookies.TryGetValue("cookieConsent", out var consent))
{
  return consent == "accepted";
}
return false; // Strict mode
```

---

## Comment System Architecture

### Flow
```
1. User fills form in Cmp_Commentator (Name, Email, Comment)
2. OnSubmit ? CommentValidationService.IsValidComment()
3. Claude API analyzes comment (moderation prompt)
4. Result: (isValid, reason)
5. If valid:
   - Add to comments list
   - Save to {file}.comments.json
   - Send email to admin
   - Show success toast
6. If invalid:
   - Save to {file}.denied.json
   - Send email to admin with reason
   - Show rejection toast with reason
```

### Toast Notifications
```javascript
// Implemented in Cmp_Commentator.razor
// Uses Bootstrap 5 Toast API
var toast = new bootstrap.Toast(toastEl, { autohide: true, delay: 5000 });
toast.show();
```

### Comment Storage
```json
// {file}.comments.json (approved)
[
  {
    "Name": "John Doe",
    "Email": "john@example.com",
    "Text": "Amazing photo!",
    "Date": "2024-01-15T10:30:00Z",
    "Reason": "Approved"
  }
]

// {file}.denied.json (rejected)
[
  {
    "Name": "Spammer",
    "Email": "spam@example.com",
    "Text": "Buy cheap products!",
    "Date": "2024-01-15T10:31:00Z",
    "Reason": "Spam content detected"
  }
]
```

---

## Data Conventions

### Image Metadata JSON
**Neutral (no language)**: `M31.jpg.json`
```json
{
  "Headline": "Andromeda Galaxy",
  "Datum": "2024-09-21",              // ISO date for sorting
  "Ort": "Heidelberg, Germany",
  "Optik": "Canon RF 240mm",
  "Kamera": "Canon R6",
  "Exposure Settings": "ISO 2000, f/6.3, 40s",
  "Kommentar": "4x10s stacked",
  "Details": "Processing with DeepSkyStacker, Siril, Photoshop"
}
```

**Localized**: `M31.jpg.de.json`
```json
{
  "Headline": "Andromeda-Galaxie",
  "Datum": "2024-09-21",              // Same date (for sorting)
  "Ort": "Heidelberg, Deutschland",
  "Optik": "Canon RF 240mm",
  "Kamera": "Canon R6",
  "Exposure Settings": "ISO 2000, f/6.3, 40s",
  "Kommentar": "4x10s gestackt",
  "Details": "Bearbeitung mit DeepSkyStacker, Siril, Photoshop"
}
```

### File Naming Conventions
- **Images**: `*.jpg`, `*.png`, `*.tif`
- **Thumbnails**: `tbn_*.jpg` (skipped by GetSubTopics)
- **Overlays**: `ovl_*.jpg` (annotations/labels)
- **Metadata**: `{filename}.json` (neutral), `{filename}.{lang}.json` (localized)
- **Comments**: `{filename}.comments.json`, `{filename}.denied.json`

### DataHelper Extensions
```csharp
// Extension methods on Dictionary<string, string>
string GetHeadline() ? story["Headline"] ?? ""
DateTime GetDateTime() ? Parse story["Datum"] or DateTime.MinValue
IEnumerable<KeyValuePair<string, string>> GetContent() 
  ? All fields except "Headline" and "Details"
IEnumerable<KeyValuePair<string, string>> GetDetailedContent() 
  ? GetContent() + "Details" at end
```

---

## Middleware Pipeline (Program.cs)

```csharp
// Order matters!
app.UseExceptionHandler("/Error")       // Production error handling
app.UseHsts()                           // HTTP Strict Transport Security
app.UseSerilogRequestLogging()          // Log all HTTP requests
app.UseHttpsRedirection()               // Force HTTPS
app.UseCookiePolicy()                   // Cookie consent checks
app.UseRequestLocalization()            // Set CurrentUICulture
app.UseStaticFiles()                    // Serve wwwroot files
app.UseAntiforgery()                    // CSRF protection
app.MapControllers()                    // API controllers
app.MapRazorComponents<App>()           // Blazor components
```

---

## Logging with Serilog

### Configuration
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/focus2infinity-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();
```

### Usage in Services
```csharp
// Constructor injection
private readonly ILogger<CommentValidationService> _logger;

public CommentValidationService(string apiKey, ILogger<CommentValidationService> logger)
{
  _logger = logger;
  _logger.LogInformation("Initializing CommentValidationService");
}

// Logging examples
_logger.LogInformation("Comment validation completed. IsValid: {IsValid}", isValid);
_logger.LogWarning("Invalid culture requested: {Culture}", culture);
_logger.LogError(ex, "Error validating comment");
```

### Log Output Location
```
logs/
??? focus2infinity-20240115.log
??? focus2infinity-20240116.log
??? ... (30 days retention)
```

---

## Blazor Patterns & Conventions

### Component Naming
- **Pages**: `PascalCase` (e.g., `ImageDetails.razor`)
- **Components**: `Cmp_PascalCase` (e.g., `Cmp_Commentator.razor`)
- **Services**: `PascalCaseService` (e.g., `F2IDataService`)
- **Interfaces**: `IPascalCaseService` (e.g., `IEMailService`)

### Component Structure
```razor
@page "/route"                 // Page directive (if routable)
@using Namespace               // Imports
@inject ServiceType Service    // Dependency injection
@rendermode InteractiveServer  // Render mode (if needed)

<!-- Markup -->
<div>...</div>

@code {
  [Parameter] public string Param { get; set; } = string.Empty;
  
  private string _privateField;
  
  protected override async Task OnInitializedAsync()
  {
    // Load data
  }
  
  private async Task HandleAction()
  {
    // Event handlers
  }
}
```

### Dependency Injection
```csharp
// Register in Program.cs
builder.Services.AddSingleton<F2IDataService>();
builder.Services.AddSingleton<ICommentValidationService>(sp => ...);

// Inject in components
@inject F2IDataService f2iDataService
@inject IStringLocalizer<SharedResource> L
@inject ILogger<MyComponent> Logger
```

### Async/Await Best Practices
- Always `await` async operations
- Use `Task.Run()` sparingly (only for CPU-bound work)
- Prefer `async Task` over `async void` (except event handlers)
- Pass `CancellationToken` for long operations

### State Management
- Use `@bind` for two-way binding
- Call `StateHasChanged()` after async updates (if needed)
- Avoid static state (breaks Blazor Server circuit isolation)

---

## Security Considerations

### CSRF Protection
- `app.UseAntiforgery()` enabled
- Blazor's `<EditForm>` automatically includes antiforgery tokens
- Protects against cross-site request forgery attacks

### Cookie Security
```csharp
new CookieOptions
{
  Expires = DateTimeOffset.UtcNow.AddYears(1),
  IsEssential = true,      // Exempt from consent (if needed)
  HttpOnly = false,        // Allow JS access (for language switcher)
  Secure = Request.IsHttps, // HTTPS only in production
  SameSite = SameSiteMode.Lax,
  Path = "/"
}
```

### API Key Management
- Stored in external `account` file (not in source control)
- Loaded at startup: `File.ReadAllLines(Path.Combine(BaseDirectory, "account"))`
- Format:
  ```
  Line 0: Anthropic API Key
  Line 1: SMTP Host
  Line 2: SMTP Port
  Line 3: SMTP Username
  Line 4: SMTP Password
  Line 5: Anthropic Model ID
  ```

### Content Moderation
- All user comments validated via Claude API before storage
- Rejected comments saved to `.denied.json` for audit trail
- Admin notified via email of all submissions (valid + invalid)

---

## Bootstrap 5 Usage

### Common Classes
```css
/* Layout */
.container, .container-fluid
.row, .col-{breakpoint}-{size}
.d-flex, .justify-content-{start|center|end}
.align-items-{start|center|end}
.position-{relative|absolute|fixed}

/* Spacing */
.m-{0-5}, .mt-3, .mb-5, .p-3, .px-4, .py-2

/* Text */
.text-{start|center|end}
.text-{primary|secondary|success|danger|warning|muted}
.fw-bold, .fst-italic

/* Components */
.card, .card-header, .card-body
.btn, .btn-{primary|secondary|success|danger}
.form-control, .form-select
.toast, .toast-container
```

### Toast API (Bootstrap 5)
```javascript
var toastEl = document.getElementById('commentToast');
var toast = new bootstrap.Toast(toastEl, { 
  autohide: true, 
  delay: 5000 
});
toast.show();
```

---

## Adding New Features

### Adding a New Main Topic
1. Create folder: `wwwroot/img/{NewTopic}/`
2. Add images: `*.jpg`, `*.png`, `*.tif`
3. Add metadata: `{filename}.json` (neutral), `{filename}.{lang}.json` (localized)
4. Create page: `Components/Pages/{NewTopic}.razor`
   ```razor
   @page "/{newtopic}"
   <Cmp_Topic Topic="{NewTopic}" />
   ```
5. Update `F2IDataService.GetMainTopics()` to include new topic
6. Add localization key to `SharedResource.{lang}.resx`

### Adding a New Localized String
1. Open `Resources/SharedResource.resx`
2. Add key/value: `<data name="MyKey"><value>My Text</value></data>`
3. Repeat for `SharedResource.de.resx`, `SharedResource.nl.resx`, `SharedResource.fr.resx`
4. Use in components: `@L["MyKey"]`

### Adding a New API Endpoint
1. Create controller in `Controllers/` folder
2. Inherit from `ControllerBase`
3. Add `[Route("api/[controller]")]` and `[ApiController]` attributes
4. Add action methods with `[HttpGet]`, `[HttpPost]`, etc.
5. Controllers are auto-registered via `app.MapControllers()`

---

## Common Tasks & Snippets

### Load Image Metadata
```csharp
@inject F2IDataService f2iDataService

private ImageStory? story;

protected override async Task OnInitializedAsync()
{
  story = await f2iDataService.GetStoryText(Topic, Src);
}

// Render
<h4>@story.GetHeadline()</h4>
@foreach (var kv in story.GetContent())
{
  <div><b>@kv.Key:</b> @kv.Value</div>
}
```

### Display Localized Text
```razor
@inject IStringLocalizer<SharedResource> L

<h3>@L["Loading"]</h3>
<button>@L["Submit"]</button>
```

### Show Toast Notification
```csharp
private async Task ShowToast()
{
  await JSRuntime.InvokeVoidAsync("eval", @"
    var toastEl = document.getElementById('myToast');
    if (toastEl && typeof bootstrap !== 'undefined') {
      var toast = new bootstrap.Toast(toastEl, { autohide: true, delay: 5000 });
      toast.show();
    }
  ");
}
```

### Check Cookie Consent
```csharp
// In controller
private bool HasCookieConsent()
{
  if (Request.Cookies.TryGetValue("cookieConsent", out var consent))
  {
    return consent == "accepted";
  }
  return false;
}
```

### Custom Link Markup (Unwrap)
```
Input:  "Visit ###NASA######nasa.gov###"
Output: <a href="https://nasa.gov"><span style="...">NASA</span></a>

Input:  "Website: ##example.com##"
Output: <a href="https://example.com"><span style="...">example.com</span></a>
```

---

## Troubleshooting

### Language Not Switching
1. Check logs: `logs/focus2infinity-{date}.log`
2. Verify cookie consent: Browser DevTools ? Application ? Cookies
3. Check `QueryStringRequestCultureProvider` order in `Program.cs`
4. Ensure Bootstrap JS is loaded in `App.razor`

### Toast Not Showing
1. Verify `<script src="bootstrap/bootstrap.bundle.min.js"></script>` in `App.razor`
2. Check browser console for JS errors
3. Ensure toast HTML is rendered before calling `toast.show()`
4. Add `StateHasChanged()` + `await Task.Delay(100)` before JS call

### Comments Not Saving
1. Check Anthropic API key in `account` file
2. Verify network access to `api.anthropic.com:443`
3. Check Serilog output for API errors
4. Ensure `F2IDataService.AddComment()` has write permissions to `wwwroot/img/`

### Images Not Loading
1. Verify file exists in `wwwroot/img/{Topic}/{Filename}`
2. Check file extensions: `.jpg`, `.png`, `.tif` only
3. Ensure files don't start with `tbn_` or `ovl_` (unless intentional)
4. Check browser Network tab for 404 errors

---

## Environment Configuration

### Development
```bash
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run
```
- Shows detailed error pages
- Enables hot reload
- Uses HTTP (localhost)

### Production
```bash
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet run
```
- Generic error pages (`/Error`)
- HSTS enabled
- HTTPS enforced
- Serilog logs to file only (no console in production IIS)

---

## Code Style Guidelines

### C# Conventions
- **Naming**: `PascalCase` for public members, `_camelCase` for private fields
- **Async Methods**: Suffix with `Async` (e.g., `GetDataAsync`)
- **Braces**: Always use braces for control flow (even single-line)
- **Nullability**: Use `?` for nullable reference types (C# 12)
- **String Interpolation**: Prefer `$"{var}"` over `string.Format()`

### Razor Conventions
- **Code Blocks**: Use `@code { }` at bottom of file
- **Parameters**: Always use `[Parameter]` attribute
- **Directives**: Order: `@page`, `@using`, `@inject`, `@rendermode`, markup, `@code`
- **Comments**: Use `@* *@` for Razor comments, `<!-- -->` for HTML comments

### Logging Conventions
```csharp
// Use structured logging (not string concatenation)
_logger.LogInformation("User {UserId} loaded {ImageCount} images", userId, count);

// Log levels:
LogTrace     // Very detailed (disabled by default)
LogDebug     // Debugging info
LogInformation // General flow
LogWarning   // Unexpected but handled
LogError     // Failures
LogCritical  // App crash
```

---

## Key Copilot Reminders

? **Always use `@inject` for services** – Never create new instances manually  
? **Use `IStringLocalizer<SharedResource>` for all user-facing text** – Don't hardcode strings  
? **Check cookie consent before setting cookies** – GDPR compliance  
? **Log important events with Serilog** – Helps debugging in production  
? **Use `async/await` for I/O operations** – Never block threads  
? **Prefer Bootstrap classes over inline styles** – Consistent UI  
? **Use DataHelper extensions for JSON** – Don't access Dictionary keys directly  
? **Handle exceptions gracefully** – Log errors, don't crash  
? **Test language switching in both consent modes** – Query parameter fallback  
? **Use neutral JSON for date-based sorting** – Avoid language-dependent sorting  

---

## Quick Reference Commands

```bash
# Build & Run
dotnet build
dotnet run

# Publish Release
dotnet publish -c Release -o ./publish

# Watch Mode (hot reload)
dotnet watch run

# Restore NuGet Packages
dotnet restore

# Add NuGet Package
dotnet add package Serilog.AspNetCore

# Check Logs
Get-Content logs/focus2infinity-$(Get-Date -Format "yyyyMMdd").log -Tail 50

# Set Environment Variable
$env:ASPNETCORE_ENVIRONMENT="Production"
```

---

**Last Updated**: 2025-01-07  
**Maintainer**: Focus2Infinity Team  
**Copilot Version**: GitHub Copilot (VS 2022)
**Cookie Consent**: ? GDPR Compliant - Implementation Complete
