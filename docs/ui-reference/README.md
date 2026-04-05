# UI Reference: Dark Analytics Dashboard

## Purpose

This document captures the style and structure of the referenced `WunderUI` dashboard screenshot so future AI agents can reuse it as a design brief.

Use this reference when the product needs:
- a premium dark-mode dashboard
- a dense but readable admin shell
- analytics-heavy layouts with strong hierarchy
- international sales, activity, or KPI visualization patterns

Do not copy the screenshot literally. Reuse its design system, layout logic, and component behavior.

## Design Summary

The page is a dark analytics dashboard with three persistent zones:
- a fixed left navigation rail
- a slim top utility bar
- a wide main analytics workspace with stacked panels

The aesthetic is premium SaaS:
- near-black background
- charcoal cards
- white primary text
- muted secondary text
- soft borders
- large radius values
- restrained accent colors

The interface feels polished because it balances dense information with generous padding, clear grouping, and very consistent component shapes.

## Layout Blueprint

### 1. App Shell

The shell uses a wide desktop canvas with a fixed sidebar on the left and content on the right.

- Left sidebar: narrow, persistent, vertically structured
- Top bar: shallow horizontal strip above the content area
- Main content: stacked cards with large internal padding

### 2. Sidebar

The sidebar includes:
- product logo and wordmark at the top
- primary navigation in the middle
- nested analytics navigation
- profile/settings area lower down
- bottom utility actions

Important behaviors:
- active nav item uses a filled highlight pill
- inactive items are simple icon + label rows
- some items include count badges
- nested sections use indentation and subtle tree lines

### 3. Top Bar

The top bar includes:
- large rounded search field
- notification icon
- user avatar/initials
- name + role text
- dropdown affordance

This bar is slim and utilitarian. It should not visually compete with the main charts.

### 4. Main Content

The content stacks in this order:
- page title
- KPI summary row
- large sales chart panel
- large geographic activity panel

This ordering matters. The user sees summary first, then trend detail, then geographic breakdown.

## Visual Tokens

### Base Palette

Use a dark neutral foundation:
- background: very dark charcoal, not pure black
- panels: slightly lighter charcoal
- borders: thin gray with low contrast
- text primary: white or near-white
- text secondary: cool muted gray

### Accent Palette

Keep accents purposeful:
- light blue: active navigation and primary shell emphasis
- purple/lavender: charts, bars, data visualization
- green: positive change and upward trends
- pink/red: count badges and alert markers

Accent colors should be sparse. The screenshot works because 80% of the UI stays neutral.

## Typography

Typography is modern product UI typography:
- bold large section titles
- medium metric labels
- small muted support text
- tight line-height in headings

Desired tone:
- clean
- product-oriented
- readable at a glance
- not playful or comic

If recreating this style, choose a modern sans family for both headings and body, with stronger weight differences instead of decorative font changes.

## Surface Treatment

Panels and controls follow these rules:
- large rounded corners
- low-contrast borders
- very soft shadows
- minimal gradients
- no heavy gloss

The effect is soft and premium, not glassy or glossy.

## Core Components

### Logo Block

Characteristics:
- compact abstract logo with multiple accent colors
- strong wordmark next to it
- clear separation from the rest of the shell

### Navigation Item

Structure:
- outline icon
- text label
- optional count badge
- optional expand/collapse chevron

States:
- active: filled light-blue background
- inactive: transparent
- nested: indented with subtle hierarchy cues

### Search Input

Structure:
- large rounded field
- search icon at the left
- muted placeholder text

Behavior:
- should feel global, not tiny

### KPI Card

Structure:
- large currency/metric value
- short label
- positive trend chip

Behavior:
- quick-scan summary
- minimal internal decoration

### Segmented Time Filter

Structure:
- horizontally aligned pills or bordered tabs
- options such as `12 months`, `30 days`, `7 days`, `24 hours`

Behavior:
- mutually exclusive
- small, precise, utility-oriented

### Product Sales Chart

Key traits:
- dominant upper analytics panel
- purple vertical bars with rounded tops
- country markers/flags on the x-axis
- light tooltip on hover
- subtle gridlines

This is not a generic chart. It combines analytics polish with a strong brand accent.

### Geographic Activity Panel

Structure:
- dotted/stylized world map on the left
- headline numeric metric on the right
- ranked country list with flags and progress bars

Behavior:
- map communicates global spread
- list communicates exact ranking
- tooltip reveals per-country details

## Interaction Rules

Future agents should preserve these interaction ideas:
- active navigation is obvious but not loud
- chart hover creates a focused state with tooltip + highlighted region
- filters are compact and clearly selectable
- badges communicate alerts or counts with minimal noise
- user identity block supports account actions
- theme toggle and primary bottom CTA are always accessible

## Information Hierarchy

The page succeeds because the hierarchy is strict:

1. App shell and navigation
2. Page title
3. KPI row
4. Main chart
5. Secondary geo panel
6. Detailed ranked list

Do not flatten all elements to equal weight. The large chart panel must visually dominate the summaries beneath it.

## Spacing Rules

Use consistent spacing bands:
- shell padding: generous
- panel padding: medium-large
- tight spacing inside stat groups
- wider gaps between major sections

The UI should feel organized through spacing, not through many dividers.

## Reusable Style Directives

When another agent uses this reference, apply the following:

- Prefer dark charcoal surfaces over pure black.
- Use one bright active-navigation color and one separate chart accent color.
- Keep panel corners large and consistent.
- Use thin low-contrast borders instead of heavy outlines.
- Keep typography modern and neutral.
- Let analytics panels be the visual priority, not the navigation.
- Use muted text generously so white text is reserved for important values.
- Keep badges and chips compact.
- Use charts that feel productized, not default-library stock visuals.
- Pair geographic visuals with ranked numeric lists for clarity.

## Anti-Patterns To Avoid

Do not drift into:
- flat black backgrounds with no layering
- too many accent colors used at once
- tiny unreadable chart labels
- oversized shadows
- overly decorative gradients
- consumer-app softness or cartoon styling
- overly bright cyberpunk neon unless explicitly requested

## Best Use In This Repo

For `Brain Games`, this reference is best applied to:
- authenticated dashboard areas
- leaderboard analytics
- player activity overview
- admin or operator surfaces

It is less suitable for:
- the core casual game board itself
- first-time onboarding
- playful lesson cards

## Agent Instructions

Before using this reference in implementation:

1. Read this file.
2. Decide whether the target screen is a dashboard/admin surface or a game-first surface.
3. Reuse the layout hierarchy and token logic, not the exact screenshot composition.
4. Keep dark mode premium and restrained.
5. If adapting to Brain Games, preserve existing product goals and avoid making the entire product feel like a finance dashboard.
