# Calendar — UI Style Guide

This guide defines the visual language for the Calendar application. It applies to **all
screens** and is the reference for **reviewers**. It is derived from the Stitch *Calendar /
Multi-Color Project Distinctions* prototype.

> **Single source of truth:** all design values live as CSS custom properties in
> [`src/CalendarApi/wwwroot/tokens.css`](../src/CalendarApi/wwwroot/tokens.css). Components in
> [`styles.css`](../src/CalendarApi/wwwroot/styles.css) reference `var(--…)` only. **Never
> hardcode a colour, size, or font** in component CSS. The sole exception is an element's own
> `color` (a per-record data value applied inline).

---

## 1. Principles

- **Flat & sharp.** No rounded corners, no drop shadows. `--radius` is `0`. Separation comes
  from **1px `--color-outline-variant` borders** and surface tints, not elevation.
- **Inter everywhere**, loaded from Google Fonts. Icons are **Material Symbols Outlined**.
- **Royal Blue primary, used sparingly.** `--color-primary` (`#004ac6`) is reserved for the
  primary action and the *today* highlight — not for decoration.
- **Neutral Slate canvas.** Whites and slate greys carry the layout; colour comes from the
  per-element palette (section 5).
- **One deliberate exception to "flat":** the **task shape is a circle**. See section 6.

---

## 2. Color tokens

| Token | Hex | Usage |
|---|---|---|
| `--color-primary` | `#004ac6` | Primary buttons, today border/badge, focus border |
| `--color-primary-hover` | `#003ea8` | Hover for primary surfaces |
| `--color-on-primary` | `#ffffff` | Text/icon on primary |
| `--color-background` / `--color-surface` | `#ffffff` | Page & card backgrounds |
| `--color-surface-container` | `#f8fafc` | Subtle raised surface |
| `--color-surface-container-low` | `#f1f5f9` | Day-list canvas, inputs |
| `--color-surface-container-high` | `#f1f5f9` | Hover fill |
| `--color-surface-container-highest` | `#e2e8f0` | Strongest neutral fill |
| `--color-on-surface` | `#0f172a` | Primary text |
| `--color-on-surface-variant` / `--color-secondary` | `#64748b` | Secondary text, icons, captions |
| `--color-outline` | `#94a3b8` | Stronger dividers, shape hover outline |
| `--color-outline-variant` | `#e2e8f0` | Default 1px borders |
| `--color-inverse-surface` | `#1e293b` | Toast background |
| `--color-error` | `#b91c1c` | Destructive actions, validation |
| `--color-error-container` | `#fee2e2` | Error background |
| `--color-tertiary` | `#943700` | Reserved warm accent |

---

## 3. Typography

Font: **Inter** (`--font-sans`). Each scale step is a `font` shorthand token; pair with a
`--tracking-*` value where noted.

| Token | Size / Weight | Usage |
|---|---|---|
| `--type-display-lg` | 36px / 700 (`--tracking-display`) | Month title (`<h1>`) |
| `--type-display-lg-mobile` | 28px / 700 | Today's day number; month title on mobile |
| `--type-headline-md` | 24px / 600 | Section headlines |
| `--type-title-sm` | 18px / 600 | Brand, modal title, day number |
| `--type-body-md` | 16px / 400 | Body text, buttons |
| `--type-body-sm` | 14px / 400 | Inputs, chips, captions |
| `--type-label-caps` | 12px / 600 (`--tracking-caps`, uppercase) | Weekday, legend title, field labels |
| `--type-label-indicator` | 11px / 700 | TODAY badge, kind badges, group labels |

---

## 4. Spacing & layout

4px base unit. Use the scale — do not invent margins.

`--space-1 4` · `--space-2 8` · `--space-3 12` · `--space-4 16` (gutter) · `--space-6 24` ·
`--space-8 32` (desktop margin) · `--space-10 40`.

Layout tokens: `--topnav-height 64`, `--legend-width 340`, `--content-max-width 960`,
`--legend-breakpoint 1280`. The app is a fixed shell: header + month nav stay put, the day
list scrolls, and the legend scrolls independently.

---

## 5. Element color palette

Per-element `color` is a free-form hex, but **prefer these curated, accessibility-tuned
colours** (white text reads on all of them). Source: the *Isolated Legend Panel* prototype.

| Name | Hex | | Name | Hex |
|---|---|---|---|---|
| Deep Royal Blue | `#002366` | | Charcoal | `#36454f` |
| Cool Slate Gray | `#708090` | | Soft Sage Green | `#8a9a5b` |
| Forest Teal | `#004d40` | | Warm Terracotta | `#c04000` |
| Muted Indigo | `#4b0082` | | Deep Plum | `#673147` |

Defaults when no colour is set: **project → Deep Royal Blue**, **task → Forest Teal**.
Text colour over an element colour is chosen automatically by `contrastText()` in
[`app.js`](../src/CalendarApi/wwwroot/app.js) (black/white by luminance) — always use it.

---

## 6. Component patterns

Build from the canonical classes in [`styles.css`](../src/CalendarApi/wwwroot/styles.css):

- **Top nav** (`.app-header`): brand · `Month` tab · search · icon buttons · avatar. Sticky,
  `--topnav-height`, white, bottom border.
- **Month nav** (`.month-nav`): centered `‹ Title ›` with `.today-btn` pinned right.
- **Day row** (`.day-row`): `.day-cell` (weekday + number) + items. Variants:
  - `.empty` → `.day-empty` "No events scheduled" (italic, secondary).
  - `.today` → 2px primary border, larger primary day number, `.today-badge` top-left,
    items become full-width **`.daybar`** bars (badge + name only — **no dates/times**).
- **Shapes** (`.shape`): 32px, P/T letter, filled with the element colour.
  `.shape.project` = **square**; `.shape.task` = **circle** (`--radius-circle`).
- **Legend** (`.legend-panel`): create actions on top, then `Calendar Legend` with two groups
  — **Active Projects** and **Current Tasks** — each row a shape + name.
- **Buttons**: `.btn.primary` (royal blue), `.btn.outline`, `.btn.danger`, `.ghost`,
  `.icon-btn`, `.fab` (mobile). **Inputs**: 1px border, focus → primary border.
- **Modal / search overlay** (`.backdrop` + `.modal`), **toast** (`.toast`, inverse surface).

---

## 7. Accessibility

- Maintain **WCAG AA** contrast (≥ 4.5:1 for text). Element colours + `contrastText()` are
  tuned for this; verify any new colour.
- Every interactive control is a real `<button>`/`<a>`/`<input>` with a visible focus state
  and an `aria-label` when it shows only an icon.
- Don't rely on colour alone: kind is also conveyed by the **P/T letter** and **shape**.

---

## 8. Known Stitch divergences

Stitch generated the prototypes with two limitations — **do not "correct" the app to match
the prototype** on these:

1. **Tasks are circles.** Stitch forces `border-radius: 0`, so it renders task shapes as
   squares. Our app intentionally uses a **circle** for tasks (square for projects).
2. **Dates, not times.** The prototype shows clock times on today's bars; our domain stores
   **dates only**, so bars show the **name only**.

---

## 9. Reviewer checklist

- [ ] No hardcoded hex/px/font in component CSS — only `var(--…)` tokens (element `color`
      excepted).
- [ ] Flat respected: no `border-radius` (except the task circle) and no `box-shadow`.
- [ ] Type uses a `--type-*` token; spacing uses the `--space-*` scale.
- [ ] Projects render as squares, tasks as circles; both carry the P/T letter.
- [ ] New element colours come from (or match the contrast of) the section 5 palette.
- [ ] Interactive elements are semantic, keyboard-focusable, and labelled.
- [ ] Layout respects the fixed shell and the `--legend-breakpoint` (legend → FAB).
