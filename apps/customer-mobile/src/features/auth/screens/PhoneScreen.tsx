import React, { useState } from 'react';
import { View } from 'react-native';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { Button } from '@/components/ui/Button';
import { Text } from '@/components/ui/Text';
import { TextField } from '@/components/ui/TextField';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import type { AuthStackParamList } from '@/navigation/types';
import { authApi } from '@/services/api/authApi';
import { useAuthStore } from '@/store/authStore';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';
import { isValidE164, normalizePhoneToE164 } from '@/utils/phone';

const schema = z.object({
  phoneNumber: z
    .string()
    .min(8, 'Enter your phone number')
    .refine((value) => isValidE164(normalizePhoneToE164(value)), {
      message: 'Use international format, e.g. +919876543210',
    }),
});

type FormValues = z.infer<typeof schema>;
type Props = NativeStackScreenProps<AuthStackParamList, 'Phone'>;

export function PhoneScreen({ navigation }: Props) {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const intendedAudience = useAuthStore((s) => s.intendedAudience);
  const [formError, setFormError] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { phoneNumber: '+91' },
    mode: 'onSubmit',
  });

  const login = useMutation({
    mutationFn: async (phoneNumber: string) => authApi.loginWithPhone(phoneNumber),
    onSuccess: () => {
      setFormError(null);
      showToast({
        title: 'Signed in',
        message: 'OTP is turned off for testing.',
        variant: 'success',
      });
    },
    onError: (error) => {
      const message = getErrorMessage(error, 'Could not sign in');
      setFormError(message);
      showToast({ title: 'Sign-in failed', message, variant: 'error' });
    },
  });

  const onContinue = handleSubmit((values) => {
    const phoneNumber = normalizePhoneToE164(values.phoneNumber);
    login.mutate(phoneNumber);
  });

  const audienceLabel = intendedAudience === 'lawyer' ? 'lawyer' : 'user';

  return (
    <Screen
      edges={['top', 'left', 'right']}
      scroll
      keyboard
      footer={
        <Button
          title="Continue"
          variant="accent"
          loading={login.isPending}
          disabled={login.isPending}
          onPress={onContinue}
          accessibilityHint="Sign in with your phone number"
        />
      }
    >
      <ScreenHeader
        title="Your phone"
        subtitle={`Continue as ${audienceLabel}`}
        onBack={navigation.canGoBack() ? () => navigation.goBack() : undefined}
      />

      <View style={{ gap: theme.spacing.md, marginTop: theme.spacing.xs }}>
        <Text variant="body" color="secondary">
          Enter your mobile number to continue. OTP is turned off for testing.
        </Text>

        <Controller
          control={control}
          name="phoneNumber"
          render={({ field: { onChange, onBlur, value } }) => (
            <TextField
              label="Phone number"
              placeholder="+919876543210"
              keyboardType="phone-pad"
              autoComplete="tel"
              textContentType="telephoneNumber"
              leftIcon="phone"
              value={value}
              onChangeText={onChange}
              onBlur={onBlur}
              error={errors.phoneNumber?.message || formError || undefined}
              hint="Example: +919876543210"
              editable={!login.isPending}
              returnKeyType="done"
              onSubmitEditing={onContinue}
              accessibilityLabel="Phone number"
            />
          )}
        />
      </View>
    </Screen>
  );
}
