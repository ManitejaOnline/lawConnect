# 17 Native phone calling

LawConnect does not provide in-app voice or video calling.

When a customer taps **Call Lawyer** on a verified marketplace profile, the app:

1. Calls authenticated `GET /api/lawyers/{lawyerId}/contact`
2. Opens the device dialer with `Linking.openURL(\`tel:${phoneNumber}\`)`

The number comes from `User.PhoneNumber` (not the `Lawyer` row). Public `GET /api/lawyers/search` and `GET /api/lawyers/{id}` never include phone numbers.

SOS / appointment counterpart dialing still uses `GET /api/contacts/{userId}` when both parties share an assigned emergency or a non-cancelled appointment.
