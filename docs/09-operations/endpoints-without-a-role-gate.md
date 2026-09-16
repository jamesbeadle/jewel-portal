# Endpoints without a role gate — the deliberate list

Every API endpoint resolves the signed-in user and checks a `RoleSet` (or a per-command
`*Authorisation` class) before it does anything, except the ones below. Each is here on purpose
and says why. An authorisation audit ("Drawing the Line", `docs/Drawing-the-Line.pdf`) should
except these by name rather than flag them again; anything it finds that is **not** on this list
is a real gap and gets a `FIX:` task.

Written 2026-09-16, when the last flagged cluster — `api/Features/TenderEnquiries/**` — turned out
to be the empty one-line stubs left when the feature was retired on 2026-09-03 (every file reads
"Retired 2026-09-03 … git rm it when convenient"). The stubs are deleted with this note; nothing
under that folder ever served a request after 2026-09-03.

## No sign-in at all

These take no `SignedInUserResolver` because the caller cannot be signed in yet, or the route's
own secret is the authorisation.

| Route | Function | Why it is open |
|---|---|---|
| `GET api/ping` | `Ping` | The availability probe that keeps the managed-functions host warm. Touches nothing. |
| `GET api/version` | `GetAppVersion` | The announced build number — already on every response header, nothing to protect. |
| `POST api/auth/login`, `auth/logout`, `auth/forgot-password`, `auth/set-password`, `GET auth/invite/{token}` | `Auth*` | The sign-in flow itself. `auth/me`, `auth/invite` and `auth/send-reset` resolve the user and are not on this list. |
| `api/oauth/authorize`, `oauth/token`, `oauth/register`, `well-known/*`, `.well-known/*` | `OAuth*` | The MCP connector's OAuth handshake (RFC 6749). `oauth/authorize` only validates the request and hands the browser to the portal's consent page, which reuses the portal sign-in; the consent itself (`oauth/approve`, `oauth/client-info`) and the `oauth/connections` pair resolve the user. |
| `api/mcp` (and the `api/api/mcp` alias) | `McpServer` | The MCP transport. The bearer token is checked inside the endpoint per call, and every tool call resolves the connection's user before it runs. |
| `api/imagine/{token}/**` | `Imagine*` | The prospect's public imagine page. The lead's token in the route is the whole authorisation (`ImaginePublicService`); unknown token → 404, no detail. |

## Signed in, but the signed-in identity is the scope

These resolve the user and stop at that, because the endpoint only ever reads or writes the
caller's own rows — there is no role to check beyond "is a user".

| Route family | Why sign-in is enough |
|---|---|
| `api/my/policy-sign-offs`, `my/policy-sign-offs/sign` | Their own sign-off list, keyed by their own email. |
| `api/client-portal/my/**` (`ClientPortal/*My*`) | A client contact's own requests and variation orders; the handler scopes by the caller's client. |
| `api/portal/my/**` (`Portal/*My*`) | A subcontractor's own work orders, variation requests and compliance documents; scoped by the caller's directory record. |
| `GET api/oauth/client-info`, `POST oauth/approve` | The user reading who is asking, and approving their own MCP connection. |
| `POST api/client-errors` | The browser's error report, logged under the caller's own identity — the user field is the resolved caller, never the body's. |

## How to keep this true

- A new endpoint gets a `RoleSet` gate (`RoleSet` + `IncludesAny`, as `ListEmailRecipientsEndpoint`
  does) or a `*Authorisation` class. If it genuinely belongs on one of the tables above, add it
  there in the same change, with the reason.
- A retired feature's files are deleted, not stubbed: a one-line "retired" file still has
  `Endpoint` in its name and reads as an ungated endpoint to every grep-based audit.
