# Codex Prompt — MindCare "Mood History" Page Redesign

Copy everything below the line and paste it into Codex.

---

You are working inside the existing ASP.NET Core MVC project **MindCare**. The shared layout (`_Layout.cshtml`), navbar/footer, and the CSS design system (`--mc-mist`, `--mc-pale`, `--mc-soft`, `--mc-teal`, `--mc-deep` variables + fonts) in `wwwroot/css/site.css` have **already been redesigned in previous steps** (Dashboard, Mood Tracking, Assessment, Resources, Home, and Privacy pages are done) — do not redefine the color variables or redo the navbar/footer. Reuse the existing variables and fonts as they are now, and keep this page visually consistent with those, especially the Mood Tracking page it links back to.

Your task now is **visual/UI redesign only** for the **Mood History** page. Do not touch any backend logic.

## Files in scope
- `MindCare/Views/Mood/History.cshtml` (main page to redesign)
- `MindCare/wwwroot/css/site.css` (append new page-scoped CSS rules here if needed — do not remove or overwrite any existing `--mc-*` variables/rules from the earlier redesigns)

## Absolute constraints (must follow strictly)
1. **Do NOT change any C# code** — no edits to Controllers, Models, ViewModels, Data, Services, Program.cs, or Migrations.
2. **Do NOT change any Razor logic or bindings.** Keep exactly as-is:
   - `@model IEnumerable<MindCare.Models.MoodLog>`
   - `asp-action="Index"` link ("Track Today's Mood")
   - The `@if (TempData["SuccessMessage"] is string successMessage)` block and its exact condition/content binding
   - The `@if (Model.Any())` / `else` block
   - The `@foreach (var moodLog in Model)` loop and all `@moodLog.CreatedAt.ToString("yyyy-MM-dd HH:mm")`, `@moodLog.Mood`, `@(string.IsNullOrWhiteSpace(moodLog.Note) ? "—" : moodLog.Note)` bindings
3. **Do NOT remove or rename any existing `id`, `name`, or attribute.**
4. You are only allowed to:
   - Add/change CSS classes on existing elements
   - Add new wrapper `<div>`/`<section>` elements purely for layout/styling
   - Add new CSS rules
   - Add icons/emoji purely as decoration (e.g. next to mood names), spacing, shadows, etc.
5. If unsure whether a change affects functionality, don't make that change.

## Design brief
Calm, light, mental-health-appropriate design — reuse the exact same palette and typography already established (`--mc-mist` background, `--mc-pale`/white card surfaces, `--mc-soft` secondary accents, `--mc-teal` primary/active states, `--mc-deep` headings/text, serif headings + sans-serif body), matching the Mood Tracking page and other redesigned pages.

### Specific improvements for this page
- Wrap the page content in a centered container with comfortable max-width, consistent with other redesigned pages.
- Restyle the header row: "Mood History" as the established serif heading style, and restyle the "Track Today's Mood" button (currently default Bootstrap `btn-primary` blue) as a solid `--mc-teal` button with white text and rounded corners matching the design system, with hover-darken toward `--mc-deep`.
- Restyle the success alert (`alert-success`) to use a soft palette-consistent style — e.g. `--mc-pale`/`--mc-soft` background with `--mc-deep` text and a thin `--mc-teal` left border or icon — instead of default Bootstrap green, while keeping it clearly recognizable as a positive/success message. Keep the exact same conditional rendering logic (`TempData["SuccessMessage"]`), only restyle its appearance.
- Redesign the mood history table for a calmer feel. You have two good options — pick whichever fits best, or combine them:
  - **Option A (table redesign):** Restyle the `<table>` with rounded-corner container, soft header row background (`--mc-pale`), `--mc-deep` header text, generous cell padding, subtle row-divider lines instead of harsh Bootstrap striping, and a soft hover-highlight row effect.
  - **Option B (card list redesign):** Convert the visual presentation into a stacked list of rounded "mood entry" cards (one per row) showing date, mood (optionally with a small emoji matching the mood, e.g. 🙂 Great, 😞 Very Sad, 😐 Okay — purely decorative, based on the existing `@moodLog.Mood` text, not a data change), and note — while keeping the underlying `<table>`/`@foreach` structure and bindings unchanged if you use CSS to reflow it responsively (e.g. `display: block` techniques or a wrapping approach), OR restructure the markup into `<div>`-based cards as long as every existing `@moodLog.*` binding is preserved exactly and the `@foreach` loop structure/order stays the same.
  Either way, ensure it looks clean, readable, and responsive on mobile (avoid horizontal scrolling table cramping on small screens).
- Restyle the empty state ("You have not saved any moods yet.") as a centered, friendly, softly-styled message (e.g. inside a light `--mc-pale` rounded box, maybe with a small calming icon) instead of plain muted text — keep the exact same `@if (Model.Any())`/`else` condition and text content.
- Ensure sufficient color contrast (WCAG AA) throughout.

## Deliverable
Update `Views/Mood/History.cshtml` and (if needed) add new rules to `wwwroot/css/site.css`, matching the already-established MindCare design system. Do not touch any other file, and do not alter any existing functionality, data bindings, or C# logic — this is a pure CSS/HTML styling task.
