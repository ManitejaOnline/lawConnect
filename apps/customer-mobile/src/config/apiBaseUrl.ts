import Constants from 'expo-constants';
import { Platform } from 'react-native';

import {
  API_DEVICE_NOT_CONFIGURED,
  isExpoBundlerUrl,
  isLoopbackApiUrl,
  readEnvApiUrl,
} from '@/config/apiBaseUrlLogic';
import { logger } from '@/utils/logger';

export {
  API_DEVICE_NOT_CONFIGURED,
  isExpoBundlerUrl,
  isLoopbackApiUrl,
  readEnvApiUrl,
} from '@/config/apiBaseUrlLogic';

let loggedResolvedOrigin: string | null = null;

function isPhysicalNativeDevice(): boolean {
  if (Platform.OS === 'web') return false;
  return Constants.isDevice === true;
}

function toPublicApiOrigin(url: string): string {
  try {
    const parsed = new URL(url);
    return `${parsed.protocol}//${parsed.host}`;
  } catch {
    return '(invalid-api-origin)';
  }
}

/** Display-only Expo Go / Metro URL. Never used for Axios or SignalR. */
export function getExpoBundleUrl(): string | null {
  const linking = Constants.linkingUri;
  if (linking && /^exp[a-z+]*:/i.test(linking)) {
    const match = linking.match(/^(exp[a-z+]*:\/\/[^/]+)/i);
    if (match) return match[1];
  }

  const host = Constants.expoConfig?.hostUri || Constants.expoGoConfig?.debuggerHost || null;
  if (!host) return null;
  if (/^exp[a-z+]*:/i.test(host)) return host.replace(/\/$/, '');
  return `exp://${host.replace(/\/$/, '')}`;
}

export function getApiConfigurationError(): string | null {
  const configured = readEnvApiUrl();

  if (configured && isExpoBundlerUrl(configured)) {
    return API_DEVICE_NOT_CONFIGURED;
  }

  if (Platform.OS === 'web') {
    return null;
  }

  if (!configured) {
    return isPhysicalNativeDevice() ? API_DEVICE_NOT_CONFIGURED : null;
  }

  if (isPhysicalNativeDevice() && isLoopbackApiUrl(configured)) {
    return API_DEVICE_NOT_CONFIGURED;
  }

  return null;
}

function reportApiOriginOnce(apiBaseUrl: string): void {
  const origin = toPublicApiOrigin(apiBaseUrl);
  if (loggedResolvedOrigin === origin) return;
  loggedResolvedOrigin = origin;
  console.info(`[LawConnect] apiOrigin=${origin}`);
}

/**
 * LawConnect HTTP API origin only.
 * Never returns an Expo Go / Metro `exp://` bundler URL.
 */
export function getApiBaseUrl(): string {
  const error = getApiConfigurationError();
  if (error) {
    logger.error('api', error, undefined, {
      configured: readEnvApiUrl(),
      expoBundleUrl: getExpoBundleUrl(),
      platform: Platform.OS,
      isDevice: Constants.isDevice,
    });
    throw new Error(error);
  }

  const configured = readEnvApiUrl();
  if (configured) {
    if ((typeof __DEV__ === 'undefined' || !__DEV__) && !configured.startsWith('https://')) {
      throw new Error(API_DEVICE_NOT_CONFIGURED);
    }
    reportApiOriginOnce(configured);
    return configured;
  }

  if (typeof __DEV__ !== 'undefined' && __DEV__) {
    const fallback =
      Platform.OS === 'web' ? 'http://localhost:5080' : 'http://192.168.1.38:5080';
    reportApiOriginOnce(fallback);
    return fallback;
  }

  throw new Error(API_DEVICE_NOT_CONFIGURED);
}
