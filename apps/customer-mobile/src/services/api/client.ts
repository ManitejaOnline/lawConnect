import axios, { AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';
import { Platform } from 'react-native';

import { useAuthStore, type AuthSession } from '@/store/authStore';
import { useToastStore } from '@/store/toastStore';
import type { AuthTokenResponse, RefreshTokenPayload } from '@/types/api';
import { AppError } from '@/utils/errors';
import { getApiBaseUrl } from '@/config/apiBaseUrl';
import { ensureDeviceIdentifier, getDevicePlatform } from '@/utils/device';

type RetriableConfig = InternalAxiosRequestConfig & {
  _retry?: boolean;
  _retryCount?: number;
};

let refreshPromise: Promise<AuthSession | null> | null = null;

const MAX_RETRIES = 2;

function sleep(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function mapAxiosError(error: AxiosError): AppError {
  if (!error.response) {
    return new AppError('Network unavailable. Check your connection.', {
      isNetworkError: true,
      cause: error,
    });
  }

  const data = error.response.data as { title?: string; detail?: string; errors?: Record<string, string[]> };
  const message = data?.detail || data?.title || error.message || 'Request failed';
  return new AppError(message, {
    status: error.response.status,
    details: data,
    cause: error,
  });
}

async function refreshSession(): Promise<AuthSession | null> {
  const current = useAuthStore.getState().session;
  if (!current?.refreshToken) {
    return null;
  }

  const deviceIdentifier = await ensureDeviceIdentifier();
  const payload: RefreshTokenPayload = {
    refreshToken: current.refreshToken,
    deviceIdentifier,
    platform: getDevicePlatform(),
  };

  const response = await axios.post<AuthTokenResponse>(`${getApiBaseUrl()}/api/auth/refresh`, payload, {
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    timeout: 15000,
  });

  const tokens = response.data;
  const next: AuthSession = {
    ...current,
    accessToken: tokens.accessToken,
    accessTokenExpiresAtUtc: tokens.accessTokenExpiresAtUtc,
    refreshToken: tokens.refreshToken,
    refreshTokenExpiresAtUtc: tokens.refreshTokenExpiresAtUtc,
  };
  useAuthStore.getState().setSession(next);
  return next;
}

function createApiClient(): AxiosInstance {
  const client = axios.create({
    timeout: 20000,
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      'X-Client': `LawConnect-Customer/${Platform.OS}`,
    },
  });

  client.interceptors.request.use(async (config) => {
    config.baseURL = getApiBaseUrl();
    const token = useAuthStore.getState().session?.accessToken;
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    if (typeof FormData !== 'undefined' && config.data instanceof FormData) {
      delete config.headers['Content-Type'];
    }
    return config;
  });

  client.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
      const original = error.config as RetriableConfig | undefined;
      const status = error.response?.status;

      if (status === 401 && original && !original._retry) {
        original._retry = true;
        try {
          refreshPromise ??= refreshSession().finally(() => {
            refreshPromise = null;
          });
          const session = await refreshPromise;
          if (!session) {
            useAuthStore.getState().clearSession();
            throw mapAxiosError(error);
          }
          original.headers.Authorization = `Bearer ${session.accessToken}`;
          return client(original);
        } catch {
          useAuthStore.getState().clearSession();
          useToastStore.getState().show({
            title: 'Session expired',
            message: 'Please sign in again.',
            variant: 'warning',
          });
          throw mapAxiosError(error);
        }
      }

      const retryCount = original?._retryCount ?? 0;
      const isRetriable =
        !error.response ||
        error.response.status === 408 ||
        error.response.status === 429 ||
        (error.response.status >= 500 && error.response.status !== 503);

      if (original && isRetriable && retryCount < MAX_RETRIES) {
        original._retryCount = retryCount + 1;
        await sleep(350 * 2 ** retryCount);
        return client(original);
      }

      const appError = mapAxiosError(error);
      if (appError.isNetworkError) {
        useToastStore.getState().show({
          title: 'You are offline',
          message: 'Changes will sync when connection returns.',
          variant: 'warning',
        });
      }
      throw appError;
    },
  );

  return client;
}

export const apiClient = createApiClient();
