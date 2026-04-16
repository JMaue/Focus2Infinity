<!--
Sync Impact Report:
- Version change: 1.0.0 → 1.1.0
- Modified principles: None renamed
- Added sections: Principle VIII (Clean Code Discipline)
- Removed sections: None
- Templates requiring updates:
  - plan-template.md: ✅ Updated (Constitution Check)
  - tasks-template.md: ✅ Updated (compliance checklist)
  - spec-template.md: ✅ No changes needed
- Follow-up TODOs: None
-->

# Focus2Infinity Constitution

## Core Principles

### I. Internationalization by Default (NON-NEGOTIABLE)
Multi-language support is foundational architecture, not a feature addition. Every user-facing string MUST be localized through `IStringLocalizer<SharedResource>`. All content metadata MUST support language fallback chains: `{file}.{culture}.json` → `{file}.{lang}.json` → `{file}.json`. Language switching MUST respect GDPR cookie consent patterns.

**Rationale**: This astrophotography gallery serves an international astronomy community from Day One. Localization retrofitting is architecturally expensive and user experience fragmenting.

### II. Synchronous Operations Forbidden
ALL I/O operations MUST be asynchronous: file reads, API calls, image processing, database operations. Services MUST be registered as singletons for stateless operations. UI rendering methods MUST use `await` patterns. Thread blocking through `.Result` or `.Wait()` is prohibited.

**Rationale**: Blazor Server performance depends on non-blocking request handling. Synchronous operations create thread pool exhaustion and poor user experience under load.

### III. Filesystem as Database Truth
Content and metadata live as version-controllable files: `wwwroot/img/{topic}/{image}.{ext}` with paired JSON metadata. No SQL database required. JSON structure MUST be consistent but optional fields are permitted (graceful degradation). File naming conventions MUST be enforced programmatically.

**Rationale**: Maximizes portability, enables version control of content, eliminates database migration complexity, supports direct file editing workflows.

### IV. Structured Logging for Observability
Serilog MUST be configured before application startup with multiple sinks: Console + File with daily rolling and 30-day retention. Every business operation MUST log with structured context. Request logging MUST capture all HTTP interactions. Error handling MUST log exceptions with context, never swallow silently.

**Rationale**: Production debugging requires trace correlation and historical analysis. Local development benefits from detailed logging without centralized infrastructure dependencies.

### V. Privacy by Design (GDPR Native)
Cookie consent MUST gate persistent feature behavior. Language switching MUST work without cookies via query parameters. User data collection requires explicit consent and audit trails. Content moderation decisions MUST be stored for transparency (`.comments.json` + `.denied.json`).

**Rationale**: GDPR compliance is legal requirement, not optional feature. Privacy-first approach builds user trust and ensures legal operation across EU jurisdictions.

### VI. AI-Enhanced Content Moderation
User-generated content MUST be validated through AI moderation (Claude API) before display. Moderation decisions MUST return structured (bool, reason) tuples. Both approved and rejected content MUST be stored for audit purposes. Moderation failures MUST degrade gracefully, not block functionality.

**Rationale**: Automation reduces manual review burden while maintaining content quality. Transparency in moderation decisions builds community trust.

### VII. Component Isolation and Composition
UI components MUST follow `Cmp_` naming convention for reusability signals. Components MUST accept parameters, not depend on tight coupling. Component composition is preferred over inheritance. Page components orchestrate, `Cmp_` components render specific concerns.

**Rationale**: Testability, reusability, and maintainability require clear separation of concerns. Predictable naming enables rapid codebase navigation.

### VIII. Clean Code Discipline (NON-NEGOTIABLE)
All code MUST adhere to Clean Code principles. Code MUST be readable, self-documenting, and express intent clearly. Specific enforceable limits:

- **Class size**: No class (including Razor `@code` blocks) SHALL exceed **500 lines of code**.
- **Method size**: No single method SHALL exceed **50 lines of code**.
- **Naming**: Variables, methods, and classes MUST have meaningful, intention-revealing names. Abbreviations are prohibited unless domain-standard (e.g., `SMTP`, `API`, `GDPR`).
- **Single Responsibility**: Each class and method MUST have exactly one reason to change.
- **DRY**: Duplicated logic MUST be extracted into shared methods or services.
- **Boy Scout Rule**: Code touched during a change MUST be left cleaner than it was found.
- **No dead code**: Commented-out code, unused variables, and unreachable branches MUST be removed.
- **Small functions**: Methods SHOULD do one thing, do it well, and do it only.

**Rationale**: Clean Code reduces cognitive load, lowers defect rates, and makes the codebase accessible to contributors. Hard limits on class and method size enforce discipline and prevent complexity accumulation.

## Technical Standards

### Technology Stack Requirements
- **.NET 8+** with C# nullable reference types enabled
- **Blazor Server** for interactive UI without JavaScript complexity  
- **Bootstrap 5** for responsive design patterns
- **Serilog** for structured application logging
- **Anthropic Claude API** for content moderation capabilities

### Security Requirements
- **HTTPS enforcement** in production via middleware
- **Antiforgery protection** for all forms  
- **API key management** via external account files (not source controlled)
- **Cookie security** with HttpOnly, Secure, SameSite attributes
- **Error page handling** that exposes request IDs but not implementation details

### Performance Standards  
- **Async/await** for all I/O operations
- **Singleton services** for stateless managers
- **Image format detection** via System.Drawing for correct rendering
- **Static file serving** for optimized image delivery
- **Bootstrap JS loading** for toast notifications and interactive components

## Development Workflow

### Code Organization Requirements
- **Services**: Data access, external APIs, business logic - registered as DI singletons
- **Components**: UI rendering with clear parameter contracts
- **Data Models**: Lightweight with extension methods for computed properties  
- **Controllers**: API endpoints for language switching and external integration

### Error Handling Standards
- **Graceful degradation**: Missing files, invalid JSON, API failures should not crash rendering
- **Fallback chains**: Missing localized content falls back to neutral/default variants
- **Exception logging**: All exceptions logged with context before user-friendly error display
- **Timeout handling**: External API calls must have reasonable timeout limits (120s max)

### File Organization Conventions
- **Image naming**: No `tbn_` or `ovl_` prefixes in gallery listings (programmatically filtered)
- **Metadata structure**: Consistent JSON schema with required fields (Headline, Datum, Ort)
- **Localization files**: `{filename}.{culture}.json` pattern with fallback chain resolution
- **Component naming**: `Cmp_` prefix for reusable components, PascalCase for pages

## Governance

This constitution supersedes all other development practices and guidelines. Any feature development, refactoring, or architectural changes MUST verify compliance with these principles. Code reviews MUST explicitly check for async/await usage, localization completeness, error handling patterns, and logging coverage.

Complexity requiring deviation from these principles MUST be documented and justified in advance through architectural decision records. The `.github/copilot-instructions.md` file provides runtime development guidance and implementation patterns.

**Version**: 1.1.0 | **Ratified**: 2026-04-16 | **Last Amended**: 2026-04-16
