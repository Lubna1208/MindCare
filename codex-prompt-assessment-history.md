# Codex Prompt — MindCare "Assessment History" Page Redesign

Copy everything below the line and paste it into Codex.

---

You are working inside the existing ASP.NET Core MVC project **MindCare**. The shared layout (`_Layout.cshtml`), navbar/footer, and the CSS design system (`--mc-mist`, `--mc-pale`, `--mc-soft`, `--mc-teal`, `--mc-deep` variables + fonts) in `wwwroot/css/site.css` have **already been redesigned in previous steps** (Dashboard, Mood Tracking, Mood History, Assessment form, Assessment Result, Resources, Home, and Privacy pages are done) — do not redefine the color variables or redo the navbar/footer. Reuse the existing variables and fonts as they are now, and keep this page visually consistent with the Mood History page (same pattern: a table listing past entries) and the Assessment Result page it links to.

Your task now is **visual/UI redesign only** for the **Assessment History** page. Do not touch any backend logic.

## Files in scope
- `MindCare/Views/Assessment/History.cshtml` (main page to redesign)
- `MindCare/wwwroot/css/site.css` (append new page-scoped CSS rules here if needed — do not remove or overwrite any existing `--mc-*` variables/rules from the earlier redesigns)

## Absolute constraints (must follow strictly)
1. **Do NOT change any C# code** — no edits to Controllers, Models, ViewModels, Data, Services, Program.cs, or Migrations.
2. **Do NOT change any Razor logic or bindings.** Keep exactly as-is:
   - `@model IEnumerable<MindCare.ViewModels.AssessmentResultViewModel>`
   - `asp-action="Index"` link ("Take Assessment")
   - The `@if (Model.Any())` / `else` block
   - The `@foreach (var assessment in Model)` loop and all bindings: `@assessment.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")`, `@assessment.Score`, `@AssessmentQuestionnaire.MaximumScore`, `@assessment.RiskLevel`
   - `asp-action="Result"`, `asp-route-id="@assessment.Id"` link ("View result")
3. **Do NOT remove or rename any existing attribute.**
4. You are only allowed to:
   - Add/change CSS classes on existing elements
   - Add new wrapper `<div>`/`<section>` elements purely for layout/styling
   - Add new CSS rules
   - Add icons/spacing/shadows/badges for visual clarity
5. If unsure whether a change affects functionality, don't make that change.

## Design brief
Calm, light, mental-health-appropriate design — reuse the exact same palette and typography already established (`--mc-mist` background, `--mc-pale`/white card surfaces, `--mc-soft` secondary accents, `--mc-teal` primary/active states, `--mc-deep` headings/text, serif headings + sans-serif body). Keep this page's visual pattern consistent with how the Mood History page was redesigned (same table/list treatment approach), since both are "history" pages in this app.

### Specific improvements for this page
- Wrap the page content in a centered container with comfortable max-width, consistent with other redesigned pages.
- Restyle the header row: "Assessment History" in the established serif heading style, and restyle the "Take Assessment" button (currently default Bootstrap `btn-primary` blue) as a solid `--mc-teal` button with white text, rounded corners, hover-darken toward `--mc-deep`.
- Redesign the history table for a calmer feel, matching whatever treatment was applied to the Mood History table (rounded-corner container, soft `--mc-pale` header background, `--mc-deep` header text, generous cell padding, subtle row dividers instead of harsh Bootstrap striping, soft hover-highlight row).
- Style the "Wellbeing indication" value (`@assessment.RiskLevel`) as a small colored badge/pill (e.g. `--mc-soft`/`--mc-teal` tones for lower-concern levels, a soft warm amber/terracotta — never harsh red — for higher-concern levels) so users can scan their history at a glance, consistent with how the indication was styled on the Assessment Result page.
- Restyle the "View result" link as a clear, calm button or pill-style link using `--mc-teal`, with a hover effect, rather than a plain default blue text link.
- Restyle the empty state ("You have not completed an assessment yet.") as a centered, friendly, softly-styled message (e.g. inside a light `--mc-pale` rounded box) instead of plain muted text — keep the exact same `@if (Model.Any())`/`else` condition and text content.
- Ensure the table/list is responsive and doesn't cause awkward horizontal scrolling on mobile.
- Ensure sufficient color contrast (WCAG AA) throughout, especially for the risk-level badges.

## Deliverable
Update `Views/Assessment/History.cshtml` and (if needed) add new rules to `wwwroot/css/site.css`, matching the already-established MindCare design system and staying visually consistent with the Mood History and Assessment Result pages. Do not touch any other file, and do not alter any existing functionality, data bindings, or C# logic — this is a pure CSS/HTML styling task.
