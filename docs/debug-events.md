# Debug events

Any mod built on Ducky.Sdk can report debugging events through one shared API. One mod at a time can declare itself
the **recorder**: it receives the events of every reporting mod and writes them into a single JSON Lines file that the
recorder mod names itself.

Reporting costs almost nothing when nobody records: without a recorder, `Report` discards the event at once and
nothing is queued in the mod message hub.

## Reporting an event

```csharp
using System.Collections.Generic;
using Ducky.Sdk.Contracts;

// kind is required; detail and fields are optional.
Contract.DebugEvents.Report("hurt", "victim=player", new Dictionary<string, object?>
{
    ["damage"] = 12
});
```

- `Report` may be called from any thread and never throws. An empty `kind` is discarded.
- `fields` is serialised as JSON, so use plain values (numbers, strings, booleans, nested dictionaries).
- Reporters look for the recorder every 5 seconds. Events start reaching a recorder about a second after it is
  already running, or within 5 seconds of a later start. Events reported while no recorder is present are not kept.

`Contract.DebugEvents` also has `IsRecorderAvailable` (true while a compatible recorder is negotiated with this mod)
and `IsRecorder` (true when this mod is the recorder).

## Recording the events

Only one mod records at a time. The recorder mod chooses the file and releases the role when it is disabled:

```csharp
using Ducky.Sdk.Contracts;
using Ducky.Sdk.ModBehaviours;

public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        // false: replace the file. true: append to it.
        bool started = Contract.DebugEvents.StartRecorder("C:/logs/run1.jsonl", append: false);
    }

    protected override void ModDisabled()
    {
        Contract.DebugEvents.StopRecorder();
    }
}
```

- `StartRecorder` returns `false` and logs one warning when another recorder already runs, or when the file cannot
  be opened. The existing recorder is not touched.
- `StopRecorder` flushes and closes the file and frees the role. Calling it when not recording does nothing.
- Disabling or hot reloading the recorder mod releases the role automatically, so the file is closed and parseable.
- Call `StartRecorder` on the main thread, for example in `ModEnabled()`.

## The alias id

The recorder registers its hub client under the fixed id **`Ducky.DebugRecorder`**. Reporters find the recorder by
looking for that id, so they need no configuration. Because a hub id has one owner, the id is also the single-recorder
rule.

Each reporter registers its own hub client under `<modId>.debug-events`, so replies never reach handlers the mod
registered for other purposes.

## Wire messages

All messages travel through the existing `ModHttpV1` hub. There are three content types.

**hello** (reporter to recorder, never versioned): lists the versions the reporter speaks.

```json
{"reporter":"Ducky.Terminals","versions":[1]}
```

**ready** (recorder to reporter, never versioned): names the version to use and every version the recorder supports.
Version `0` means that no version is common.

```json
{"version":1,"supported":[1]}
```

**batch** (reporter to recorder, versioned in the content type `ducky.debug-event.batch.v<N>`): carries the events.

```json
{"reporter":"Ducky.Terminals","dropped":500,"events":[{"kind":"hurt","detail":"victim=player","at":"2026-11-02T01:02:03.456Z","fields":{"damage":12}},{"kind":"state","at":"2026-11-02T01:02:03.500Z"}]}
```

- `reporter` is the reporting mod's id, the id the file records.
- `dropped` is the number of events lost since the previous batch (see below).
- `at` is the UTC time the reporter created the event. `detail` and `fields` are optional.

## Version rules

- The batch version is part of the content type. An incompatible format becomes `batch.v2`, and it is sent only after
  negotiation.
- `hello` and `ready` never carry a version suffix. Their bodies only gain fields, so any two SDK versions can always
  complete the handshake.
- Within one version, fields are only added. They are never renamed, retyped or removed.
- Readers ignore unknown fields, treat a missing optional field as absent, and skip an event without a `kind` without
  discarding the other events in the batch.
- **Negotiation:** a reporter sends `hello`; the recorder answers `ready` with the highest version both support. A
  reporter that supports `[1]` meets a recorder that supports `[1, 2]`: both use version 1. With no common version the
  reporter sends nothing and logs one warning.
- A recorder decodes a batch of any version it supports even without a prior `hello`, so a restarted recorder loses
  nothing. A batch of a version it does not support is ignored, with one warning per sender and content type.
- When the recorder disappears, the reporter stops sending and negotiates again when it returns.

## Batching and drops

- A reporter sends at most **50 events per batch**, one batch every **200 ms**, so a reporter sends at most five
  messages a second.
- A reporter holds at most **1000 unsent events**. When the buffer is full, the oldest event is dropped and counted.
- The next batch carries the count in `dropped`. The recorder writes it as a `debug-events-dropped` line, so a gap in
  the file is visible.

## The recorder file: JSON Lines

The file is **JSON Lines** (`.jsonl`): one complete JSON object per line, many lines in one file. It has no wrapping
array, no commas between lines and no header or footer. It is UTF-8 without a BOM, and every line ends with `\n`.
Because each line stands alone, the file can be appended to across restarts, read while the game runs, and read
line by line even after a crash.

Each event becomes one line:

```json
{"n":1,"t":41.25,"at":"2026-11-02T01:02:03.456Z","source":"sdk","reporter":"Ducky.Terminals","kind":"hurt","detail":"victim=player","fields":{"damage":12}}
```

| Field | Meaning |
| --- | --- |
| `n` | Line number, starting at 1 and rising by one for each line in the file (also across appended runs). |
| `t` | Seconds since game start when the line was written. |
| `at` | When the reporter created the event, ISO 8601 UTC. |
| `source` | Always `sdk`. |
| `reporter` | The reporting mod's id. |
| `kind`, `detail`, `fields` | The event. `detail` and `fields` appear only when the reporter sent them. |

A batch that reports dropped events adds one line before its events:

```json
{"n":2,"t":41.5,"at":"2026-11-02T01:02:03.700Z","source":"sdk","reporter":"Ducky.Terminals","kind":"debug-events-dropped","fields":{"count":500}}
```

A line never contains a raw line break: line breaks and quotes in `detail` are escaped, so each event stays on one
line.

Reading the file:

```bash
# one JSON object per line; jq -c keeps the output one line per event
jq -c . "C:/logs/run1.jsonl"

# read line by line in Python
python3 -c 'import json,sys; [print(json.loads(l)["kind"]) for l in open(sys.argv[1], encoding="utf-8") if l.strip()]' "C:/logs/run1.jsonl"
```

Do not parse the whole file as one JSON document: it is not one.

## Size cap

The recorder stops writing when the file reaches **64 MiB**. The cap is checked after each complete line, so no line
is cut. The recorder then releases the role and logs one warning. A later `StartRecorder` with `append: false` starts a
new file.

## Single recorder

Only one recorder exists in a game session. While one is registered, `StartRecorder` from another mod is refused with
one warning, and the existing recorder keeps writing. The reporters are unaffected.

Two mods that start a recorder in the very same instant can race. The failure mode is a missing file, never a
corrupted one.

## Limits

- Delivery is not guaranteed. Under a burst beyond the buffer, events are dropped and counted, never silently.
- Events are for developers. Nothing in the file is sent back through the hub, and rotation and compression are not
  provided.
- The recorder's path comes only from the recorder mod's own code. A message cannot name or change it.
