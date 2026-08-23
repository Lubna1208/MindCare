# Codex Prompt — MindCare "Privacy Policy" Page Redesign

Copy everything below the line and paste it into Codex.

---

You are working inside the existing ASP.NET Core MVC project **MindCare**. The shared layout (`_Layout.cshtml`), navbar/footer, and the CSS design system (`--mc-mist`, `--mc-pale`, `--mc-soft`, `--mc-teal`, `--mc-deep` variables + fonts) in `wwwroot/css/site.css` have **already been redesigned in previous steps** (Dashboard, Mood Tracking, Assessment, Resources, and Home pages are done, navbar now uses the real `mindcare-icon.png` logo) — do not redefine the color variables or redo the navbar/footer. Reuse the existing variables and fonts as they are now, and keep this page visually consistent with those.

Your task now is to redesign the **Privacy Policy** page.

## File in scope
- `MindCare/Views/Home/Privacy.cshtml`

This page currently has **no model, no data bindings, and no form logic** — it is fully static placeholder text (`ViewData["Title"]` plus one generic placeholder sentence: "Use this page to detail your site's privacy policy."). Because there is no functional logic on this page at all, you have creative freedom here:
- You MAY fully rewrite the HTML content/copy of this page — this is static content, not application logic, so rewriting it is safe.
- Do NOT add a `@model` directive, do NOT call any controller actions, and do NOT create any new routes.

## Absolute constraints
1. Do NOT change any C# code — no edits to Controllers, Models, ViewModels, Data, Services, Program.cs, or Migrations.
2. Do NOT touch `_Layout.cshtml`, `site.css`, or any other view file in this pass — only `Views/Home/Privacy.cshtml`.
3. Keep `ViewData["Title"] = "Privacy Policy";` as-is.
4. If unsure whether a change affects functionality, don't make that change.

## Content & design goals
Write genuine, sensible **placeholder privacy-policy content** appropriate for a student/academic mental-health web app project (course project — MindCare, CSE 3224, Information System Design & Software Engineering Lab), not a real production legal document. Include reasonable sections such as:
- A short intro paragraph explaining that MindCare respects user privacy and this policy explains what data is collected and how it's used.
- "Information We Collect" — e.g. account details (name, email), mood tracking entries, assessment responses, and resource browsing activity.
- "How We Use Your Information" — e.g. to provide mood tracking and assessment features, personalize resources, and improve the service.
- "Data Storage & Security" — a brief note that data is stored securely and access is limited to authorized roles (User/Counsellor/Admin).
- "Your Rights" — e.g. users can view or request deletion of their data.
- "Contact" — a placeholder note that users can reach out with privacy questions.
Keep the tone calm, reassuring, and clear (mental-health-app appropriate — avoid cold/legalistic tone where possible while still being clear).

### Visual design
- Present the content inside a soft, readable card/container (rounded corners, `--mc-pale` or white background on `--mc-mist` page background, comfortable padding, max-width so line length stays readable, e.g. `max-width: 800px` centered).
- Use the established serif heading font for the page title and section headings, sans-serif body font for paragraphs, with generous line-height for readability.
- Use `--mc-deep` for headings, sensible body text color for good contrast, and `--mc-teal` sparingly for any accent (e.g. section heading underline or icon bullets) if desired.
- Add clear visual separation between sections (spacing, subtle divider, or section icons) so the policy is easy to scan.
- Fully responsive on mobile.

## Deliverable
Rewrite `Views/Home/Privacy.cshtml` as a polished, readable Privacy Policy page matching the established MindCare design system. Do not touch any other file, and do not alter any existing functionality or C# logic.
