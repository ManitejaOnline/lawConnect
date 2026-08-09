export type ApiProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
};

export type AuthTokenResponse = {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
};

export type AuthLoginResponse = {
  userId: string;
  phoneNumber: string;
  roles: string[];
  isNewUser: boolean;
  tokens: AuthTokenResponse;
};

export type RequestOtpResponse = {
  destination: string;
  expiresAtUtc: string;
  expiresInSeconds: number;
};

export type CurrentAuthUserResponse = {
  userId: string;
  phoneNumber: string;
  firstName: string;
  lastName: string;
  roles: string[];
};

export type RequestOtpPayload = {
  phoneNumber: string;
};

export type VerifyOtpPayload = {
  phoneNumber: string;
  code: string;
  deviceIdentifier: string;
  platform: number;
  fcmToken?: string | null;
  appVersion?: string | null;
  osVersion?: string | null;
};

export type RefreshTokenPayload = {
  refreshToken: string;
  deviceIdentifier: string;
  platform: number;
  fcmToken?: string | null;
  appVersion?: string | null;
  osVersion?: string | null;
};
