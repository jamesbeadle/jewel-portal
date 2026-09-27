# Portal hosting upgrade, 27 September 2026: what changed and what it costs

A summary for the directors. Figures before VAT unless stated; the October invoice confirms them.

## What was wrong

The portal's API went to sleep after twenty minutes without use, so the first click of the
morning took seven to ten seconds. Every release restarted it for everyone using the portal.
And it ran in a different Azure region from its database, which added delay to every query.

## What happened with Microsoft

The first plan, a dedicated App Service plan at about £94 a month, was refused by Microsoft
after eighteen days and four engineers: North Europe is short of that hardware and quota
requests there are being turned down. We stayed on Azure and moved to a different Azure
service, Azure Functions Flex Consumption, which draws on its own capacity and was accepted
first time.

## What was done today

The API now runs on that service in the same region as its database, with two always-on
instances, so it never sleeps and a release replaces one instance at a time while the other
keeps serving. Backups were strengthened at the same time: 35-day point-in-time restore,
weekly backups kept twelve weeks and monthly ones twelve months, and the document storage is
now copied to a second region with fourteen-day recovery of anything deleted. Production
switched over at 13:04 with no measurable interruption.

## What it measured

| | Before | After |
|---|---|---|
| First click after twenty idle minutes | 10.7 s | 1.4 s |
| Every click after that | | 0.35 s |
| Mail triage page | 17 s | 8 s |
| A release | ~10 s outage for everyone | no failed request |

The triage page's remaining eight seconds is a live read from the mailbox itself, not the
hosting; the improvement for that is already designed and is a separate piece of work.

## What it costs

August's Azure bill was £216.90, of which the SQL database was £209.55. The move adds about
£45 to £49 a month: two always-on instances about £33 when idle and a few pounds more when
busy, backups £1 to £2, geo-redundant document storage £2 to £3, and a small storage account
under £1. Allowing a full month of the connector host's plan, which billed only days of August,
the expected bill from October is **about £270 a month before VAT, about £325 with VAT**.

Nigel approved an increase of £35 to £45; two instances put us at the top of that range, and
the second instance is what makes releases invisible to users. The refused App Service plan
would have cost £94 a month for one instance. The database is nearly four fifths of the bill;
if cost ever matters more than headroom, that is where to look, as a separate decision.

## What is next

A £350 monthly budget with alerts (Azure budgets count cost before VAT), the alert rules for
failures and sign-in problems, a rehearsal of a restore next weekend, a review of the first
week's readings, and the end-to-end regression test on the new hosting.
