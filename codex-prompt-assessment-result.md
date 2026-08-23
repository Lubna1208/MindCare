# Codex Prompt — MindCare "Assessment Result" Page Redesign

Copy everything below the line and paste it into Codex.

---

You are working inside the existing ASP.NET Core MVC project **MindCare**. The shared layout (`_Layout.cshtml`), navbar/footer, and the CSS design system (`--mc-mist`, `--mc-pale`, `--mc-soft`, `--mc-teal`, `--mc-deep` variables + fonts) in `wwwroot/css/site.css` have **already been redesigned in previous steps** (Dashboard, Mood Tracking, Mood History, Assessment form, Resources, Home, and Privacy pages are done) — do not redefine the color variables or redo the navbar/footer. Reuse the existing variables and fonts as they are now, and keep this page visually consistent with the Wellbeing Assessment form page it follows.

Your task now is **visual/UI redesign only** for the **Assessment Result** page. Do not touch any backend logic.

## Files in scope
- `MindCare/Views/Assessment/Result.cshtml` (main page to redesign)
- `MindCare/wwwroot/css/site.css` (append new page-scoped CSS rules here if needed — do not remove or overwrite any existing `--mc-*` variables/rules from the earlier redesigns)

## Absolute constraints (must follow strictly)
1. **Do NOT change any C# code** — no edits to Controllers, Models, ViewModels, Data, Services, Program.cs, or Migrations.
2. **Do NOT change any Razor logic or bindings.** Keep exactly as-is:
   - `@model MindCare.ViewModels.AssessmentResultViewModel`
   - `@Model.Score`, `@AssessmentQuestionnaire.MaximumScore`, `@Model.RiskLevel`, `@Model.Recommendation` bindings
   - The `@if (Model.IsHighRisk)` conditional block and its exact text content
   - `asp-action="History"` and `asp-controller="Dashboard" asp-action="Index"` links
3. **Do NOT remove or rename any existing attribute.**
4. You are only allowed to:
   - Add/change CSS classes on existing elements
   - Add new wrapper `<div>`/`<section>` elements purely for layout/styling
   - Add new CSS rules
   - Add icons/spacing/shadows for visual clarity
5. If unsure whether a change affects functionality, don't make that change.

## Important sensitivity note
This page shows results of a mental-health screening (score, risk level, recommendation, and — when `Model.IsHighRisk` is true — a warning encouraging the user to seek professional support). This content must remain **clearly visible, legible, and appropriately prominent** — do not visually de-emphasize, hide, shrink, or bury the `IsHighRisk` warning alert or the disclaimer paragraph. Styling should feel calm and supportive, never alarming or cold, but the important safety information must stay easy to notice and read.

## Design brief
Calm, light, mental-health-appropriate design — reuse the exact same palette and typography already established (`--mc-mist` background, `--mc-pale`/white card surfaces, `--mc-soft` secondary accents, `--mc-teal` primary/active states, `--mc-deep` headings/text, serif headings + sans-serif body).

### Specific improvements for this page
- Wrap the page in the same centered container width used on other form/result pages for consistency.
- Restyle the main result `card`: soft rounded corners, subtle shadow, `--mc-pale` or white background. Consider visually separating "Score", "Wellbeing indication", and "Recommendation" into clearer sub-sections within the card (e.g. a small label + larger value for Score, a colored badge/pill for the Wellbeing indication using `--mc-soft`/`--mc-teal` tones for lower-concern results, or a slightly warmer but still calm tone for higher-concern results — do not use harsh red/alarming colors even for high-risk indications; keep it supportive, e.g. a soft amber/terracotta rather than bright red), and a clearly separated "Recommendation" section with its own small heading style.
- If you add a score visual (e.g. a simple progress bar or ring showing `Model.Score` out of `AssessmentQuestionnaire.MaximumScore`), it must be purely decorative/CSS-driven using the existing Razor-rendered numeric values already in the markup (e.g. inline `style="width: ...%"` calculated from the already-rendered `@Model.Score`/`@AssessmentQuestionnaire.MaximumScore` values in Razor, since these are just being displayed, not recalculated) — do not add any new C# logic to compute this; if you're not confident this can be done safely with pure Razor expression + CSS without any backend change, skip the visual and keep the plain text score display instead.
- Restyle the `IsHighRisk` warning alert (currently Bootstrap `alert-warning`) to feel calm-but-clearly-noticeable — e.g. a soft warm background (light amber/terracotta, not harsh yellow), a small icon (heart, hand, or info icon), rounded corners matching the design system. It must remain visually distinct/prominent from the rest of the page, not minimized.
- Restyle the disclaimer paragraph ("This assessment cannot diagnose...") as a clearly legible muted note — soft `--mc-deep` at reduced opacity or a light bordered box — but keep it fully readable, not faded to the point of being missed.
- Restyle the "View Assessment History" button as a solid `--mc-teal` button with white text (currently default Bootstrap `btn-primary` blue), and "Back to Dashboard" as a calm outlined secondary button using `--mc-deep`/`--mc-soft`, both with rounded corners and comfortable spacing, matching the button styles used elsewhere in the app.
- Ensure the page is fully responsive and readable on mobile.

## Deliverable
Update `Views/Assessment/Result.cshtml` and (if needed) add new rules to `wwwroot/css/site.css`, matching the already-established MindCare design system, while keeping all safety-relevant content clearly visible. Do not touch any other file, and do not alter any existing functionality, data bindings, or C# logic — this is a pure CSS/HTML styling task.
