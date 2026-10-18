# ADR 0003 — Shared Web presentation patterns

Date: 16 October 2026. Status: Accepted and implemented. Product/cycle: Scout.Web v1 M6. Refines [ADR 0002](0002-web-v1-desktop-ux.md); does not supersede its scope or privacy requirements.

## Context

The user approved the Work centered review and requested rollout across suitable pages. The existing Blazor Server application already owns validation, persistence, direct editor routes and an explicitly restricted interview Workspace. A consistent presentation layer should reuse these contracts.

## Decision

Use a shared wide CSS layer, page/section/item action components, local SVG icons and native HTML dialogs. Rich detail views use context at left, the main task in a larger center column and supporting resources at right. Sparse views use fewer groups; catalogs and timelines keep their ordered structure.

Reuse focused form components in both dialogs and direct routes. Share busy, save-success and dirty-navigation handling through DraftForm/UiDialog. Native dialog modality makes background content inactive; the small local JavaScript module handles explicit Tab/Shift+Tab wrapping, Escape, input dirty state and focus return. Confirmed operations acknowledge success; intermediate review and live autosave do not.

Named breadcrumbs query only small context projections in the private MainLayout. The shared Workspace keeps its own layout, existing approved view model and draft/recovery implementation. It must never inherit private breadcrumbs or management forms.

## Alternatives

A UI framework or remote icon set would add dependencies and introduce a separate styling/interaction system. A per-page implementation would duplicate save, discard and focus behavior. Converting every editor to a dialog would constrain long text and remove useful direct navigation. These alternatives are unnecessary for the existing workflows.

## Consequences

No API, persistence contract or migration is required. Shared components reduce drift but require careful verification of lifecycle, dirty navigation and nested operations. Long editors and HTTP transcript correction retain their routes. Future UI changes should preserve the private/shared boundary and use the same action/dialog conventions. Actual verification and remaining limits belong in the [M6 delivery record](../../cycles/scout-web-v1/milestones/MILESTONE_6_DELIVERY.md).
