---
name: Dual Boot Switcher
description: A compact Windows utility styled with the ui-modern-dash palette.
colors:
  brand: "#6366F1"
  primary-action: "#6265F0"
  primary-hover: "#4F46E5"
  primary-pressed: "#4338CA"
  secondary: "#818CF8"
  selected: "#E0E7FF"
  canvas: "#F8FAFC"
  surface: "#FFFFFF"
  ink: "#1E293B"
  muted: "#64748B"
  border: "#E2E8F0"
  disabled: "#F1F5F9"
typography:
  headline:
    fontFamily: "Segoe UI, Microsoft YaHei UI, sans-serif"
    fontSize: "20px"
    fontWeight: 700
    lineHeight: 1.2
  title:
    fontFamily: "Segoe UI, Microsoft YaHei UI, sans-serif"
    fontSize: "17px"
    fontWeight: 700
    lineHeight: 1.2
  body:
    fontFamily: "Segoe UI, Microsoft YaHei UI, sans-serif"
    fontSize: "13px"
    fontWeight: 400
    lineHeight: 1.4
  label:
    fontFamily: "Segoe UI, Microsoft YaHei UI, sans-serif"
    fontSize: "12px"
    fontWeight: 600
    lineHeight: 1.2
rounded:
  surface: "8px"
  control: "10px"
  badge: "9px"
motion:
  press: "100ms"
  state: "180ms"
  easing: "ease-out-quart"
  reducedMotion: "Windows client-area animation setting"
spacing:
  sm: "8px"
  md: "16px"
  lg: "28px"
components:
  button-primary:
    backgroundColor: "{colors.primary-action}"
    textColor: "{colors.surface}"
    rounded: "{rounded.control}"
    height: "40px"
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.control}"
    height: "40px"
---

# Design System: Dual Boot Switcher

## 1. Overview

**Creative North Star: "Modern Dash Control"**

The visual source of truth is the [ui-modern-dash palette](https://www.ysdaima.com/palettes/ui-modern-dash/). Its light slate canvas, white surfaces, indigo actions, pale-indigo selection states, and deep slate typography are mapped onto a compact Windows control surface.

The utility keeps one status surface and one scan-friendly boot table. It borrows the reference's palette, spacing, light header, and restrained rounding without copying its marketing preview or dashboard card density. Rounded surfaces use antialiased GDI+ paths instead of hard control-region clipping. The user-provided dual-boot mark is embedded into the executable and reused in the title bar.

## 2. Colors

The five reference colors are `#6366F1`, `#818CF8`, `#E0E7FF`, `#F8FAFC`, and `#1E293B`. Slate support colors from the same UI family provide readable secondary text and borders.

### Primary
- **Dashboard Indigo** (`#6366F1`): product mark, focus identity, and active state.
- **Accessible Action Indigo** (`#6265F0`): one-step darkened action fill so white button text reaches WCAG AA.
- **Hover Indigo** (`#4F46E5`): primary hover and text-on-pale-indigo state.
- **Pressed Indigo** (`#4338CA`): primary press feedback.

### Secondary
- **Soft Indigo** (`#818CF8`): secondary brand detail in the application icon.
- **Selected Indigo Wash** (`#E0E7FF`): selected boot row, administrator badge, and partition metadata background.

### Neutral
- **Dashboard Canvas** (`#F8FAFC`): application background and table header.
- **Clear Surface** (`#FFFFFF`): header, status panel, data rows, and secondary buttons.
- **Deep Slate** (`#1E293B`): primary text.
- **Slate Label** (`#64748B`): secondary text and disabled labels.
- **Slate Border** (`#E2E8F0`): dividers and control boundaries.

**The Indigo Restraint Rule.** Indigo is reserved for the logo, primary action, selected row, and explicit status. It is not general decoration.

## 3. Typography

**Display Font:** Segoe UI, with Microsoft YaHei UI fallback.
**Body Font:** Segoe UI, with Microsoft YaHei UI fallback.

**Character:** Native, compact, and legible at Windows desktop DPI settings. Headings use weight rather than oversized type.

### Hierarchy
- **Headline** (700, 20px, 1.2): current default system name.
- **Title** (700, 17px, 1.2): application identity in the header.
- **Body** (400, 13px, 1.4): boot-entry data and confirmation text.
- **Label** (600, 12px, 1.2): table headers, status labels, and action state.

## 4. Elevation

Depth is created with `#F8FAFC` behind white surfaces and one-pixel `#E2E8F0` boundaries. No decorative shadows are used in the desktop utility.

## 5. Components

### Buttons
- **Shape:** antialiased 10px logical corners, scaled with the drawing DPI, and a fixed 40px logical height.
- **Primary:** Accessible Action Indigo with white text, Hover Indigo on hover, and Pressed Indigo on press.
- **Secondary:** white surface, Slate Border, and Deep Slate text.
- **Disabled:** pale slate background with written disabled state.

### Boot Table
- **Style:** white rows, a Dashboard Canvas header, an antialiased 8px outer boundary, and a full-row selection model.
- **Columns:** boot system, partition, saved purpose remark, and written state.
- **State:** the default system receives written status; the selected target eases into the pale-indigo wash.

### Remarks
- **Entry point:** `编辑备注` button or double-clicking a boot row.
- **Storage:** current-user registry under `Software\DualBootSwitcher\BootRemarks`, keyed by the BCD identifier.
- **Safety:** remarks never call `bcdedit`; only the explicit default/restart actions modify BCD.

### Current Default Surface
- **Style:** a white 8px-radius surface on the slate canvas.
- **Content:** a label, system name, and pale-indigo partition badge with full text available by tooltip.

## 6. Motion

- **Timing:** 100ms for press depth and 180ms for hover, enable/disable, row selection, and status text changes.
- **Easing:** ease-out-quart, with no bounce, elastic motion, or decorative entrance sequence.
- **Performance:** timers update only the active control and stop at completion; controls use double-buffered painting.
- **Reduced motion:** when Windows disables client-area animations, every state is applied immediately.

## 7. Do's and Don'ts

### Do:
- **Do** use the exact five-color reference palette as the visual anchor.
- **Do** keep at least 28px outer spacing and one clear primary action.
- **Do** pair indigo state color with written labels such as `当前默认` and `可切换`.
- **Do** keep the embedded user-provided dual-boot mark at a stable 42px title-bar size with transparent corners.
- **Do** reserve motion for feedback and state continuity.

### Don't:
- **Don't** copy the reference site's marketing Hero, advertisements, or card-heavy dashboard composition.
- **Don't** introduce colors outside the indigo and slate families without semantic need.
- **Don't** use gradient text, glass effects, neon glow, or saturated inactive controls.
- **Don't** add page-load choreography, bouncing, or animation longer than 250ms.
