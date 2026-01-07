# Focus2Infinity – Copilot Instructions

Project overview
- Tech stack: Blazor (.NET 8, C# 12) with Razor Components (Server). Bootstrap/Font Awesome via static includes.
- Entry points:
  - `Program.cs`: configures services and maps Razor components with interactive server render mode.
  - `Components/App.razor`: app shell using `<HeadOutlet />` and `<Routes />`.
- Purpose: Photo gallery site organized by main topics (e.g., `Galaxies`, `Nebulae`, `Clusters`, `Planets`, `Eclipses`, `Milkyway`, `Moon`, `Sun`, `Sunsets`, `Comets`, `Landscapes`, `StarTrails`, `Others`).

Solution layout
- Pages (routeable): `Components/Pages/*.razor` (e.g., `Home`, `StarTrails`, `Moon`, `Clusters`, `Galaxies`, etc.). Many topic pages are thin wrappers around the `Cmp_Topic` component, e.g.:
  ```razor
  @page "/startrails"
  <Cmp_Topic Topic="StarTrails" />
  ```
- Components: `Components/*` include reusable UI pieces: `Cmp_Topic`, `Cmp_ImgHead`, `Cmp_ImageCard`, `Cmp_ImgStory`, `Cmp_TextCard`, `Routes.razor`, `_Imports.razor`.
- Layout: `Components/Layout/MainLayout.razor`, `Components/Layout/NavMenu.razor`.
- Static assets: `wwwroot/**`. Images under `wwwroot/img/{MainTopic}/`. Optional sidecar JSON per image at `wwwroot/img/{MainTopic}/{FileName}.json`.

Dependency injection and services
- `Program.cs` registers services and Razor Components:
  - `builder.Services.AddRazorComponents().AddInteractiveServerComponents();`
  - `builder.Services.AddSingleton<F2IDataService>();`
- `F2IDataService` (data provider) uses `IWebHostEnvironment.WebRootPath` to read from `wwwroot/img/**`.
  - `Task<List<string>> GetMainTopics()` ? list of main topics.
  - `Task<List<string>> GetSubTopicsSorted(string mainTopic)` ? file names in topic, sorted by date (desc) from JSON `"Datum"`.
  - `Task<List<Tuple<string, string>>> GetAllTopics()` ? across all topics, returns `(src, topic)` sorted by date.
  - `Task<Dictionary<string,string>> GetStoryText(string topic, string src)` ? loads `{src}.json` if present.
  - `Task<(int width,int height)> GetImageFormat(string topic, string src)` ? reads image dimensions via `System.Drawing`.
  - `Task<bool> OverlayExists(string topic, string src)` ? checks for `ovl_{src}` in topic folder.
  - `string Unwrap(string input)` ? expands custom link markup:
    - `###Text######Url###` ? anchor with bold span
    - `##Url##` ? anchor with bold span
- `DataHelper` (extensions for story JSON):
  - `GetHeadline(this Dictionary<string,string>)` ? `"Headline"` or empty.
  - `GetDateTime(this Dictionary<string,string>)` ? parses `"Datum"` to `DateTime` (fallback `DateTime.MinValue`).
  - `GetContent(this Dictionary<string,string>)` ? all key/value except `"Headline"` and `"Details"`.
  - `GetDetailedContent(this Dictionary<string,string>)` ? `GetContent` plus trailing `"Details"` if present.

File and data conventions
- Image files considered: `.jpg`, `.png`, `.tif` in `wwwroot/img/{MainTopic}`. Skips files starting with `tbn_` (thumbnail) and `ovl_` (overlay file prefix used for overlay detection).
- Story metadata JSON per image: `wwwroot/img/{MainTopic}/{FileName}.json` with keys like:
  ```json
  {
    "Headline": "Andromeda Galaxy",
    "Datum": "2024-09-21",
    "Camera": "Nikon D750",
    "Scope": "...",
    "Details": "Processing notes, acquisition details, etc."
  }
  ```
- Use `DataHelper` extension methods to access and render this JSON safely.

Known components/pages and usage
- `Home.razor`:
  - Injects `F2IDataService` and loads `images: List<Tuple<string,string>>` from `GetAllTopics()`.
  - Renders a grid of cards using `Cmp_ImgHead` with `Topic` and `Src`:
    ```razor
    <Cmp_ImgHead Topic="@GetTopic(i)" Src="@GetImg(i)" />
    ```
- Topic pages (e.g., `StarTrails.razor`, `Moon.razor`, `Clusters.razor`, etc.) typically delegate to `Cmp_Topic` with `Topic="..."`.
- Other reusable components present: `Cmp_ImageCard`, `Cmp_ImgStory`, `Cmp_TextCard` (consult those components for parameters; follow naming `PascalCase` with `Cmp_` prefix).

Blazor patterns and conventions
- Prefer Blazor Server patterns over MVC/Razor Pages.
- Keep `.razor` files thin. For complex logic, add `partial` code-behind files (`.razor.cs`) and inject services with `[Inject]` or `@inject`.
- Use async/await and avoid blocking. Pass cancellation tokens for long-running operations.
- Prefer Bootstrap classes and `wwwroot/css` styles over inline styles.
- Use string interpolation and culture-aware formatting for user content.
- When handling dates for sorting, rely on `DataHelper.GetDateTime()`.

Adding a new main topic
1) Add images to `wwwroot/img/{NewTopic}/` (optionally add `{FileName}.json` with `Headline/Datum/Details`).
2) Add `Components/Pages/{NewTopic}.razor`:
   ```razor
   @page "/{route}"
   <Cmp_Topic Topic="{NewTopic}" />
   ```
3) Add the topic to `F2IDataService.GetMainTopics()`.

Example usage snippets
- Loading and rendering image details in a component:
  ```razor
  @inject F2IDataService Data
  @code {
    [Parameter] public string Topic { get; set; } = string.Empty;
    [Parameter] public string Src { get; set; } = string.Empty;

    private Dictionary<string,string>? story;
    private (int w,int h) size;

    protected override async Task OnParametersSetAsync()
    {
      story = await Data.GetStoryText(Topic, Src);
      size = await Data.GetImageFormat(Topic, Src);
    }
  }
  ```
- Rendering JSON content using `DataHelper`:
  ```razor
  @using Focus2Infinity.Data
  @if (story is not null)
  {
    <h4>@story.GetHeadline()</h4>
    @foreach (var kv in story.GetDetailedContent())
    {
      <div><b>@kv.Key:</b> @((MarkupString)Data.Unwrap(kv.Value))</div>
    }
  }
  ```

Naming/style guidance for Copilot
- Components: `Cmp_*` prefix, `PascalCase` names.
- Services: `PascalCase` classes; `F2IDataService` is already registered as `Singleton`.
- Avoid introducing external state; read-only access through the service.
- Prefer strongly typed records for new models instead of `Tuple<,>` in new code, but keep existing APIs stable.

Notes for Copilot
- Assume .NET 8 Razor Components with interactive server (`AddInteractiveServerComponents`), `<Routes />`, `<HeadOutlet />`.
- Prefer Blazor-first solutions (no MVC Controllers).
- Use DI for `F2IDataService` instead of directly reading from `wwwroot`.
- Consider overlays: when generating UI that displays images, optionally check `OverlayExists(topic, src)` to decide whether to render an overlay layer.
