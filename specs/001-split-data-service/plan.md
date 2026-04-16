# Implementation Plan: Split F2IDataService into Focused Services

**Branch**: `001-split-data-service` | **Date**: 2026-04-16 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-split-data-service/spec.md`

## Summary

Split the monolithic `F2IDataService` (374 lines, 5 responsibilities) into four focused services plus a shared `ImagePathResolver`. This eliminates 2 `.Result` blocking calls (Constitution II violation), enforces Single Responsibility (Constitution VIII), and removes duplicated path construction logic (DRY). Pure structural refactoring — zero behavior changes.

## Technical Context

**Project**: Focus2Infinity - International Astrophotography Gallery
**Language/Version**: C# .NET 8.0 with nullable reference types enabled
**Primary Dependencies**: Blazor Server, Bootstrap 5, Serilog.AspNetCore, Anthropic.SDK, System.Drawing.Common
**Storage**: Filesystem-based metadata (JSON files paired with images), no SQL database
**Target Platform**: Windows/Linux server deployment via Kestrel
**Project Type**: Web application with server-side rendering and real-time UI updates

## Constitution Check

*GATE: All items verified — clear to proceed.*

- ✅ **Internationalization**: No user-facing strings added/changed (pure refactoring)
- ✅ **Async Operations**: `.Result` calls eliminated; `AnnotateWithDate` becomes async
- ✅ **Filesystem Storage**: File access patterns preserved, paths centralized in `ImagePathResolver`
- ✅ **Structured Logging**: Not added in this refactoring (separate concern)
- ✅ **Privacy by Design**: Comment audit trails preserved identically
- ✅ **Component Isolation**: Only `@inject` directives change, component behavior unchanged
- ✅ **Clean Code**: Largest new class ~150 lines (well under 500); all methods under 50 lines; SRP enforced

## Project Structure

### Source Code Changes

```text
Focus2Infinity/
├── Data/
│   ├── F2IDataService.cs          # DELETE after migration
│   ├── ImagePathResolver.cs       # NEW — shared path construction
│   ├── ImageCatalogService.cs     # NEW — topic listing, sorting, navigation
│   ├── ImageMetadataService.cs    # NEW — story text, image format, markup
│   ├── OverlayService.cs          # NEW — SVG overlay CRUD
│   ├── CommentService.cs          # NEW — comment read/write
│   ├── ImageStory.cs              # UNCHANGED
│   ├── ImageItem.cs               # UNCHANGED
│   ├── CommentItem.cs             # UNCHANGED
│   └── OverlayData.cs             # UNCHANGED
├── Components/
│   ├── Cmp_Commentator.razor      # UPDATE @inject: CommentService
│   ├── Cmp_ImgHead.razor          # UPDATE @inject: ImageMetadataService
│   ├── Cmp_ImgStory.razor         # UPDATE @inject: OverlayService
│   ├── Cmp_TextCard.razor         # UPDATE @inject: ImageMetadataService
│   ├── Cmp_Topic.razor            # UPDATE @inject: ImageCatalogService
│   ├── Layout/NavMenu.razor       # UPDATE @inject: ImageCatalogService
│   └── Pages/
│       ├── Home.razor             # UPDATE @inject: ImageCatalogService
│       ├── ImageDetails.razor     # UPDATE @inject: 3 services
│       └── Editor.razor           # UPDATE @inject: 2 services
└── Program.cs                     # UPDATE DI registrations
```

**Structure Decision**: All new service files go in `Data/` alongside the existing data models, matching the current project convention. No new folders needed.

## Detailed Design

### 1. ImagePathResolver (Shared Infrastructure)

```csharp
// Data/ImagePathResolver.cs (~40 lines)
public class ImagePathResolver
{
    private readonly string _imageRoot;

    public ImagePathResolver(IWebHostEnvironment env)
    {
        _imageRoot = Path.Combine(env.WebRootPath, "img");
    }

    public string GetTopicDirectory(string topic)
        => Path.Combine(_imageRoot, topic);

    public string GetImagePath(string topic, string filename)
        => Path.Combine(_imageRoot, topic, filename);

    public string GetOverlayJsonPath(string topic, string src)
    {
        var baseName = Path.GetFileNameWithoutExtension(src);
        return Path.Combine(_imageRoot, topic, $"svg_{baseName}.overlay.json");
    }

    public string GetCommentsPath(string topic, string src)
        => Path.Combine(_imageRoot, topic, $"{src}.comments.json");

    public string GetDeniedCommentsPath(string topic, string src)
        => Path.Combine(_imageRoot, topic, $"{src}.denied.json");
}
```

**Validation**: Topic names come from the hardcoded list in `GetMainTopics()`, so path traversal is not a risk in the current design. No sanitization needed.

### 2. ImageMetadataService

```csharp
// Data/ImageMetadataService.cs (~110 lines)
// Owns: GetStoryText, DoGetStoryText, ResolveStoryJsonPath, GetImageFormat, DoGetImageFormat, Unwrap
// Depends on: ImagePathResolver
// Consumers: Cmp_ImgHead, Cmp_TextCard, ImageDetails
```

Key design decisions:
- Keeps the culture fallback chain logic (`{file}.{culture}.json` → `{file}.{lang}.json` → `{file}.json`)
- `Unwrap()` stays here because it transforms metadata text for rendering
- No `.Result` calls — all methods already properly async or synchronous

### 3. ImageCatalogService

```csharp
// Data/ImageCatalogService.cs (~150 lines)
// Owns: GetMainTopics, GetSubTopicsSorted, GetAllTopics, GetPreviosNextReferences,
//       GetSubTopics, AnnotateWithDate (now async), ToSortedList
// Depends on: ImagePathResolver, ImageMetadataService
// Consumers: Home, Cmp_Topic, NavMenu, ImageDetails, Editor
```

Key design decisions:
- `AnnotateWithDate` becomes `async Task<List<Tuple<DateTime, string>>>` — eliminates both `.Result` calls
- `GetAllTopics` no longer wraps in `Task.Run` with `.Result` — directly `await`s `GetMainTopics()` and uses async `AnnotateWithDate`
- Depends on `ImageMetadataService.GetStoryText()` for date extraction (cross-service call, acceptable per spec assumptions)

### 4. OverlayService

```csharp
// Data/OverlayService.cs (~100 lines)
// Owns: OverlayExists, DoOverlayExists, DoOverlayJsonExists, GetOverlayPathForImage,
//       SaveOverlayData, GetOverlayData, DoGetOverlayData
// Depends on: ImagePathResolver
// Consumers: Cmp_ImgStory, ImageDetails, Editor
```

Key design decisions:
- `SaveOverlayData` should become `async Task` (currently synchronous `void` — blocks on `File.WriteAllText`)
- Uses `ImagePathResolver.GetOverlayJsonPath()` for `svg_*.overlay.json` path construction
- Legacy `ovl_*.jpg` check uses `ImagePathResolver.GetImagePath()` with `$"ovl_{src}"` prefix

### 5. CommentService

```csharp
// Data/CommentService.cs (~50 lines)
// Owns: GetCommentHistory, AddComment
// Depends on: ImagePathResolver
// Consumers: Cmp_Commentator
```

Key design decisions:
- Already properly async (`ReadAllTextAsync`, `WriteAllTextAsync` in `AddComment`)
- Uses `ImagePathResolver.GetCommentsPath()` and `GetDeniedCommentsPath()`

### 6. Program.cs DI Registration Changes

```csharp
// Replace:
builder.Services.AddSingleton<F2IDataService>();

// With:
builder.Services.AddSingleton<ImagePathResolver>();
builder.Services.AddSingleton<ImageMetadataService>();
builder.Services.AddSingleton<ImageCatalogService>();
builder.Services.AddSingleton<OverlayService>();
builder.Services.AddSingleton<CommentService>();
```

Registration order matters: `ImagePathResolver` first (no dependencies), then `ImageMetadataService` (depends on resolver), then `ImageCatalogService` (depends on both), then the rest.

## Consumer Injection Updates

| Consumer | Remove | Add |
|---|---|---|
| `Home.razor` | `@inject F2IDataService f2iDataService` | `@inject ImageCatalogService imageCatalogService` |
| `Cmp_Topic.razor` | `@inject F2IDataService f2iDataService` | `@inject ImageCatalogService imageCatalogService` |
| `NavMenu.razor` | `@inject F2IDataService f2iDataService` | `@inject ImageCatalogService imageCatalogService` |
| `ImageDetails.razor` | `@inject F2IDataService f2iDataService` | `@inject ImageCatalogService imageCatalogService`<br>`@inject ImageMetadataService imageMetadataService`<br>`@inject OverlayService overlayService` |
| `Cmp_ImgHead.razor` | `@inject F2IDataService f2iDataService` | `@inject ImageMetadataService imageMetadataService` |
| `Cmp_TextCard.razor` | `@inject F2IDataService f2iDataService` | `@inject ImageMetadataService imageMetadataService` |
| `Cmp_ImgStory.razor` | `@inject F2IDataService f2iDataService` | `@inject OverlayService overlayService` |
| `Cmp_Commentator.razor` | `@inject F2IDataService f2iDataService` | `@inject CommentService commentService` |
| `Editor.razor` | `@inject F2IDataService f2iDataService` | `@inject ImageCatalogService imageCatalogService`<br>`@inject OverlayService overlayService` |

## Async Fix Detail

The two `.Result` blocking calls in `F2IDataService` are eliminated as follows:

**Before** (in `F2IDataService`):
```csharp
// Line 53 — inside AnnotateWithDate (synchronous)
var src = GetStoryText(mainTopic, f.Name).Result;

// Line 91 — inside GetAllTopics Task.Run block
foreach (var topic in GetMainTopics().Result)
```

**After** (in `ImageCatalogService`):
```csharp
// AnnotateWithDate becomes async
private async Task<List<Tuple<DateTime, string>>> AnnotateWithDate(
    IEnumerable<FileInfo> files, string mainTopic, IStringLocalizer L)
{
    var list = new List<Tuple<DateTime, string>>();
    foreach (var f in files)
    {
        var src = await _imageMetadataService.GetStoryText(mainTopic, f.Name);
        // ...
    }
    return list;
}

// GetAllTopics directly awaits instead of Task.Run + .Result
public async Task<List<ImageItem>> GetAllTopics(IStringLocalizer L)
{
    var allTopics = new List<Tuple<DateTime, string, string>>();
    foreach (var topic in await GetMainTopics())
    {
        var files = GetSubTopics(topic);
        var list = await AnnotateWithDate(files, topic, L);
        // ...
    }
    // ...
}
```

## Implementation Order

The implementation must follow this dependency chain:

```
Phase 1: ImagePathResolver           (no dependencies)
Phase 2: ImageMetadataService         (depends on Phase 1)
         CommentService               (depends on Phase 1, parallel with above)
         OverlayService               (depends on Phase 1, parallel with above)
Phase 3: ImageCatalogService          (depends on Phases 1+2)
Phase 4: Program.cs DI registration   (depends on all Phase 1-3)
Phase 5: Consumer injection updates   (depends on Phase 4)
Phase 6: Delete F2IDataService.cs     (depends on Phase 5)
Phase 7: Build verification           (depends on Phase 6)
```

## Complexity Tracking

No constitution violations in this plan. All classes well under 500 lines, all methods under 50 lines, SRP enforced.

## Risk Assessment

| Risk | Mitigation |
|---|---|
| Behavior regression in gallery sorting | No logic changes — only method relocation and async fix |
| `ImageCatalogService` → `ImageMetadataService` circular dependency | Not circular: catalog depends on metadata, not vice versa |
| `SaveOverlayData` is currently synchronous `void` | Make async with `File.WriteAllTextAsync` — callers already in async context |
| Missing `using` directives after split | `dotnet build` will catch immediately in Phase 7 |
