# Scout documentation

Start with the [repository README](../README.md) for launch instructions or the [portfolio roadmap](ROADMAP.md) for product and development-cycle status.

## Guides

| Document | Purpose |
|---|---|
| [Web user guide](products/scout-web/USER_GUIDE.md) | Implemented application, document and interview workflows. |
| [Web operations](products/scout-web/OPERATIONS.md) | Running, backup, restore and troubleshooting. |
| [Development guide](engineering/DEVELOPMENT.md) | Repository setup, build, verification and optional samples. |

## Specification

Product documents evolve to describe the current product and explicitly identified planned capabilities. They are not release snapshots or evidence that planned functionality exists.

| Product | References |
|---|---|
| Scout.Web | [Product](products/scout-web/PRODUCT.md), [technical design](products/scout-web/TECHNICAL.md), [domain model](products/scout-web/DOMAIN_MODEL.md) |
| Scout.Scribe — experimental companion | [Product](products/scout-scribe/PRODUCT.md), [technical design](products/scout-scribe/TECHNICAL.md) |

The [important constraints](products/scout-web/TECHNICAL.md#5-important-constraints) apply to both products. The [specification guide](SCOUT_SPEC.md) remains an entry point. Rough ideas belong in the [feature wishlist](FEATURE_WISHLIST.md).

## Milestone plans and delivery records

Milestone numbering is local to a product's development cycle. Always identify the cycle when referring to a milestone: **Web v1 M6** or **Scribe v1 M1**.

| Development cycle | Scope and status |
|---|---|
| [Scout.Web v1](cycles/scout-web-v1/README.md) | M1–M6 implemented; [release baseline](cycles/scout-web-v1/RELEASE_SUMMARY.md) records checks and remaining physical-display acceptance. |
| [Scout.Scribe v1](cycles/scout-scribe-v1/README.md) | M1 capture experiment and tray/settings implemented; remaining reliability and hardware acceptance pending. M2 transcription and M3 extraction/review are planned. |

The independent [Scribe.js Electron experiment](../src/Scribe.js/README.md) compares a JavaScript implementation with .NET Scribe. It includes a pinned microphone option and combined recording output; those choices do not change the .NET product's scope. See its [verification report](../src/Scribe.js/VERIFICATION.md) for evidence and remaining acceptance.

Each cycle owns a roadmap, milestone plans and delivery evidence. Cycle indexes link to every historical record. A possible Web v2 cycle will be created when its goals are prioritized; wishlist entries alone do not open a release cycle.

## Documentation lifecycle

- `products/`: living product specifications, user guides and operations. Update them as behavior or direction changes, labeling planned capabilities clearly.
- `engineering/`: living repository guidance; the [ADR collection](engineering/decisions/README.md) records significant dated architecture, product-boundary and UX decisions, including what they supersede. Use its template for future substantial choices.
- `cycles/<product>-<release>/`: release-specific scope and historical evidence. Active plans may evolve with dated revisions. Completed plans and delivery records retain original wording, assumptions and verification limits; later decisions supersede them explicitly. Repair relative links when files move without rewriting their historical claims.
- `archive/` within a cycle: superseded proposals with a separate index explaining their status and successor. Historical names and paths in prose remain as recorded.
- `FEATURE_WISHLIST.md`: rough ideas without delivery commitments. Promote an idea into a cycle only after prioritization and acceptance criteria are defined.

At cycle completion, add `RELEASE_SUMMARY.md` identifying the actual delivered baseline, verification, deviations and known limitations. No completed release summary is created before the release work is done. Git retains document revisions; cycle records preserve a readable account of the decisions and outcomes.

See the [product-cycle decision](engineering/decisions/0001-product-development-cycles.md) for the transition from the former shared milestone sequence and the Scout.Recorder name.
