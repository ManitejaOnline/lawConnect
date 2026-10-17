import React, { useState } from 'react';
import { View } from 'react-native';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { BrandMark } from '@/components/brand/BrandMark';
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

const schema = z.object({
  identifier: z.string().trim().email('Enter a valid email'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
});

type FormValues = z.infer<typeof schema>;
type Props = NativeStackScreenProps<AuthStackParamList, 'Login'>;

export function LoginScreen({ navigation }: Props) {
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
    defaultValues: { identifier: '', password: '' },
  });

  const login = useMutation({
    mutationFn: (values: FormValues) => authApi.loginWithPassword(values.identifier.trim(), values.password),
    onSuccess: () => {
      setFormError(null);
      showToast({ title: 'Signed in', variant: 'success' });
    },
    onError: (error) => {
      const message = getErrorMessage(error, 'Could not sign in');
      setFormError(message);
      showToast({ title: 'Sign-in failed', message, variant: 'error' });
    },
  });

  return (
    <Screen
      edges={['top', 'left', 'right']}
      scroll
      keyboard
      footer={
        <Button
          title="Sign in"
          variant="primary"
          loading={login.isPending}
          disabled={login.isPending}
          onPress={handleSubmit((values) => login.mutate(values))}
        />
      }
    >
      <ScreenHeader title="Sign in" subtitle="Email and password" onBack={() => navigation.goBack()} />
      <View style={{ alignItems: 'center', marginBottom: theme.spacing.sm }}>
        <BrandMark size={56} />
      </View>
      <View style={{ gap: theme.spacing.md }}>
        <Controller
          control={control}
          name="identifier"
          render={({ field: { onChange, onBlur, value } }) => (
            <TextField
              label="Email"
              value={value}
              onChangeText={onChange}
              onBlur={onBlur}
              autoCapitalize="none"
              keyboardType="email-address"
              error={errors.identifier?.message || formError || undefined}
            />
          )}
        />
        <Controller
          control={control}
          name="password"
          render={({ field: { onChange, onBlur, value } }) => (
            <TextField
              label="Password"
              value={value}
              onChangeText={onChange}
              onBlur={onBlur}
              secureTextEntry
              error={errors.password?.message}
            />
          )}
        />
        <Text
          variant="bodyMedium"
          color="accent"
          onPress={() =>
            navigation.navigate(intendedAudience === 'lawyer' ? 'LawyerSignup' : 'CustomerSignup')
          }
        >
          Create an account
        </Text>
      </View>
    </Screen>
  );
}
