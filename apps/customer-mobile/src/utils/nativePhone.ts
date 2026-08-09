import { Linking } from 'react-native';

import { logger } from '@/utils/logger';
import {
  normalizeDialablePhone,
  PHONE_APP_UNAVAILABLE,
  PHONE_UNAVAILABLE,
  toTelUri,
} from '@/utils/nativePhoneLogic';

export {
  normalizeDialablePhone,
  PHONE_APP_UNAVAILABLE,
  PHONE_UNAVAILABLE,
  toTelUri,
} from '@/utils/nativePhoneLogic';

export async function openNativeDialer(rawPhone: string | null | undefined): Promise<void> {
  const phone = normalizeDialablePhone(rawPhone);
  if (!phone) {
    throw new Error(PHONE_UNAVAILABLE);
  }

  const uri = toTelUri(phone);
  try {
    const canOpen = await Linking.canOpenURL(uri);
    if (!canOpen) {
      logger.warn('phone.dialer', 'canOpenURL returned false; attempting openURL', { uri });
    }
    await Linking.openURL(uri);
  } catch (error) {
    if (error instanceof Error && error.message === PHONE_UNAVAILABLE) {
      throw error;
    }
    logger.error('phone.dialer', 'Failed to open native dialer', error);
    throw new Error(PHONE_APP_UNAVAILABLE);
  }
}
