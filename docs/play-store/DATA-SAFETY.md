# Play Store release materials

LawConnect customer Android package: `com.lawconnect.customer`

## Privacy Policy URL

**BLOCKED — real public Privacy Policy URL still required.**

This repository does not host a public privacy policy. Do not submit to Google Play until a
HTTPS privacy policy is published on a domain you control and that URL is entered in Play Console
and (optionally) `expo.extra.playStore.privacyPolicyUrl` in `apps/customer-mobile/app.json`.

Do not treat a local HTML file as a Play-compliant privacy policy.

## Data safety (Play Console)

Complete the Play Console Data safety form from the live app behavior. Suggested starting map
(confirm against production before submitting):

| Data type | Collected | Shared | Purpose |
| --- | --- | --- | --- |
| Name, email, phone | Yes | No (except assigned lawyer / case parties via product APIs) | Account |
| Location | Yes (when SOS / nearby lawyers) | With matching lawyers for SOS | App functionality |
| Photos / files | Yes (profile, case documents) | With authorized case parties | App functionality |
| Device IDs | Yes (session device identifier) | No | App functionality / security |

Encryption in transit: HTTPS to `https://lawconnect-uddw.onrender.com`.

## Permission disclosure (Android)

Copy already in `app.json` / Expo plugins (keep in Play listing as well):

- Camera: video consultations
- Microphone: voice and video calls
- Location: show nearby lawyers / emergency matching
- Photos: profile picture
- Documents: case files (document picker)

## Data safety contacts

Privacy inquiries: publish on the same site as the privacy policy. Do not invent an email here.
