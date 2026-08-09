import { contactsApi } from '@/services/api/contactsApi';
import { lawyersApi } from '@/services/api/lawyersApi';
import { useToastStore } from '@/store/toastStore';
import { openNativeDialer } from '@/utils/nativePhone';
import { PHONE_APP_UNAVAILABLE, PHONE_UNAVAILABLE } from '@/utils/nativePhoneLogic';

function showDialError(error: unknown): void {
  const dialerFailed = error instanceof Error && error.message === PHONE_APP_UNAVAILABLE;
  useToastStore.getState().show({
    title: dialerFailed ? PHONE_APP_UNAVAILABLE : PHONE_UNAVAILABLE,
    variant: 'error',
  });
}

/** Marketplace Call: authenticated customer → verified lawyer contact. */
export async function callLawyerById(lawyerId: string | null | undefined): Promise<void> {
  if (!lawyerId?.trim()) {
    useToastStore.getState().show({ title: PHONE_UNAVAILABLE, variant: 'error' });
    return;
  }

  try {
    const contact = await lawyersApi.getContact(lawyerId);
    await openNativeDialer(contact.phoneNumber);
  } catch (error) {
    showDialError(error);
  }
}

/** SOS / appointment counterpart by user id. */
export async function callUserById(userId: string | null | undefined): Promise<void> {
  if (!userId?.trim()) {
    useToastStore.getState().show({ title: PHONE_UNAVAILABLE, variant: 'error' });
    return;
  }

  try {
    const contact = await contactsApi.getCallContact(userId);
    await openNativeDialer(contact.phoneNumber);
  } catch (error) {
    showDialError(error);
  }
}
