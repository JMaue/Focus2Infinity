# Feature Specification: Split F2IDataService into Focused Services

**Feature Branch**: `001-split-data-service`
**Created**: 2026-04-16
**Status**: Draft
**Input**: Constitution compliance analysis revealed F2IDataService (374 lines) holds 5 responsibilities, violates Single Responsibility Principle (Constitution VIII), contains 2 blocking `.Result` calls (Constitution II), and lacks structured logging (Constitution IV).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Gallery Navigation Continues Working (Priority: P1)

As a visitor, I can browse topics, view sorted image lists, and navigate between images exactly as before the refactoring — the split is invisible to me.

**Why this priority**: This is the core user journey. Any regression here breaks the entire site.

**Independent Test**: Navigate to Home, click a topic (e.g., Galaxies), verify images appear sorted by date descending. Click an image, verify previous/next navigation works. Verify the NavMenu loads all topics.

**Acceptance Scenarios**:

1. **Given** the site is running, **When** I visit the Home page, **Then** all images from all topics load sorted by date descending
2. **Given** I'm on a topic page (e.g., /galaxies), **When** the page loads, **Then** images appear sorted by date descending
3. **Given** I'm viewing image details, **When** I click previous/next, **Then** navigation cycles through images correctly
4. **Given** the NavMenu renders, **When** topics load, **Then** all 12 topic names appear as navigation links

---

### User Story 2 - Image Metadata and Story Display (Priority: P1)

As a visitor, I can view image details with localized metadata (headlines, dates, technical info) and the custom markup links render correctly.

**Why this priority**: Image metadata is the core content of the site — equal priority with navigation.

**Independent Test**: Navigate to an image detail page, verify headline, date, location, and technical metadata display. Switch language, verify localized content loads. Verify `###link###` markup renders as HTML links.

**Acceptance Scenarios**:

1. **Given** I view image details for an image with `.de.json` metadata, **When** my language is German, **Then** the German metadata displays
2. **Given** I view image details for an image without French metadata, **When** my language is French, **Then** the neutral JSON metadata displays (fallback)
3. **Given** metadata contains `###Text###url###` markup, **When** the detail page renders, **Then** styled anchor tags appear

---

### User Story 3 - Comment System Continues Working (Priority: P2)

As a visitor, I can view existing comments and submit new comments that are moderated by AI, with proper email notifications and storage.

**Why this priority**: Important but secondary to image browsing. Isolated from gallery navigation.

**Independent Test**: Navigate to an image with comments, verify they display. Submit a new comment, verify moderation runs, toast notification appears, and comment persists in `.comments.json` or `.denied.json`.

**Acceptance Scenarios**:

1. **Given** an image has approved comments, **When** I view the image details, **Then** comments load and display
2. **Given** I submit a valid comment, **When** AI moderation approves it, **Then** it saves to `.comments.json` and success toast shows
3. **Given** I submit a spam comment, **When** AI moderation rejects it, **Then** it saves to `.denied.json` and rejection toast shows with reason

---

### User Story 4 - Overlay Editor Continues Working (Priority: P2)

As a content author, I can use the overlay editor to create SVG annotations on images, save them, and see them render on the image detail page.

**Why this priority**: Editor is an internal tool, used less frequently than gallery browsing.

**Independent Test**: Open `/editor/{topic}/{name}`, draw a line, circle, and text. Save. Navigate to the image detail page, verify the SVG overlay renders.

**Acceptance Scenarios**:

1. **Given** I open the overlay editor for an image, **When** existing overlay data exists, **Then** it loads and displays
2. **Given** I draw elements and click Save, **When** I navigate to the image detail page, **Then** the overlay renders
3. **Given** I view an image detail page, **When** the image has SVG overlay data, **Then** `OverlayExists` returns true and overlay renders

---

### Edge Cases

- What happens when `ImagePathResolver` receives a topic with path traversal characters (e.g., `../`)? Must sanitize or reject.
- What happens when `ImageCatalogService` calls `ImageMetadataService` for a non-existent image? Must return fallback `ImageStory` (existing behavior preserved).
- What happens when overlay JSON is malformed? `OverlayService` must return `null` gracefully (existing behavior preserved).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST split `F2IDataService` into four focused services: `ImageCatalogService`, `ImageMetadataService`, `OverlayService`, `CommentService`
- **FR-002**: System MUST introduce `ImagePathResolver` as shared infrastructure to construct `wwwroot/img/{topic}/...` paths, eliminating duplicated `Path.Combine(root, "img", topic, ...)` calls across services
- **FR-003**: `ImageCatalogService` MUST own: `GetMainTopics`, `GetSubTopicsSorted`, `GetAllTopics`, `GetPreviosNextReferences`, and private helpers `GetSubTopics`, `AnnotateWithDate`, `ToSortedList`
- **FR-004**: `ImageMetadataService` MUST own: `GetStoryText`, `GetImageFormat`, `Unwrap`, and private helpers `DoGetStoryText`, `DoGetImageFormat`, `ResolveStoryJsonPath`
- **FR-005**: `OverlayService` MUST own: `OverlayExists`, `GetOverlayPathForImage`, `SaveOverlayData`, `GetOverlayData`, and private helpers `DoOverlayExists`, `DoOverlayJsonExists`, `DoGetOverlayData`
- **FR-006**: `CommentService` MUST own: `GetCommentHistory`, `AddComment`
- **FR-007**: `ImagePathResolver` MUST encapsulate `Path.Combine(_hostingEnvironment.WebRootPath, "img", topic, ...)` path construction used by all four services
- **FR-008**: All `.Result` blocking calls MUST be eliminated — `AnnotateWithDate` MUST become async
- **FR-009**: All four services MUST be registered as singletons in `Program.cs`
- **FR-010**: All Razor component `@inject` directives MUST be updated to inject the appropriate focused service
- **FR-011**: `F2IDataService.cs` MUST be deleted after migration is complete
- **FR-012**: Application MUST build and run without errors after refactoring

### Key Entities

- **ImagePathResolver**: Shared utility injected into all four services. Encapsulates `IWebHostEnvironment.WebRootPath` and provides methods like `GetImagePath(topic, filename)`, `GetTopicDirectory(topic)`, etc.
- **ImageCatalogService**: Gallery navigation — topic listing, image sorting, prev/next references. Depends on `ImageMetadataService` for date-based sorting.
- **ImageMetadataService**: Image metadata resolution — JSON loading with culture fallback, image dimensions, markup transformation.
- **OverlayService**: SVG overlay CRUD — read/write overlay JSON, existence checks.
- **CommentService**: Comment persistence — read/write `.comments.json` and `.denied.json` files.

### Consumer-to-Method Mapping (Injection Updates)

| Consumer | Current `F2IDataService` methods used | New service(s) to inject |
|---|---|---|
| `Home.razor` | `GetAllTopics` | `ImageCatalogService` |
| `Cmp_Topic.razor` | `GetSubTopicsSorted` | `ImageCatalogService` |
| `NavMenu.razor` | `GetMainTopics` | `ImageCatalogService` |
| `ImageDetails.razor` | `GetStoryText`, `GetImageFormat`, `OverlayExists`, `GetOverlayData`, `GetPreviosNextReferences` | `ImageCatalogService`, `ImageMetadataService`, `OverlayService` |
| `Cmp_ImgHead.razor` | `GetStoryText`, `Unwrap` | `ImageMetadataService` |
| `Cmp_TextCard.razor` | `GetStoryText`, `Unwrap` | `ImageMetadataService` |
| `Cmp_ImgStory.razor` | `OverlayExists` | `OverlayService` |
| `Cmp_Commentator.razor` | `GetCommentHistory`, `AddComment` | `CommentService` |
| `Editor.razor` | `GetMainTopics`, `GetOverlayData`, `SaveOverlayData` | `ImageCatalogService`, `OverlayService` |
| `Program.cs` | `AddSingleton<F2IDataService>()` | 5 registrations (4 services + resolver) |

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: No single service class exceeds 200 lines of code (well within 500-line constitution limit)
- **SC-002**: Zero `.Result` or `.Wait()` calls remain in the codebase
- **SC-003**: `dotnet build` succeeds with zero errors and zero warnings related to the refactoring
- **SC-004**: All existing user journeys (gallery, detail, comments, editor, overlay) function identically to pre-refactoring behavior
- **SC-005**: Each service has exactly one reason to change (Single Responsibility verified)
- **SC-006**: `ImagePathResolver` eliminates all duplicated `Path.Combine(root, "img", ...)` patterns

## Assumptions

- The existing runtime behavior is correct and serves as the specification for post-refactoring validation
- No new features are added during this refactoring — pure structural change
- `ImageCatalogService` may depend on `ImageMetadataService` (for date-based sorting) — this cross-service dependency is acceptable
- All services continue to use `IWebHostEnvironment` indirectly through `ImagePathResolver`
- The `Unwrap` method logically belongs with `ImageMetadataService` because it transforms metadata content for display
