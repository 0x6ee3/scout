# 0001 — Product development cycles and Scout.Scribe

Date: 16 October 2026. Status: accepted by the user. This records a documentation/planning decision, not implementation or release evidence.

## Context

Scout.Web milestones 1–5 provide application/interview management and manual audio/transcript review. The next proposal used M6A/6B for a companion called Scout.Recorder, with Web UI polish outside the milestone sequence. The companion will also perform transcription and local LLM processing, so both its name and development lifecycle need broader scope. Historical plans must remain understandable as direction changes.

## Decision

Use independent cycles identified by product and target release. Scout.Web v1 contains M1–M5 and M6 UI overhaul; completing M6 closes its intended 1.0 cycle. Scout.Scribe v1 has its own M1 recording, M2 local transcription and M3 fact extraction and review. It is not a Web v1 release dependency. A possible Web v2 cycle remains uncommitted until real use and prioritization establish goals.

Name the planned Windows desktop companion **Scout.Scribe**. Its proposed project is `src/Scout.Scribe/Scout.Scribe.csproj`; no such project is created by this change. Preserve loose coupling through portable audio and TXT artifacts, independent processes and existing manual Web imports. Local LLM output starts as reviewed text; shared database access and automatic structured Web updates are not implied.

Keep living specifications/guides in `docs/products/`, repository guidance in `docs/engineering/`, dated decisions here, and scoped plans/evidence in `docs/cycles/<product>-<release>/`. Keep unprioritized ideas in the top-level feature wishlist. Milestone numbering restarts within each cycle and must be qualified when ambiguous.

## Historical preservation and consequences

M1–M5 plans/delivery records move into the Web v1 cycle with original wording and evidence preserved; only relative links are repaired. Retain the former M6A plan and pre-cycle roadmap under the [Scribe archive](../../cycles/scout-scribe-v1/archive/README.md). Derive the current [Scribe M1 plan](../../cycles/scout-scribe-v1/milestones/MILESTONE_1_PLAN.md) from that proposal, updating naming, ownership and successor milestones.

This supersedes the earlier joint Web/Recorder release sequence, M6A/M6B numbering and treatment of all local LLM processing as uncommitted. Scribe M3 is now a cycle goal; detailed design and dependencies remain undecided. Automatic structured promotion into Web and advanced Web/CV features remain wishlist items.

Active plans can evolve with dated revisions. Completed plans and delivery evidence retain their historical meaning; superseding decisions are new records. At each cycle's completion, write a release summary from actual evidence. Product references continue evolving and should not be mistaken for frozen release snapshots.
