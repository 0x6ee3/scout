# Agent workflow for Scout

## Reliable file editing and bounded recovery

- Prefer the built-in patch tool for focused edits to Markdown, JSON, configuration,
  and source files. Do not introduce Node.js, npm packages, or MCP servers just to
  read or edit ordinary text files.
- Use `rg` for text discovery and native PowerShell for a local file-editing
  fallback. Use single-quoted here-strings for literal multiline content; preserve
  UTF-8 encoding, existing line endings, and unrelated user changes.
- After a failed operation, inspect the error before retrying. A retry must address
  an identified cause or a clearly transient failure. Never repeat an unchanged
  failing call.
- For the same file-editing objective, allow at most two failed attempts through
  one tool or runtime, including attempts with revised syntax. Then switch to an
  independent available method. Renaming the command or script does not reset
  this limit.
- If a Node.js or MCP runtime fails, use a native shell fallback instead of
  debugging or reinstalling that runtime during an unrelated documentation task.
- Before a fallback write, check whether the failed operation partially changed
  the target. After the write, verify the intended content and inspect the diff.
- If the independent fallback also fails, stop that operation and report the
  exact error, any partial changes, and the smallest next step. Continue unrelated
  work that can still be completed. Do not start another chain of speculative
  retries or silently expand the task into environment repair.

## Keep verification proportional

- For documentation-only edits, inspect the affected text and diff and run
  `git diff --check -- <changed paths>`. Check local links when changed. Do not
  build the application or run the full test suite unless the documentation task
  requires verifying application behavior or executable examples.
- For code changes, run checks appropriate to the affected behavior; see
  [docs/engineering/DEVELOPMENT.md](docs/engineering/DEVELOPMENT.md#build-and-verification) for the existing
  .NET, JavaScript, and EF verification commands.
- Read relevant sections of large documents rather than repeatedly dumping whole
  files. Keep command output focused on evidence needed for the task.
- Treat these retry limits as agent instructions, not an enforced spending cap.
  Report unresolved failures honestly; never claim that a failed check passed.

## Record significant decisions

- Use the [ADR collection](docs/engineering/decisions/README.md) and its template
  for significant architecture, product-boundary and durable UX decisions.
- Preserve accepted historical rationale; document material changes in a new
  numbered ADR that identifies the affected earlier decision. Routine edits do
  not need an ADR.
