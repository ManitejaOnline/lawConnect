import axios from 'axios';

import { apiClient } from './client';
import type {
  AuthLoginResponse,
  AuthTokenResponse,
  CurrentAuthUserResponse,
  RequestOtpPayload,
  RequestOtpResponse,
  VerifyOtpPayload,
} from '@/types/api';
import { getApiBaseUrl } from '@/config/apiBaseUrl';
import { ensureDeviceIdentifier, getDevicePlatform } from '@/utils/device';
import { normalizePhoneToE164 } from '@/utils/phone';
import { useAuthStore, type AuthSession } from '@/store/authStore';

async function applyLogin(login: AuthLoginResponse) {
  useAuthStore.getState().setSession(toSession(login));
  useAuthStore.getState().setProfile({
    userId: String(login.userId),
    phoneNumber: login.phoneNumber,
    firstName: '',
    lastName: '',
    roles: [...login.roles],
  });
  try {
    await authApi.me();
  } catch {
    // Profile hydrate is best-effort; session is already stored.
  }
  return login;
}

function toSession(login: AuthLoginResponse): AuthSession {
  return {
    userId: String(login.userId),
    phoneNumber: login.phoneNumber,
    roles: [...login.roles],
    accessToken: login.tokens.accessToken,
    accessTokenExpiresAtUtc: login.tokens.accessTokenExpiresAtUtc,
    refreshToken: login.tokens.refreshToken,
    refreshTokenExpiresAtUtc: login.tokens.refreshTokenExpiresAtUtc,
  };
}

export const authApi = {
  requestOtp: async (payload: RequestOtpPayload) => {
    const { data } = await apiClient.post<RequestOtpResponse>('/api/auth/otp/request', payload);
    return data;
  },

  registerCustomer: async (payload: {
    fullName: string;
    email: string;
    phoneNumber: string;
    password: string;
  }) => {
    const deviceIdentifier = await ensureDeviceIdentifier();
    const { data } = await apiClient.post<AuthLoginResponse>('/api/auth/register/customer', {
      ...payload,
      deviceIdentifier,
      platform: getDevicePlatform(),
    });
    return applyLogin(data);
  },

  registerLawyer: async (payload: {
    fullName: string;
    email: string;
    phoneNumber: string;
    password: string;
  }) => {
    const deviceIdentifier = await ensureDeviceIdentifier();
    const { data } = await apiClient.post<AuthLoginResponse>('/api/auth/register/lawyer', {
      ...payload,
      deviceIdentifier,
      platform: getDevicePlatform(),
    });
    useAuthStore.getState().setIntendedAudience('lawyer');
    return applyLogin(data);
  },

  loginWithPassword: async (identifier: string, password: string) => {
    const deviceIdentifier = await ensureDeviceIdentifier();
    const trimmed = identifier.trim();
    const normalized = trimmed.includes('@') ? trimmed : normalizePhoneToE164(trimmed);
    const { data } = await apiClient.post<AuthLoginResponse>('/api/auth/login', {
      identifier: normalized,
      password,
      deviceIdentifier,
      platform: getDevicePlatform(),
    });
    return applyLogin(data);
  },

  /** Development-only passwordless login. Production API returns 404 for this route. */
  loginWithPhone: async (phoneNumber: string) => {
    if (typeof __DEV__ === 'undefined' || !__DEV__) {
      throw new Error('Phone login without a password is not available.');
    }
    const deviceIdentifier = await ensureDeviceIdentifier();
    const { data } = await apiClient.post<AuthLoginResponse>('/api/auth/dev-login', {
      phoneNumber,
      deviceIdentifier,
      platform: getDevicePlatform(),
    });
    useAuthStore.getState().setSession(toSession(data));
    useAuthStore.getState().setProfile({
      userId: String(data.userId),
      phoneNumber: data.phoneNumber,
      firstName: '',
      lastName: '',
      roles: [...data.roles],
    });
    return data;
  },

  verifyOtp: async (phoneNumber: string, code: string) => {
    const deviceIdentifier = await ensureDeviceIdentifier();
    const payload: VerifyOtpPayload = {
      phoneNumber,
      code,
      deviceIdentifier,
      platform: getDevicePlatform(),
    };
    const { data } = await apiClient.post<AuthLoginResponse>('/api/auth/otp/verify', payload);
    useAuthStore.getState().setSession(toSession(data));
    useAuthStore.getState().setProfile({
      userId: String(data.userId),
      phoneNumber: data.phoneNumber,
      firstName: '',
      lastName: '',
      roles: [...data.roles],
    });
    return data;
  },

  me: async () => {
    const { data } = await apiClient.get<CurrentAuthUserResponse>('/api/auth/me');
    useAuthStore.getState().setProfile({
      userId: String(data.userId),
      phoneNumber: data.phoneNumber,
      firstName: data.firstName,
      lastName: data.lastName,
      roles: [...data.roles],
    });
    return data;
  },

  /**
   * Rotates refresh token using a bare axios call (avoids interceptor recursion).
   */
  refresh: async (): Promise<AuthSession | null> => {
    const current = useAuthStore.getState().session;
    if (!current?.refreshToken) {
      return null;
    }

    const deviceIdentifier = await ensureDeviceIdentifier();
    const { data } = await axios.post<AuthTokenResponse>(
      `${getApiBaseUrl()}/api/auth/refresh`,
      {
        refreshToken: current.refreshToken,
        deviceIdentifier,
        platform: getDevicePlatform(),
      },
      {
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        timeout: 60000,
      },
    );

    const next: AuthSession = {
      ...current,
      accessToken: data.accessToken,
      accessTokenExpiresAtUtc: data.accessTokenExpiresAtUtc,
      refreshToken: data.refreshToken,
      refreshTokenExpiresAtUtc: data.refreshTokenExpiresAtUtc,
    };
    useAuthStore.getState().setSession(next);
    return next;
  },

  /**
   * Validates persisted session on cold start.
   */
  restoreSession: async (): Promise<boolean> => {
    const session = useAuthStore.getState().session;
    if (!session?.refreshToken) {
      return false;
    }

    try {
      await authApi.refresh();
      const me = await authApi.me();
      const current = useAuthStore.getState().session;
      if (current) {
        useAuthStore.getState().setSession({ ...current, roles: [...me.roles] });
      }
      return true;
    } catch {
      useAuthStore.getState().clearSession();
      return false;
    }
  },

  deleteAccount: async () => {
    await apiClient.delete('/api/auth/account');
    useAuthStore.getState().clearSession();
    const { stopEmergencyHub } = await import('@/services/realtime/emergencyHub');
    await stopEmergencyHub();
  },

  logout: async () => {
    const session = useAuthStore.getState().session;
    if (session?.refreshToken) {
      try {
        await apiClient.post('/api/auth/logout', { refreshToken: session.refreshToken });
      } catch {
        // Best-effort logout; always clear local session.
      }
    }
    useAuthStore.getState().clearSession();
    const { stopEmergencyHub } = await import('@/services/realtime/emergencyHub');
    await stopEmergencyHub();
  },
};
