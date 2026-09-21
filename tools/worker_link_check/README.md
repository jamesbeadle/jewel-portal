# Worker link check

```
python3 -m tools.worker_link_check.check .
```

`worker/Jewel.JPMS.Worker.csproj` links a named subset of `api/` by `Compile Include`, so every
linked file is compiled twice: once with the whole api, once with the worker's much smaller set.
A type in the file's own namespace needs no `using`, so a reference to something the worker does
not compile leaves no trace in the source — the api build stays green and the worker build fails.
That happened three times on 21 September 2026.

This reads the project file, expands its globs, and reports any linked api file that reaches for a
type declared in `api/` but outside what the worker compiles. Exit 1 on a finding.

Reaching for a type means naming it as a static member's owner (`LeadMarketingConsents.Of`), in a
construction (`new AzureBlobImagineImageStore`), in type position (`IImagineImageStore store`), or
as a type argument. A name in member position — the `Outcome` of `public string Outcome` — is not,
which is what keeps a property sharing a type's name from reading as a finding.

Interpolated strings are read, not skipped: `$"…{PrivacyNoticeLink.Path}…"` reaches for a type,
and an earlier draft that dropped whole string literals missed exactly that break.

## Fixing a finding

Two ways out, and the type decides which:

- A fact the api, the worker and the portal all state belongs in `contracts/`, under
  `Jewel.JPMS.Models` — all three projects global-use it.
- A type that is genuinely the api's own earns a `Compile Include` line in the worker's project
  file, next to the file that needs it.

## What it does not do

It is regular expressions over source, not a compiler, so it answers one question only: is the
type there to be found. It says nothing about whether the code is otherwise correct, and it does
not look at the MCP host or the Static Web App, which both build the whole api and so cannot hit
this class of failure.
