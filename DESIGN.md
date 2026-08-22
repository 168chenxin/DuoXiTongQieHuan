---
name: Dual Boot Switcher
description: A single-page Windows boot workspace for safe boot-target selection.
colors:
  brand: "#168C68"
  primary-action: "#168C68"
  primary-hover: "#0F6F52"
  primary-pressed: "#0B5A43"
  secondary: "#4CB18F"
  selected: "#E5F4EE"
  canvas: "#EEF1F4"
  surface: "#FFFFFF"
  chrome: "#F7F9FA"
  ink: "#17202A"
  muted: "#66737C"
  border: "#DBE4E7"
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
    fontWeight: 700
    lineHeight: 1.2
rounded:
  workspace: "12px"
  surface: "8-10px"
  control: "8px"
  badge: "8px"
motion:
  press: "90ms"
  state: "160ms"
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

**Creative North Star: "Desktop Boot Console"**

The current information architecture follows macOS HIG principles: one task-oriented workspace, clear selection focus, restrained material layers, and contextual actions. The visual system uses a pale neutral canvas with one green action/status family.

The utility presents system management in one page without a navigation sidebar. A compact startup status bar shows the current default system and the next boot target, while announcements and update status remain secondary. The lower desktop layout uses a roughly 64:36 list-to-details split; narrow windows stack the list above the inspector. Boot timeout lives in the list toolbar, so there is no separate preferences page or overlay.

### UI Library Decision
- **AntdUI 2.4.4:** selected as the single control and interaction runtime because it supports .NET Framework 4.0, provides DPI-aware GDI+ controls, and already includes interruptible button, table, input, and modal animation.
- **FluentTransitions:** evaluated for property transitions, but the current package requires .NET Framework 4.8 and duplicates the state transitions already supplied by AntdUI and `AnimatedLabel`.
- **WinFormAnimation:** evaluated for keyframe paths; no product workflow needs decorative 2D/3D motion, so adding a second animation clock would increase jitter and cleanup risk without improving task feedback.
- **CuoreUI:** evaluated for rounded and blurred controls, but it requires .NET Framework 4.7.2 and overlaps AntdUI's control set. Mixing both would break the one-component-vocabulary rule.

The release therefore combines the useful interaction patterns from the evaluated libraries while shipping one visual runtime. This preserves the standalone EXE and avoids conflicting hover, focus, and animation state machines.

## 2. Colors

The shared colors are `#168C68`, `#0F6F52`, `#0B5A43`, `#4CB18F`, `#EEF1F4`, `#F7F9FA`, `#FFFFFF`, `#17202A`, and `#DBE4E7`.

### Primary
- **Focus Green** (`#168C68`): product mark, focus identity, active state, and primary action.
- **Strong Green** (`#0F6F52`): primary hover and readable text-on-soft-green state.
- **Pressed Green** (`#0B5A43`): primary press feedback.

### Secondary
- **Secondary Green** (`#4CB18F`): focus ring and secondary active detail.
- **Selected Green Wash** (`#E5F4EE`): selected boot row and partition metadata background.

### Neutral
- **Work Canvas** (`#EEF1F4`): application background and table header.
- **Chrome** (`#F7F9FA`): auxiliary material and inspector background.
- **Clear Surface** (`#FFFFFF`): header, status panel, data rows, and secondary buttons.
- **Deep Ink** (`#17202A`): primary text.
- **Muted Label** (`#66737C`): secondary text and disabled labels.
- **Neutral Border** (`#DBE4E7`): dividers and control boundaries.

**The Green Restraint Rule.** Green is reserved for the logo, active navigation, primary action, selected row, and explicit status. It is not general decoration.

## 3. Typography

**Display Font:** Segoe UI, with Microsoft YaHei UI fallback.
**Body Font:** Segoe UI, with Microsoft YaHei UI fallback.

**Character:** Native, compact, and legible at Windows desktop DPI settings. Headings use weight rather than oversized type.

### Hierarchy
- **Headline** (700, 20px, 1.2): current default system name.
- **Title** (700, 17px, 1.2): application identity in the header.
- **Body** (400, 13px, 1.4): boot-entry data and confirmation text.
- **Label** (700, 12px, 1.2): table headers, status labels, and action state.

### Weight Roles
- **Bold:** application title, current default, section headings, system name, partition, status, populated remarks, and the primary restart action.
- **Regular:** empty remark placeholders, metadata, helper text, refresh, edit, cancel, and secondary actions.

## 4. Elevation

Depth is created with the neutral canvas behind white surfaces and one-pixel `#DBE4E7` boundaries. No decorative shadows are used in the desktop utility.

## 5. Components

### Buttons
- **Shape:** AntdUI-rendered 8px logical corners, DPI-aware drawing, and a 36-42px logical height based on hierarchy.
- **Primary:** bold white text on Focus Green, Strong Green on hover, and Pressed Green on press.
- **Secondary:** regular-weight Deep Ink text on a white surface with Neutral Border.
- **Disabled:** pale slate background with written disabled state.

### Dialogs
- **Window shell:** every application-owned dialog uses the shared 12px rounded frame, cool chrome title bar, one-pixel Slate Border, and the same Canvas background as the dashboard.
- **Hierarchy:** dialog content uses white 12px-radius surfaces, 24-28px outer spacing, Segoe UI headings, and the shared primary/secondary button vocabulary.
- **Compatibility:** Windows 11 receives the native rounded-window preference; the clipped window region preserves the same corner shape on Windows 10.

### Boot Table
- **Style:** white rows, a neutral-canvas header, an 8px outer radius, hover feedback, and an animated full-row selection model.
- **Columns:** boot system, partition, saved purpose remark, and written state.
- **Weight:** system, partition, and status are bold for scanning; populated remarks use bold Accent text, while `未设置` remains regular Muted text.
- **State:** the default system receives written status; the selected target eases into the pale green wash.

### Selected System Inspector
- **Placement:** a stable right-side pane aligned with the boot table.
- **Content:** system name, partition, written state, prominent purpose remark, and the remark-edit action.
- **Safety:** default and restart actions remain in the bottom action bar and are disabled for the current default system.

### Remarks
- **Entry point:** `编辑备注` button or double-clicking a boot row.
- **Storage:** current-user registry under `Software\DualBootSwitcher\BootRemarks`, keyed by the BCD identifier.
- **Safety:** remarks never call `bcdedit`; only the explicit default/restart actions modify the Windows BCD.

### Boot Timeout
- **Display:** a compact secondary button beside the boot-entry count shows the active timeout in seconds.
- **Input:** a numeric control accepts `0` through `999`; `0` is explicitly described as skipping system selection.
- **Save behavior:** the primary action saves the selected value immediately; the dialog itself keeps the `0`-second consequence visible before saving.
- **State:** the button is disabled while BCD is loading or when the timeout cannot be read.

### Current Default Surface
- **Style:** a white 8px-radius surface on the neutral canvas.
- **Content:** a label, system name, and pale green partition badge with full text available by tooltip.

## 6. Motion

- **Timing:** 90ms for press depth and 160ms for hover, navigation, page change, enable/disable, row selection, and status text changes.
- **Easing:** ease-out-quart, with no bounce, elastic motion, or decorative entrance sequence.
- **Performance:** one shared 16ms scheduler drives token-based, interruptible state transitions; controls use double-buffered painting, and canceled animations release their callbacks without changing layout bounds.
- **Reduced motion:** when Windows disables client-area animations, every state is applied immediately.

## 7. Do's and Don'ts

### Do:
- **Do** use the restrained green token set and compact startup status bar as the visual anchor.
- **Do** keep 24px working-area spacing and one clear primary action.
- **Do** pair green state color with written labels such as `当前默认` and `可切换`.
- **Do** keep the embedded user-provided dual-boot mark as the application and executable icon.
- **Do** reserve motion for feedback and state continuity.

### Don't:
- **Don't** copy the reference site's marketing Hero, advertisements, or card-heavy dashboard composition.
- **Don't** introduce colors outside the green and neutral families without semantic need.
- **Don't** use gradient text, glass effects, neon glow, or saturated inactive controls.
- **Don't** add page-load choreography, bouncing, or animation longer than 250ms.
