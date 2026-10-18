# Scout operations

See the [quick start](../../../README.md#quick-start) for installation and launch commands, the [user guide](USER_GUIDE.md) for workflows, and the [development guide](../../engineering/DEVELOPMENT.md) for build/test commands and optional samples.

## Running the local server

Open **http://localhost:5180**. Keep one Scout process running; stop with **Ctrl+C**. Startup creates storage directories and applies committed migrations without resetting data. After building, use `dotnet run --project src/Scout.Web --no-build --no-restore`.

Restore needs NuGet access. Normal operation uses the local process and local scripts/styles, without accounts, AI, or integrations. Website/profile/meeting links open only on explicit activation. Scout does not send applications/messages, fetch vacancies, or record interviews.

Append `-- --Scout:Port=5181` to use another loopback port. Scout enforces loopback listeners and rejects unexpected Host/cross-origin HTTP/WebSocket requests. There is no login or at-rest encryption; software running under your account can access the root.

When the backend stops, an open page shows **Scout is offline**, without a countdown. Start Scout and reconnect/reload; the previous server circuit may no longer exist. Unsaved general management forms do not survive reload or backend shutdown. Workspace browser drafts have the separate recovery behavior below.

## Storage location

The fixed root is `<user home>\Documents\Scout`:

```text
Documents\Scout\
  database\scout.db
  assets\documents\<generated GUID>\original.<allowed extension>
  recordings\<working audio files and subdirectories>
```

It is resolved independently of the working directory, shared by Development/ordinary runs, and outside static assets. Only tests replace the storage service with temporary roots; no storage settings screen is provided.

There is no automatic filesystem cleanup or backup/retention service. Back up the whole root, including retained failed imports. Whole-root backups preserve saved preparation, answers and Workspace data as well as files; browser drafts are stored separately.

## File imports and recovery

Imports preserve original files and do not silently overwrite existing bytes. Missing or externally changed files produce recoverable errors; Scout does not substitute another file. Restore original bytes from backup after external loss or corruption.

Failed or interrupted imports can leave `.partial` or unregistered files. Keep these files and include them in backups; Scout reports their relative location where possible. An uncertain save is checked against the exact generated path before a retry is offered. There is no automatic cleanup or reconciliation, and unregistered files cannot be downloaded through guessed paths.

For allowed formats, size/preview limits and submission behavior, see [files and submission evidence](USER_GUIDE.md#files-and-submission-evidence). Technical path validation and import mechanics are documented in [development](../../engineering/DEVELOPMENT.md#file-storage-and-import-mechanics).

## Backup, upgrade, and restore

1. Stop every Scout process and wait for shutdown.
2. Copy the **entire root** into a new separate backup: database, assets, recordings, any SQLite WAL/SHM sidecars, and retained failed imports. A backup omitting `recordings/` cannot restore linked audio.
3. Verify file counts/hashes, `PRAGMA integrity_check` (ok), and `PRAGMA foreign_key_check` (no rows). Stronger restore checks also open files and relationships from a separate copied root.
4. Restart Scout. Keep durable backups separately; there is no automatic backup/retention.

Back up before upgrading. SQLite migrations may rebuild tables, including timeline and preparation tables. Failed migrations stop startup without a reset or fallback; preserve the whole root before investigating failures or locks. Never rewrite applied migrations or substitute `EnsureCreated`.

To restore: stop Scout, retain the current whole root separately, then put the complete backup at the fixed root. Do not overlay only the database onto existing sidecars/assets. Start the matching version; newer committed migrations apply on startup. Tests/acceptance restore separate copies and never overlay personal data.

Historical migration counts and upgrade/restore evidence are recorded in the [milestone delivery records](../../README.md#milestone-plans-and-delivery-records). Tests and acceptance checks use separate copies and do not overlay personal data.

## Browser drafts and reconnecting

Scout must run for database operations. Drafts depend on completed IndexedDB writes in the same browser profile/origin/port and can be lost if browser data is cleared/evicted. Browser storage failures show recovery-unavailable feedback; save or copy immediately. The quiet reconnect path waits for pending local writes before reloading a rejected circuit, or leaves copy controls available when safe recovery cannot be established. Old/offline tabs may retain previously approved content; screen-share privacy covers this view, not other desktop windows. Archived rounds reopen read-only.

Whole-root backups contain canonical SQLite saves, not unsent browser drafts. Drafts are separated by Workspace, round, review, target and browser tab and are not automatically expired. Private management forms do not gain browser draft recovery.

Use **Restore draft**, **Keep saved version** or **Copy draft** for current-review recovery. Excluded or older-review drafts can be recovered only from the private review page. Follow the [saving and recovery workflow](USER_GUIDE.md#saving-conflicts-and-draft-recovery) before approving recovered content for sharing.

## Preview limitations

The browser must support native PDF rendering for inline previews. The in-app browser showed a blank PDF surface during acceptance checks; use **Download original** or **Download submitted CV**. Registered MP3/WAV/M4A recordings have native audio controls on private interview details. Actual codec support varies; verified download is the fallback. Every newly opened media source is fully hashed before streaming, which can delay opening/seeking large recordings. Missing/changed linked sources require restoration or explicit new registration; Scout never updates expected hashes silently.
