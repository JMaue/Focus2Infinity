# Tasks: Split F2IDataService into Focused Services

**Input**: Design documents from `/specs/001-split-data-service/`
**Prerequisites**: plan.md (required), spec.md (required)

**Tests**: Not requested — no test tasks included.

**Organization**: Tasks follow the dependency chain from plan.md: shared infrastructure first, then independent services in parallel, then the dependent catalog service, then wiring, then cleanup.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the shared `ImagePathResolver` that all four services depend on

- [X] T001 Create `ImagePathResolver` class with path construction methods in Focus2Infinity/Data/ImagePathResolver.cs

  Extract all `Path.Combine(_hostingEnvironment.WebRootPath, "img", ...)` patterns from F2IDataService into dedicated methods:
  - Constructor takes `IWebHostEnvironment`, stores `Path.Combine(env.WebRootPath, "img")` as `_imageRoot`
  - `GetTopicDirectory(string topic)` → `Path.Combine(_imageRoot, topic)`
  - `GetImagePath(string topic, string filename)` → `Path.Combine(_imageRoot, topic, filename)`
  - `GetOverlayJsonPath(string topic, string src)` → svg_ prefix + `.overlay.json`
  - `GetOverlayLocalizedJsonPath(string topic, string src, string lang)` → svg_ prefix + `.overlay.{lang}.json`
  - `GetLegacyOverlayPath(string topic, string src)` → `{baseName}.overlay.json`
  - `GetCommentsPath(string topic, string src)` → `{src}.comments.json`
  - `GetDeniedCommentsPath(string topic, string src)` → `{src}.denied.json`
  - `GetStoryJsonCandidates(string topic, string src, CultureInfo ui)` → returns ordered array of fallback paths

**Checkpoint**: ImagePathResolver compiles standalone — no other files modified yet

---

## Phase 2: Foundational Services (Independent — Parallel)

**Purpose**: Create the three services that depend only on `ImagePathResolver` (no cross-service dependencies)

**⚠️ CRITICAL**: These three tasks can be done in parallel — they touch different files with no shared dependencies

- [X] T002 [P] [US2] Create `ImageMetadataService` with story text, image format, and markup methods in Focus2Infinity/Data/ImageMetadataService.cs

  Move from F2IDataService:
  - `GetStoryText(string topic, string src)` → async, uses `ImagePathResolver.GetStoryJsonCandidates()` for culture fallback
  - `DoGetStoryText(string topic, string src, CultureInfo ui)` → private, uses resolver for path construction
  - `ResolveStoryJsonPath(string topic, string src, CultureInfo ui)` → private, replaced by resolver candidates
  - `GetImageFormat(string topic, string src)` → async, uses `ImagePathResolver.GetImagePath()`
  - `DoGetImageFormat(string topic, string src)` → private
  - `Unwrap(string input)` → regex markup transformation (3 patterns: `###text###url###`, `###text~~~url###`, `##url##`)
  - Constructor: inject `ImagePathResolver`

- [X] T003 [P] [US4] Create `OverlayService` with overlay CRUD methods in Focus2Infinity/Data/OverlayService.cs

  Move from F2IDataService:
  - `OverlayExists(string topic, string src)` → async, checks both legacy JPG and new JSON overlay
  - `DoOverlayExists(string topic, string src)` → private, uses `ImagePathResolver.GetImagePath(topic, $"ovl_{src}")`
  - `DoOverlayJsonExists(string topic, string src)` → private, uses `ImagePathResolver.GetOverlayJsonPath()` and `GetLegacyOverlayPath()`
  - `GetOverlayPathForImage(string topic, string src)` → uses `ImagePathResolver.GetOverlayJsonPath()`
  - `SaveOverlayData(string topic, string src, OverlayData data)` → change from sync `void` to `async Task` using `File.WriteAllTextAsync`
  - `GetOverlayData(string topic, string src)` → async
  - `DoGetOverlayData(string topic, string src, CultureInfo ui)` → private, uses resolver for path candidates
  - Keep `OverlayJsonOptions` as private static field
  - Constructor: inject `ImagePathResolver`

- [X] T004 [P] [US3] Create `CommentService` with comment read/write methods in Focus2Infinity/Data/CommentService.cs

  Move from F2IDataService:
  - `GetCommentHistory(string topic, string src)` → async, uses `ImagePathResolver.GetCommentsPath()`
  - `AddComment(string topic, string src, CommentItem comment, bool isValid)` → async, uses `ImagePathResolver.GetCommentsPath()` and `GetDeniedCommentsPath()`
  - Constructor: inject `ImagePathResolver`

**Checkpoint**: All three services compile standalone alongside existing F2IDataService — no consumers updated yet

---

## Phase 3: User Story 1 — Gallery Navigation (Priority: P1)

**Goal**: Create ImageCatalogService that depends on ImageMetadataService, with async fix for `.Result` calls

**Independent Test**: Build succeeds; at runtime Home page loads all images sorted by date, topic pages show images, prev/next navigation works

- [X] T005 [US1] Create `ImageCatalogService` with topic listing, sorting, and navigation methods in Focus2Infinity/Data/ImageCatalogService.cs

  Move from F2IDataService:
  - `GetMainTopics()` → async (keep existing list: Galaxies, Nebulae, Clusters, etc.)
  - `GetSubTopicsSorted(string mainTopic, IStringLocalizer L)` → async, uses `AnnotateWithDate`
  - `GetSubTopics(string mainTopic)` → private, uses `ImagePathResolver.GetTopicDirectory()` for directory listing
  - `AnnotateWithDate(IEnumerable<FileInfo> files, string mainTopic, IStringLocalizer L)` → **CHANGE to `async Task<List<Tuple<DateTime, string>>>`** — use `await _imageMetadataService.GetStoryText()` instead of `.Result`
  - `ToSortedList(List<Tuple<DateTime, string>> list)` → private helper
  - `GetAllTopics(IStringLocalizer L)` → **CHANGE: remove `Task.Run` + `.Result` wrapper** — directly `await GetMainTopics()` and `await AnnotateWithDate()`
  - `GetPreviosNextReferences(string topic, string name, string context, IStringLocalizer<SharedResource> localizer)` → async, preserve existing prev/next logic
  - Constructor: inject `ImagePathResolver` and `ImageMetadataService`

**Checkpoint**: ImageCatalogService compiles. Both `.Result` calls are eliminated. All 5 new service files exist.

---

## Phase 4: DI Wiring (Priority: P1)

**Goal**: Register all new services and remove old registration in Program.cs

- [X] T006 [US1] Update DI registrations in Focus2Infinity/Program.cs — replace `F2IDataService` singleton with 5 new registrations

  Replace:
  ```csharp
  builder.Services.AddSingleton<F2IDataService>();
  ```
  With (order matters for dependency resolution):
  ```csharp
  builder.Services.AddSingleton<ImagePathResolver>();
  builder.Services.AddSingleton<ImageMetadataService>();
  builder.Services.AddSingleton<ImageCatalogService>();
  builder.Services.AddSingleton<OverlayService>();
  builder.Services.AddSingleton<CommentService>();
  ```
  Add `using Focus2Infinity.Data;` if not already present.

**Checkpoint**: Program.cs compiles — old service removed, new services registered

---

## Phase 5: Consumer Updates — Gallery Navigation (Priority: P1) 🎯 MVP

**Goal**: Update all Razor components that use gallery/metadata/overlay services — these are the P1 user stories

- [X] T007 [P] [US1] Update `Home.razor` injection — replace `F2IDataService` with `ImageCatalogService` in Focus2Infinity/Components/Pages/Home.razor

  - Replace `@inject F2IDataService f2iDataService` with `@inject ImageCatalogService imageCatalogService`
  - Update call: `f2iDataService.GetAllTopics(L)` → `imageCatalogService.GetAllTopics(L)`

- [X] T008 [P] [US1] Update `Cmp_Topic.razor` injection — replace `F2IDataService` with `ImageCatalogService` in Focus2Infinity/Components/Cmp_Topic.razor

  - Replace `@inject F2IDataService f2iDataService` with `@inject ImageCatalogService imageCatalogService`
  - Update call: `f2iDataService.GetSubTopicsSorted(Topic, L)` → `imageCatalogService.GetSubTopicsSorted(Topic, L)`

- [X] T009 [P] [US1] Update `NavMenu.razor` injection — replace `F2IDataService` with `ImageCatalogService` in Focus2Infinity/Components/Layout/NavMenu.razor

  - Replace `@inject F2IDataService f2iDataService` with `@inject ImageCatalogService imageCatalogService`
  - Update call: `f2iDataService.GetMainTopics()` → `imageCatalogService.GetMainTopics()`

- [X] T010 [P] [US2] Update `Cmp_ImgHead.razor` injection — replace `F2IDataService` with `ImageMetadataService` in Focus2Infinity/Components/Cmp_ImgHead.razor

  - Replace `@inject F2IDataService f2iDataService` with `@inject ImageMetadataService imageMetadataService`
  - Update calls: `f2iDataService.GetStoryText(...)` → `imageMetadataService.GetStoryText(...)`
  - Update calls: `f2iDataService.Unwrap(...)` → `imageMetadataService.Unwrap(...)`

- [X] T011 [P] [US2] Update `Cmp_TextCard.razor` injection — replace `F2IDataService` with `ImageMetadataService` in Focus2Infinity/Components/Cmp_TextCard.razor

  - Replace `@inject F2IDataService f2iDataService` with `@inject ImageMetadataService imageMetadataService`
  - Update calls: `f2iDataService.GetStoryText(...)` → `imageMetadataService.GetStoryText(...)`
  - Update calls: `f2iDataService.Unwrap(...)` → `imageMetadataService.Unwrap(...)`

- [X] T012 [P] [US1] [US4] Update `Cmp_ImgStory.razor` injection — replace `F2IDataService` with `OverlayService` in Focus2Infinity/Components/Cmp_ImgStory.razor

  - Replace `@inject F2IDataService f2iDataService` with `@inject OverlayService overlayService`
  - Update call: `f2iDataService.OverlayExists(...)` → `overlayService.OverlayExists(...)`

- [X] T013 [US1] [US2] [US4] Update `ImageDetails.razor` injection — replace `F2IDataService` with 3 focused services in Focus2Infinity/Components/Pages/ImageDetails.razor

  - Replace `@inject F2IDataService f2iDataService` with:
    - `@inject ImageCatalogService imageCatalogService`
    - `@inject ImageMetadataService imageMetadataService`
    - `@inject OverlayService overlayService`
  - Update calls:
    - `f2iDataService.GetStoryText(...)` → `imageMetadataService.GetStoryText(...)`
    - `f2iDataService.GetImageFormat(...)` → `imageMetadataService.GetImageFormat(...)`
    - `f2iDataService.OverlayExists(...)` → `overlayService.OverlayExists(...)`
    - `f2iDataService.GetOverlayData(...)` → `overlayService.GetOverlayData(...)`
    - `f2iDataService.GetPreviosNextReferences(...)` → `imageCatalogService.GetPreviosNextReferences(...)`

**Checkpoint**: All P1 user story consumers updated — gallery navigation and metadata display work with new services

---

## Phase 6: Consumer Updates — Comments & Editor (Priority: P2)

**Goal**: Update remaining P2 consumer components

- [X] T014 [P] [US3] Update `Cmp_Commentator.razor` injection — replace `F2IDataService` with `CommentService` in Focus2Infinity/Components/Cmp_Commentator.razor

  - Replace `@inject F2IDataService f2iDataService` with `@inject CommentService commentService`
  - Update calls:
    - `f2iDataService.GetCommentHistory(...)` → `commentService.GetCommentHistory(...)`
    - `f2iDataService.AddComment(...)` → `commentService.AddComment(...)`

- [X] T015 [P] [US4] Update `Editor.razor` injection — replace `F2IDataService` with `ImageCatalogService` and `OverlayService` in Focus2Infinity/Components/Pages/Editor.razor

  - Replace `@inject F2IDataService f2iDataService` with:
    - `@inject ImageCatalogService imageCatalogService`
    - `@inject OverlayService overlayService`
  - Update calls:
    - `f2iDataService.GetMainTopics()` → `imageCatalogService.GetMainTopics()`
    - `f2iDataService.GetOverlayData(...)` → `overlayService.GetOverlayData(...)`
    - `f2iDataService.SaveOverlayData(...)` → `await overlayService.SaveOverlayData(...)` (now async)

**Checkpoint**: All 9 consumer components updated — zero references to F2IDataService remain in Razor files

---

## Phase 7: Cleanup & Verification

**Purpose**: Remove the old monolith and verify the build

- [X] T016 Delete the old `F2IDataService.cs` file at Focus2Infinity/Data/F2IDataService.cs

  Remove the file. Verify no remaining `using` or `@inject` references to `F2IDataService` exist anywhere in the solution.

- [X] T017 Run `dotnet build` and verify zero errors and zero warnings related to the refactoring

  - Run `dotnet build Focus2Infinity/Focus2Infinity.csproj`
  - Verify: zero errors, zero new warnings
  - Verify: no `.Result` or `.Wait()` calls remain in the codebase (grep check)
  - Verify: no references to `F2IDataService` remain anywhere

---

## Dependencies

```
T001 (ImagePathResolver)
├── T002 (ImageMetadataService)    ─┐
├── T003 (OverlayService)          ─┤ Parallel
├── T004 (CommentService)          ─┘
│
└── T005 (ImageCatalogService)     ─── depends on T001 + T002
    │
    └── T006 (Program.cs DI)
        │
        ├── T007-T013 (P1 consumers) ─┐ Parallel
        │                             │
        ├── T014-T015 (P2 consumers) ─┘ Parallel
        │
        └── T016 (Delete old)
            │
            └── T017 (Build verify)
```

## Parallel Execution Examples

**Maximum parallelism in Phase 2**: T002, T003, T004 can all execute simultaneously (different files, shared dependency T001 already complete)

**Maximum parallelism in Phase 5**: T007, T008, T009, T010, T011, T012 can all execute simultaneously (different .razor files, no shared state)

**Maximum parallelism in Phase 6**: T014, T015 can execute simultaneously

## Implementation Strategy

- **MVP**: Complete through Phase 5 (T001–T013) — gallery navigation and metadata display fully working with new services
- **Full delivery**: Complete Phase 6 (T014–T015) — comments and editor updated
- **Verification**: Phase 7 (T016–T017) — old file removed, clean build confirmed
- **Total tasks**: 17
- **Estimated parallelizable**: 11 of 17 tasks can run in parallel with other tasks
