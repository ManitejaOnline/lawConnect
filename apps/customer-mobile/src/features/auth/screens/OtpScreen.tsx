import React, { useEffect, useRef, useState } from 'react';
import { View } from 'react-native';
import { useMutation } from '@tanstack/react-query';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { Button } from '@/components/ui/Button';
import { OtpInput } from '@/components/ui/OtpInput';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import type { AuthStackParamList } from '@/navigation/types';
import { authApi } from '@/services/api/authApi';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';

type Props = NativeStackScreenProps<AuthStackParamList, 'Otp'>;

export function OtpScreen({ navigation, route }: Props) {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const phoneNumber = route.params.phoneNumber;
  const [code, setCode] = useState('');
  const [formError, setFormError] = useState<string | null>(null);
  const submittedCode = useRef<string | null>(null);

  const verify = useMutation({
    mutationFn: (otp: string) => authApi.verifyOtp(phoneNumber, otp),
    onSuccess: () => {
      setFormError(null);
      showToast({ title: 'Signed in', message: 'Welcome to LawConnect.', variant: 'success' });
    },
    onError: (error) => {
      const message = getErrorMessage(error, 'Invalid or expired code');
      setFormError(message);
      showToast({ title: 'Verification failed', message, variant: 'error' });
    },
  });

  const resend = useMutation({
    mutationFn: () => authApi.requestOtp({ phoneNumber }),
    onSuccess: () => {
      setCode('');
      setFormError(null);
      submittedCode.current = null;
      showToast({ title: 'Code sent', message: 'Check your messages for a new OTP.', variant: 'success' });
    },
    onError: (error) => {
      showToast({
        title: 'Could not resend',
        message: getErrorMessage(error, 'Try again in a moment.'),
        variant: 'error',
      });
    },
  });

  const verifyOtp = verify.mutate;
  useEffect(() => {
    if (code.length !== 6 || submittedCode.current === code) {
      return;
    }
    submittedCode.current = code;
    verifyOtp(code);
  }, [code, verifyOtp]);

  return (
    <Screen
      edges={['top', 'left', 'right']}
      scroll
      keyboard
      footer={
        <View style={{ gap: theme.spacing.sm }}>
          <Button
            title="Verify"
            variant="accent"
            loading={verify.isPending}
            disabled={verify.isPending || code.length !== 6}
            onPress={() => verify.mutate(code)}
            accessibilityHint="Verify the one-time password"
          />
          <Button
            title="Resend code"
            variant="ghost"
            loading={resend.isPending}
            disabled={resend.isPending || verify.isPending}
            onPress={() => resend.mutate()}
          />
        </View>
      }
    >
      <ScreenHeader
        title="Enter OTP"
        subtitle={phoneNumber}
        onBack={navigation.canGoBack() ? () => navigation.goBack() : undefined}
      />

      <View style={{ gap: theme.spacing.md, marginTop: theme.spacing.xs }}>
        <Text variant="body" color="secondary">
          We sent a 6-digit verification code to your phone. Enter it to continue.
        </Text>
        <OtpInput
          value={code}
          onChange={(next) => {
            setFormError(null);
            setCode(next);
          }}
          error={formError ?? undefined}
          editable={!verify.isPending}
        />
      </View>
    </Screen>
  );
}
