# Web v1 M6 — Package 1 verification notes

Date: 16 October 2026. Scope: source inventory and isolated interactive design proposals, not production UI acceptance. Status: ready for user design review; no composition has been approved for rollout. See the [design brief](M6_DESIGN_REVIEW.md) and [inventory](M6_UX_INVENTORY.md).

## Environment and checks

Used bundled Python to wrap the review fragment and bundled Playwright with installed **headless Microsoft Edge 155.0.4283.45** for local browser checks. The wrapper was generated under `.local/m6-review/`; the prototype used fictional records and never loaded Scout's server or database. No dependencies were added to the application or installed for this exercise.

The initial sandboxed Edge launch failed with access-denied/profile errors. The local check then ran successfully outside the sandbox. An early interaction run timed out while a dialog was still intercepting a subsequent click; the final exercise explicitly waited for dialog closure. Visual inspection also caught invalid blank alternative screenshots caused by viewport-dependent stage growth/scroll position. The stage now has bounded height and screenshots use actual carousel navigation with scroll reset. Those early runs are not reported as passing evidence; screenshots and results below are from the final successful run.

| CSS viewport | Measured content-grid width | Work-centered columns, rounded | Result |
|---|---|---|---|
| 3840 × 2160 | 3712px | 973 / 1546 / 1145px | Three columns, no measured horizontal grid overflow. |
| 2560 × 1440 | 2432px | 633 / 1006 / 745px | Three columns, no measured horizontal grid overflow. |
| 1920 × 1080 | 1824px | 472 / 749 / 555px | Three columns, no measured horizontal grid overflow. |
| 720 × 1100 | 648px | Single column | Narrow application-view sanity check; not the primary design target. |

Desktop checks covered Application, Interview preparation and Position editor, populated/sparse/long-name settings and both proposed column arrangements: 27 screen/state/viewport combinations, each measuring both layouts. Sparse states apply to application/preparation collections; the existing-position editor retains its fields. The work-first alternative was also selected through the carousel for a screenshot. Static captures were visually inspected for the three proposed screens, the alternative arrangement, dark appearance and the central confirmation.

Interaction checks passed for:

- Unsaved position changes → Cancel → Keep editing, with field values retained.
- Simulated position-save failure, with no success acknowledgment and the draft retained.
- Explicit-save success acknowledgment in a central dialog.
- Dirty contact dialog → Escape → Keep editing, retaining its entered name.
- Simulated contact-save failure retaining inputs, followed by cancel/discard.
- Unlink confirmation, cancel, horizontal centering and return focus to the invoking icon.
- Review-screen switching, layout-carousel navigation and populated/sparse/long-name rendering.

Final browser page-error collection was empty. Raw measurements and interaction outcomes are in [review-check-results.json](evidence/review-check-results.json). The temporary check script is a local review harness, not a new application test suite.

## Captured review evidence

All PNGs below are 3840 × 2160 browser-viewport captures at device scale factor 1. They show the review surface within a wrapper; they do not establish physical-monitor scaling, an actual production UI, or a full production browser workflow.

- [Application — work centered](evidence/application-work-centered-3840.png).
- [Interview preparation — work centered](evidence/preparation-work-centered-3840.png).
- [Position editor — work centered](evidence/editor-work-centered-3840.png).
- [Application — work first](evidence/application-work-first-3840.png).
- [Application — simplified structural baseline](evidence/application-structural-baseline-3840.png).
- [Long names](evidence/application-long-names-3840.png).
- [Sparse application](evidence/application-sparse-3840.png).
- [Central unlink dialog](evidence/unlink-dialog-3840.png).
- [Dark appearance](evidence/application-dark-3840.png).

## Limits and remaining review

The user's physical 4K monitor, OS scale, browser zoom and actual CSS viewport have not been measured. Headless viewport emulation is not physical-device verification. The structural baseline is reconstructed from source, not captured from the running app. Production form saving, guarded downloads, uploads, codecs, Workspace privacy, cross-tab conflicts and draft recovery are not exercised by this prototype and retain their existing verification status.

The prototype is for layout and interaction review. Some actions show an explanatory local placeholder; it does not provide every destination, rich-text renderer, field-level rule, source-state variation or persistence path. It preserves the production intent in the inventory rather than claiming full feature implementation. Native dialog behavior and a focus-return check are not comprehensive accessibility certification. Screenshot spacing/readability still needs review on the user's setup.

Application/test/solution content hashes were compared with the start of this package to confirm no application changes. Document/link/route-coverage and whitespace checks accompany delivery of these review materials. No .NET build or application suite is required for this package because the implementation is untouched. M6 packages 2–5 and the Web 1.0 release remain outstanding.
