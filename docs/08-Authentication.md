# 08 Authentication

## Overview

Passwordless phone authentication using OTP, JWT access tokens, and rotating refresh tokens.

## Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/auth/otp/request` | Anonymous | Request OTP (rate limited) |
| POST | `/api/auth/otp/verify` | Anonymous | Verify OTP, issue tokens, track device |
| POST | `/api/auth/refresh` | Anonymous | Rotate refresh token |
| POST | `/api/auth/logout` | Anonymous | Revoke refresh token |
| GET | `/api/auth/me` | Bearer | Current auth profile |

## Security

- OTP hashed with HMAC-SHA256 (never stored plaintext)
- OTP expiration and max attempts
- One-time OTP (marked used on success/expiry/max attempts)
- Secure random OTP and refresh tokens (`RandomNumberGenerator`)
- Refresh token rotation with reuse detection (revokes all sessions)
- ASP.NET rate limiting on auth endpoints + distributed OTP request throttling
- Role claims embedded in JWT (`Customer` / `Lawyer` / `Admin`)

## First login

Creates a `User` with the `Customer` role (passwordless). Does not create Lawyer/Customer profile APIs.

## Configuration

See `Otp` and `Jwt` sections in `appsettings*.json`.
