namespace LawConnect.Api;

/// <summary>Public privacy policy served at <c>/privacy</c>.</summary>
public static class PrivacyPolicyPage
{
    /// <summary>HTML document returned by GET /privacy.</summary>
    public const string Html = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <title>LawConnect Privacy Policy</title>
          <style>
            body { font-family: Georgia, serif; max-width: 42rem; margin: 2rem auto; padding: 0 1rem; line-height: 1.5; color: #142033; }
            h1 { font-size: 1.6rem; }
          </style>
        </head>
        <body>
          <h1>LawConnect Privacy Policy</h1>
          <p>Last updated: 17 October 2026</p>
          <p>LawConnect is a consultation app. This policy describes what the Android app and its API store.</p>
          <h2>Account data</h2>
          <p>Registration asks for your name, email address, phone number, and a password. Passwords are stored only as a one-way hash. Refresh tokens are stored only as an HMAC hash and are rotated on each refresh. Signing in uses email and password. SMS one-time codes are not used in production.</p>
          <h2>Documents</h2>
          <p>Documents you share from the customer app stay on that phone. The server stores the file name, type, size, and a document ID. A lawyer can receive the file only while you are online and approve the transfer. The API relays the bytes in memory and does not keep a copy. Files a lawyer receives are saved on the lawyer's phone.</p>
          <p>Separate case-file uploads, when used, are stored on the server's local disk. That disk is not a paid cloud file store. Those files are not the phone-to-phone documents.</p>
          <h2>Other information</h2>
          <p>The app may use your location, while you allow it, to show nearby lawyers. Search results do not publish a lawyer's exact coordinates. Appointment, case, and chat records are kept so both people in a matter can see the history.</p>
          <p>Payments are not enabled. The app does not collect card numbers.</p>
          <h2>Account deletion</h2>
          <p>You can delete your account from Profile. Deletion disables the account immediately, revokes sessions, removes the password, and clears the name, email, phone number, and profile photo from the account record. Phone-document metadata you own is removed. Appointments, cases, and messages already shared with another person are kept so that person's record remains intact, without your email or phone number attached to the deleted account.</p>
          <h2>Contact</h2>
          <p>Questions about this policy can be sent through the Help &amp; Support item in the app, or through the project repository at https://github.com/ManitejaOnline/lawConnect.</p>
        </body>
        </html>
        """;
}
