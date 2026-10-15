
## C# Code Intelligence — Rider MCP

This project is developed in .NET/C#. JetBrains Rider is
available as a semantic code intelligence server through MCP.

### General policy

Prefer Rider MCP tools when an operation requires understanding
C# symbols, types, references, inheritance, or call relationships.

Do not default to PowerShell, rg, or textual replacement for
operations that can be performed more accurately using Rider.

Use the available tools deliberately. Do not call MCP tools
when ordinary file reads or text searches are sufficient.

### Code exploration

- Use `search_symbol` to locate C# classes, interfaces,
  methods, and other symbols.
- Use `get_symbol_info` to inspect definitions and signatures.
- Use `analyze_calls` with `INCOMING_CALLS` to identify callers.
- Use `analyze_calls` with `OUTGOING_CALLS` to understand
  dependencies and execution paths.
- Use `get_file_problems` to inspect IDE diagnostics.

Before making changes to existing functionality, inspect
the affected symbols and their relevant dependencies.

### Refactoring

- Prefer `rename_refactoring` for renaming C# symbols.
- Do not use global string replacements to rename classes,
  methods, properties, or interfaces.
- Use ordinary file editing for implementation changes.
- Preserve established architectural conventions.

### Verification

After modifying C# source files:

1. Use `build_project` when available.
2. Inspect relevant diagnostics using `get_file_problems`.
3. Run the appropriate automated tests.
4. Report any remaining errors or unverified behavior.

A successful build does not replace running tests.

### Fallbacks

If Rider MCP is unavailable, fails, or does not support a
required operation, use standard CLI and file tools.

Do not repeatedly retry a failing MCP operation.

Use `rg` and other textual searches for configuration,
documentation, string literals, and exploratory searches.

### Tool availability

Do not assume a tool is available simply because it is
documented here. Use only tools exposed in the current session.
