---
name: technologies-fa-integrations-questions
description: "Does a headless-filled cart show in the Woolworths app; no rate limiting on a third-party key that is not ours; no agreement or notice channel with Woolworths at all"
type: question
last_verified: 2026-08-21
---

# Open questions

## Does a headless-filled cart appear in the Woolworths mobile app?
The basket is account-bound and survived sign-out during testing, so it should.
Not confirmed on a phone. This matters because the whole Siri Shortcut flow ends
with "open the cart" — if the app shows an empty cart, the user experience is
broken even though the API worked.

## No rate limiting, on a key that is not ours
The service is open by design. The anonymous search endpoint proxies
Constructor.io using a **public client key lifted from the Woolworths
storefront**. Abuse of that key through our hostname is the exposure worth
thinking about before `integrations.frostaura.net` is public — the consequence
lands on Woolworths' search contract, not on us, which makes it worse rather
than better.

## Will Woolworths tolerate this traffic pattern?
Unknown. There is no agreement, no documented API, no terms covering it, and no
notice channel if they change or block it. Everything here was established by
observing the site. Treat the whole integration as something that can break
without warning, and do not build anything user-facing that fails badly when it
does.

An unresolved judgement call sits behind this: the service automates a customer
acting on their own account with their own credentials, which is materially
different from scraping, but it is still undocumented use. Worth a deliberate
decision before the service is public rather than after.

## Should the write path require an explicit confirmation step?
Currently `woolworths_add_shopping_list_to_cart` adds up to ~15 items in one
call with no confirmation. It stops short of checkout, so nothing is purchased,
but an AI agent calling it will silently mutate a real cart. The
`preview` tool exists as the safe alternative; whether the write tool should
require a confirmation token is undecided.

## Related
[Current state](state.md) · [Decisions](decisions.md)
