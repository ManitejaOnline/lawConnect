# LawConnect mobile — local development

This app targets **Expo SDK 54**, the version currently supported by **Play Store Expo Go**.

| What | Example | Used for |
|---|---|---|
| Metro / Expo Go | `exp://...` or `*.exp.direct` | Load JS into Expo Go |
| LawConnect API | `http://192.168.x.x:5080` | Axios, SignalR, auth |

Never set `EXPO_PUBLIC_API_URL` to an `exp://` URL, `*.exp.direct`, or port `8081`.

## Physical phone (Expo Go)

1. Start the API on port **5080** (`http` launch profile). Bind all interfaces so the phone can reach it (`http://0.0.0.0:5080`).
2. Find the PC Wi-Fi IPv4 address: `ipconfig` → **Wireless LAN adapter Wi-Fi** → **IPv4 Address**.
3. In `apps/customer-mobile/.env` (copy from `.env.example`):

   ```
   EXPO_PUBLIC_API_URL=http://<PC-LAN-IP>:5080
   ```

4. Phone and PC on the **same Wi-Fi** (not guest / VPN / mobile data).
5. Allow **TCP 5080** inbound in Windows Firewall if the phone cannot connect.
6. Start Metro with a tunnel:

   ```
   npx expo start --tunnel
   ```

7. In Play Store **Expo Go**, paste the `exp://...` URL from the terminal (or scan the QR code). API calls go to `EXPO_PUBLIC_API_URL`.

If the API URL is missing or is `localhost` on a physical device, the app shows:

**API server is not configured for this device.**

In development, Metro logs `[api] Resolved API base URL` with `apiBaseUrl` and `expoBundlerHost` so you can confirm they differ.

## Web / iOS Simulator

`EXPO_PUBLIC_API_URL=http://localhost:5080` is valid. The browser/simulator shares the PC loopback.

Android emulator should use `http://10.0.2.2:5080` (host loopback), not `localhost`.

## Production

Set `EXPO_PUBLIC_API_URL` to the real HTTPS API origin before creating a release build.
