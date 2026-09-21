# The system-wide permission check

Three doors reach the same data — the **API**, the **Blazor UI** and the **MCP connector** — and
each declares permissions in its own vocabulary. This check resolves all three to `Role` names and
reports every place they disagree with `policy.json`.

    python3 -m tools.permissions.check .                      # the reading
    python3 -m tools.permissions.check . --json tools/permissions/permission-check.json
    python3 -m tools.permissions.check . --baseline tools/permissions/baseline.json
    python3 -m tools.permissions.gate tools/permissions/baseline.json tools/permissions/permission-check.json

It is read-only. It writes no source file, needs no database and no .NET SDK.

## What it reads

| Surface | Read from | Resolved to |
|---|---|---|
| API | every `[HttpTrigger]` in `api/**` | route + verbs → the roles its gate admits |
| UI | every `@page` in `jpms/**` | route → the role set its `Page OpenTo` names, if it names one |
| Connector | `AiTool` and `AiAction` in `api/Features/Ai/Tools/**` | `VisibleTo` versus the roles its `AuthorisationType` admits |

The role vocabulary — `RoleSet`, the `JpmsRoles` aliases and every shared set (`JpmsRoleSets`,
`TriageRoles`, `SalesRoles`, `LabourRoleSets`…) — is declared in `contracts/Models`, where the
API's gates and the pages' `OpenTo` read the same constant; a set only one endpoint uses may still
be declared beside that endpoint in `api/**`. Both places are read.

A gate is followed through all four shapes the codebase uses: an inline
`SomeRoles.Set.IncludesAny(user.Roles)`, `AdminGate.Allows(user)`, an injected `*Authorisation`
field, and one gate delegating to another. Resolution follows private helper methods, partial
classes split across files, and expression-bodied members — an endpoint whose gate sits in a
private `Gate(request)` helper is not a hole, and must not be reported as one.

The connector is held to the site's record scopes as well as its role gates: an endpoint that
consults a `*Scope` after its role check is matched by an entry in `AiActionScopes`, which
`perform_action` runs before the handler — the rule "a scoped command is scoped on the connector
too" reports any scoped command the connector dispatches without one.

## policy.json is the declared truth

The policy is written in the business's words and reviewed like a document. It says who may reach
correspondence, which route prefixes each external role is confined to, which routes may be open,
which are scoped by the caller's own identity, and which gates are resolved at run time.

`correspondence.mayReach` is deliberately **ahead of the code**: it records the rule as stated
(2026-09-19), so the check reports the distance still to travel rather than blessing what is there.

`pagesWithoutARoleCheck` names the routes that render nothing to gate — the sign-in pages, a
public token door, a redirect — with the reason beside each. Every other routed page names the
role set that may open it (`<Page OpenTo="…">`), and `Page` throws rather than render a page that
names nobody.

`externalRouteExceptions` is where an external role's reach beyond its own portal is written down,
with the reason — the permissions matrix row that allows it. It is the answer to "which of this is
purposely shared?". Routes left OUT of it keep failing on purpose: the architect's seven
mailbox and outbound-email routes are an open question for the directors, not a settled rule, and
the check should go on saying so until somebody decides. Adding a route here is the deliberate act
that new external exposure has to pass through.

## The ratchet

`baseline.json` holds the count of places each rule is broken today. The gate fails when any count
goes up, so the numbers can only come down. A new endpoint without a gate fails before it merges
rather than at the next audit.

## Before trusting a change to this tool

It has been wrong, confidently, in ways that are easy to repeat:

- **Mask comments and string literals before reading structure.** `"…the audit record of why."`
  inside a validation message parsed as a type declaration named `of`, whose body then carried one
  file's gates into another file's answer — and reported that a Client could move timesheets.
- **Never shadow a parameter with a loop variable.** One such shadow silently disabled type-local
  role-set lookups for a whole class of gates.

After changing the resolver, draw a sample of endpoints at random and read their gates by hand. A
checker that is quietly wrong is worse than no checker, because its findings are believed.
