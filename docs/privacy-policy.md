# LawConnect Privacy Policy

Last updated: 17 October 2026

The same text is served by the API at `/privacy`.

LawConnect is a consultation app. This policy describes what the Android app and its API store.

## Account data

Registration asks for your name, email address, phone number, and a password. Passwords are stored only as a one-way hash. Refresh tokens are stored only as an HMAC hash and are rotated on each refresh. Signing in uses email and password. SMS one-time codes are not used in production.

## Documents

Documents you share from the customer app stay on that phone. The server stores the file name, type, size, and a document ID. A lawyer can receive the file only while you are online and approve the transfer. The API relays the bytes in memory and does not keep a copy. Files a lawyer receives are saved on the lawyer's phone.

Separate case-file uploads, when used, are stored on the server's local disk. That disk is not a paid cloud file store. Those files are not the phone-to-phone documents.

## Other information

The app may use your location, while you allow it, to show nearby lawyers. Search results do not publish a lawyer's exact coordinates. Appointment, case, and chat records are kept so both people in a matter can see the history.

Payments are not enabled. The app does not collect card numbers.

## Account deletion

You can delete your account from Profile. Deletion disables the account immediately, revokes sessions, removes the password, and clears the name, email, phone number, and profile photo from the account record. Phone-document metadata you own is removed. Appointments, cases, and messages already shared with another person are kept so that person's record remains intact, without your email or phone number attached to the deleted account.

## Contact

Questions about this policy can be sent through the Help & Support item in the app, or through the project repository at https://github.com/ManitejaOnline/lawConnect.
