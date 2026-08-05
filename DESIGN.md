---
name: Dual Boot Switcher
description: A compact Windows utility for choosing the next default boot system.
colors:
  primary: "#466B3F"
  primary-deep: "#35542F"
  accent: "#00736D"
  canvas: "#FFFFFF"
  surface: "#F4F7F3"
  ink: "#202720"
  muted: "#58645A"
  border: "#D7DED8"
  selected: "#E7F0E6"
  disabled: "#E9EDE9"
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
  sm: "0px"
spacing:
  sm: "8px"
  md: "16px"
  lg: "28px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.canvas}"
    rounded: "{rounded.sm}"
    height: "40px"
  button-secondary:
    backgroundColor: "{colors.canvas}"
    textColor: "{colors.ink}"
    rounded: "{rounded.sm}"
    height: "40px"
---

# Design System: Dual Boot Switcher

## 1. Overview

**Creative North Star: "The Quiet Switch Panel"**

This is a practical system utility used at the end of a work session. The dark header establishes a distinct identity, while the operating surface stays light, dense, and familiar. The visual language should feel like a deliberate Windows control surface, not a dashboard or a marketing site.

Key characteristics: a visible current default, a single scan-friendly boot table, restrained moss-green primary actions, and an explicit state line before any system change.

## 2. Colors

The palette is restrained: an olive-green primary derived from `oklch(0.45 0.09 120)`, a teal status accent, pure-white content space, and charcoal-green text.

### Primary
- **Moss Control** (`#466B3F` / `oklch(0.45 0.09 120)`): primary action, application mark, and default-system emphasis.
- **Moss Pressed** (`#35542F`): pressed and hover state for primary actions.

### Secondary
- **Signal Teal** (`#00736D` / approximately `oklch(0.46 0.09 190)`): compact status metadata and the partition chip.

### Neutral
- **Clear Canvas** (`#FFFFFF`): main content background.
- **Quiet Surface** (`#F4F7F3`): current-default band and disabled states.
- **Graphite Green** (`#202720`): primary text.
- **Measured Gray** (`#58645A`): secondary text.
- **Soft Divider** (`#D7DED8`): table and control borders.

**The One Voice Rule.** Use the green primary only for actions, selected rows, and the active default state. Teal identifies compact metadata rather than competing for attention.

## 3. Typography

**Display Font:** Segoe UI, with Microsoft YaHei UI fallback.
**Body Font:** Segoe UI, with Microsoft YaHei UI fallback.

**Character:** Native, compact, and legible at Windows desktop DPI settings. Labels are lighter and smaller than data; headings use weight rather than exaggerated scale.

### Hierarchy
- **Headline** (700, 20px, 1.2): current default system name.
- **Title** (700, 17px, 1.2): application identity in the header.
- **Body** (400, 13px, 1.4): boot-entry data and confirmation text.
- **Label** (600, 12px, 1.2): table headers, status labels, and action state.

## 4. Elevation

The interface is flat by default. Depth comes from the dark header, surface contrast, and one-pixel table boundaries. No decorative shadows are used.

## 5. Components

### Buttons
- **Shape:** native flat Windows rectangles with square corners and a fixed 40px height.
- **Primary:** Moss Control background with white text. Hover uses Moss Pressed.
- **Secondary:** white background, Soft Divider border, Graphite Green text.
- **Disabled:** Quiet Surface background, muted text, and no saturated fill.

### Boot Table
- **Style:** one clear, full-row selection model with 42px rows and a quiet surface header.
- **State:** the default system receives both a written status and emphasis; the selected target receives a pale green selection fill.

### Current Default Band
- **Style:** a low-contrast full-width status band, not a floating card.
- **Content:** a label, system name, and a teal partition chip.

## 6. Do's and Don'ts

### Do:
- **Do** keep a pure-white content canvas and at least 28px outer spacing.
- **Do** use the selected row and written state together to distinguish default and target systems.
- **Do** keep primary actions on the bottom-right in a stable 40px control row.
- **Do** use the embedded monitor-and-cog mark only as product identity.

### Don't:
- **Don't** use marketing-page heroes, gradient text, floating-card stacks, or oversized typography.
- **Don't** use colored side stripes, wide soft shadows, glass effects, or over-rounded controls.
- **Don't** use purple neon, gaming-panel visuals, or a saturated inactive button state.
