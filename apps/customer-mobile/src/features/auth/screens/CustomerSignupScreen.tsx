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

const schema = z
  .object({
    fullName: z.string().min(2, 'Enter your full name'),
    email: z.string().email('Enter a valid email'),
    phoneNumber: z.string().min(8, 'Enter your phone number'),
    password: z.string().min(8, 'At least 8 characters'),
    confirmPassword: z.string().min(8, 'Confirm your password'),
  })
  .refine((values) => isValidE164(normalizePhoneToE164(values.phoneNumber)), {
    message: 'Use international format, e.g. +919876543210',
    path: ['phoneNumber'],
  })
  .refine((values) => values.password === values.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  });

type FormValues = z.infer<typeof schema>;
type Props = NativeStackScreenProps<AuthStackParamList, 'CustomerSignup'>;

export function CustomerSignupScreen({ navigation }: Props) {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const setIntendedAudience = useAuthStore((s) => s.setIntendedAudience);
  const [formError, setFormError] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      fullName: '',
      email: '',
      phoneNumber: '+91',
      password: '',
      confirmPassword: '',
    },
  });

  const signup = useMutation({
    mutationFn: async (values: FormValues) => {
      setIntendedAudience('customer');
      return authApi.registerCustomer({
        fullName: values.fullName.trim(),
        email: values.email.trim(),
        phoneNumber: normalizePhoneToE164(values.phoneNumber),
        password: values.password,
      });
    },
    onSuccess: () => {
      setFormError(null);
      showToast({ title: 'Account created', variant: 'success' });
    },
    onError: (error) => {
      const message = getErrorMessage(error, 'Could not create account');
      setFormError(message);
      showToast({ title: 'Sign up failed', message, variant: 'error' });
    },
  });

  return (
    <Screen
      edges={['top', 'left', 'right']}
      scroll
      keyboard
      footer={
        <Button
          title="Create account"
          variant="primary"
          loading={signup.isPending}
          disabled={signup.isPending}
          onPress={handleSubmit((values) => signup.mutate(values))}
        />
      }
    >
      <ScreenHeader title="Sign up" subtitle="Create your LawConnect account" onBack={() => navigation.goBack()} />
      <View style={{ gap: theme.spacing.md }}>
        <Controller
          control={control}
          name="fullName"
          render={({ field: { onChange, onBlur, value } }) => (
            <TextField label="Full name" value={value} onChangeText={onChange} onBlur={onBlur} error={errors.fullName?.message} />
          )}
        />
        <Controller
          control={control}
          name="email"
          render={({ field: { onChange, onBlur, value } }) => (
            <TextField
              label="Email"
              value={value}
              onChangeText={onChange}
              onBlur={onBlur}
              autoCapitalize="none"
              keyboardType="email-address"
              error={errors.email?.message}
            />
          )}
        />
        <Controller
          control={control}
          name="phoneNumber"
          render={({ field: { onChange, onBlur, value } }) => (
            <TextField
              label="Phone"
              value={value}
              onChangeText={onChange}
              onBlur={onBlur}
              keyboardType="phone-pad"
              error={errors.phoneNumber?.message || formError || undefined}
            />
          )}
        />
        <Controller
          control={control}
          name="password"
          render={({ field: { onChange, onBlur, value } }) => (
            <TextField label="Password" value={value} onChangeText={onChange} onBlur={onBlur} secureTextEntry error={errors.password?.message} />
          )}
        />
        <Controller
          control={control}
          name="confirmPassword"
          render={({ field: { onChange, onBlur, value } }) => (
            <TextField
              label="Confirm password"
              value={value}
              onChangeText={onChange}
              onBlur={onBlur}
              secureTextEntry
              error={errors.confirmPassword?.message}
            />
          )}
        />
        <Text variant="bodyMedium" color="accent" onPress={() => navigation.navigate('Login')}>
          Already have an account? Sign in
        </Text>
      </View>
    </Screen>
  );
}
