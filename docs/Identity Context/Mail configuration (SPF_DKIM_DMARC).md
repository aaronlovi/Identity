
[Games Platform](..\..\..\Games%20Platform.md) > [2025-06](..\..\2025-06.md) > [Identity Context](..\Identity%20Context.md)

# Mail configuration (SPF/DKIM/DMARC)

- [1. Overview](#key-1-overview)
- [2. Pick the system that will send your mail](#key-2-pick-the-system-that-will-send-your-mail)
- [3. Add an SPF TXT record](#key-3-add-an-spf-txt-record)
- [4. Generate a DKIM key pair](#key-4-generate-a-dkim-key-pair)
- [5. Publish the DKIM public key](#key-5-publish-the-dkim-public-key)
- [6. (Recommended) Add DMARC to complete the trio](#key-6-recommended-add-dmarc-to-complete-the-trio)
- [7. Verify and troubleshoot](#key-7-verify-and-troubleshoot)
- [8. Recap](#key-8-recap)

# 1. Overview

The following outline is for sending password-reset and verification emails so that they do not land in a user’s junk email folder. Here’s the condensed, vendor-agnostic playbook. The exact UI clicks differ at GoDaddy vs. Cloudflare vs. AWS Route 53, but the elements are identical.

# 2. Pick the system that will send your mail

|  Scenario                                                                       |  Typical sender hostname to “include”                                    |
|:--------------------------------------------------------------------------------|:-------------------------------------------------------------------------|
| You’ll run your own SMTP on the same domain                                     | none (list the server’s public IP instead)                               |
| You’ll relay through a SaaS ESP (SendGrid, Mailgun, Postmark, Amazon SES, etc.) | `include:sendgrid.net`, `include:spf.mailgun.org`, … (the ESP tells you) |

> Keep this list short; each added include widens who can impersonate you.

---

# 3. Add an **SPF** TXT record

1. Open your DNS zone editor.
2. **Name / Host**: leave empty (or `@`) so it applies to the root domain.
3. **Value** (examples):  
   *Own SMTP*

   ```java
   v=spf1 ip4:203.0.113.25 -all
   ```

   *SendGrid + fall-back Google Workspace*

   ```java
   v=spf1 include:_spf.google.com include:sendgrid.net ~all
   ```

   *Rules of thumb*

   - `v=spf1 …` must be first.
   - End with `-all` (hard fail) or `~all` (soft fail).
   - Publish **one** SPF record; merge all mechanisms into it.

Propagation is just DNS TTL—usually minutes on Cloudflare, up to 24 h elsewhere.

---

# 4. Generate a **DKIM** key pair

How you do this depends on the sender:

|  Sender                           |  Where to click                                                                                          |
|:----------------------------------|:---------------------------------------------------------------------------------------------------------|
| **SendGrid / Mailgun / Postmark** | Dashboard → *Domain settings* → “Add DKIM / domain authentication” – they generate and show DNS records. |
| **Amazon SES**                    | Console → *Verified identities* → select domain → *Create DKIM records*.                                 |
| **Your own Postfix / Exim**       | Run `opendkim-genkey -s mail -d example.com` (creates `mail.private` and `mail.txt`).                    |

You’ll get:

- a **selector** (`mail`, `s1`, `ses`, …)
- a **public key** block (long base64 string)
- optional “policy” flags (e.g., `t=s` for testing)

---

# 5. Publish the DKIM public key

1. Add a **new TXT record**

   - **Name / Host**: `<selector>._domainkey` → e.g., `mail._domainkey`
   - **Value**:

     ```java
     v=DKIM1; k=rsa; p=MIIBIjANBgkq…IDAQAB
     ```
2. Save. Wait for DNS to propagate.

You can publish multiple selectors (e.g., `s1`, `s2`) so you can rotate keys without downtime.

---

# 6. (Recommended) Add **DMARC** to complete the trio

DMARC tells receivers what to do if SPF or DKIM fails and emails claim to be from you.

```java
Type: TXT Name: _dmarc Value: v=DMARC1; p=quarantine; rua=mailto:dmarc-reports@example.com; pct=100 
```

- `p=none` (monitor), `quarantine`, or `reject`.
- `rua` gets aggregate failure reports.

---

# 7. Verify and troubleshoot

- Use **Google Admin Toolbox → Check MX** or `dig`/`nslookup` to confirm the TXT records are visible.
- Send an email to a Gmail address, open “Show original” → you should see **PASS** for SPF **and** DKIM.
- If SPF shows **permerror** you probably have two separate SPF TXT records—merge them.

---

# 8. Recap

1. Publish exactly **one** SPF TXT with all your senders.
2. Publish at least one DKIM TXT under `<selector>._domainkey.domain`.
3. Optionally publish DMARC to specify enforcement.
4. Confirm with any DMARC/SPF/DKIM validator—done!

Once these records pass, password-reset and verification mails will reliably land in users’ inboxes instead of spam.
