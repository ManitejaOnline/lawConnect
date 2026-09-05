import React, { useEffect, useState } from 'react';
import { View } from 'react-native';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { ErrorState } from '@/components/feedback/ErrorState';
import { LoadingState } from '@/components/feedback/LoadingState';
import { Button } from '@/components/ui/Button';
import { Text } from '@/components/ui/Text';
import { TextField } from '@/components/ui/TextField';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { homeQueryKeys, useUserProfileQuery } from '@/features/home/hooks/useHomeQueries';
import type { ProfileStackParamList } from '@/navigation/types';
import { usersApi } from '@/services/api/usersApi';
import { useAuthStore } from '@/store/authStore';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';

type Props = NativeStackScreenProps<ProfileStackParamList, 'EditProfile'>;

export function EditProfileScreen({ navigation }: Props) {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const queryClient = useQueryClient();
  const setProfile = useAuthStore((s) => s.setProfile);
  const authProfile = useAuthStore((s) => s.profile);
  const query = useUserProfileQuery();
  const current = query.data;

  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [dateOfBirth, setDateOfBirth] = useState('');
  const [gender, setGender] = useState('');
  const [preferredLanguage, setPreferredLanguage] = useState('');
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (!current) return;
    setFirstName(current.firstName ?? '');
    setLastName(current.lastName ?? '');
    setEmail(current.email ?? '');
    setDateOfBirth(current.dateOfBirth ? String(current.dateOfBirth).slice(0, 10) : '');
    setGender(current.gender ?? '');
    setPreferredLanguage(current.preferredLanguage ?? '');
  }, [current]);

  const save = useMutation({
    mutationFn: () => {
      const first = firstName.trim();
      const last = lastName.trim();
      if (first.length < 2 || last.length < 2) {
        throw new Error('Enter your first and last name.');
      }
      const dob = dateOfBirth.trim();
      if (dob && !/^\d{4}-\d{2}-\d{2}$/.test(dob)) {
        throw new Error('Date of birth must be YYYY-MM-DD.');
      }
      return usersApi.update({
        firstName: first,
        lastName: last,
        email: email.trim() || null,
        dateOfBirth: dob || null,
        gender: gender.trim() || null,
        preferredLanguage: preferredLanguage.trim() || null,
      });
    },
    onSuccess: (profile) => {
      setFormError(null);
      void queryClient.setQueryData(homeQueryKeys.profile(), profile);
      setProfile({
        userId: profile.userId,
        phoneNumber: profile.phoneNumber,
        firstName: profile.firstName,
        lastName: profile.lastName,
        roles: profile.roles?.length ? profile.roles : authProfile?.roles ?? [],
      });
      showToast({ title: 'Profile updated', variant: 'success' });
      navigation.goBack();
    },
    onError: (error) => {
      const message = getErrorMessage(error, 'Could not save profile');
      setFormError(message);
      showToast({ title: 'Could not save', message, variant: 'error' });
    },
  });

  if (query.isLoading && !current) {
    return (
      <Screen edges={['top', 'left', 'right', 'bottom']}>
        <ScreenHeader title="Edit profile" onBack={() => navigation.goBack()} />
        <LoadingState label="Loading profile" />
      </Screen>
    );
  }

  if (query.isError && !current) {
    return (
      <Screen edges={['top', 'left', 'right', 'bottom']}>
        <ScreenHeader title="Edit profile" onBack={() => navigation.goBack()} />
        <ErrorState message="Could not load your profile." onRetry={() => query.refetch()} />
      </Screen>
    );
  }

  return (
    <Screen scroll keyboard edges={['top', 'left', 'right', 'bottom']}>
      <ScreenHeader title="Edit profile" subtitle="Your account details" onBack={() => navigation.goBack()} />
      <View style={{ gap: theme.spacing.md }}>
        <TextField label="First name" value={firstName} onChangeText={setFirstName} autoCapitalize="words" />
        <TextField label="Last name" value={lastName} onChangeText={setLastName} autoCapitalize="words" />
        <TextField
          label="Email"
          value={email}
          onChangeText={setEmail}
          keyboardType="email-address"
          autoCapitalize="none"
        />
        <TextField
          label="Date of birth"
          value={dateOfBirth}
          onChangeText={setDateOfBirth}
          placeholder="YYYY-MM-DD"
        />
        <TextField label="Gender" value={gender} onChangeText={setGender} placeholder="Optional" />
        <TextField
          label="Preferred language"
          value={preferredLanguage}
          onChangeText={setPreferredLanguage}
          placeholder="e.g. en"
          autoCapitalize="none"
        />
        {formError ? (
          <Text variant="caption" color="danger">
            {formError}
          </Text>
        ) : null}
        <Button title="Save" variant="primary" loading={save.isPending} onPress={() => save.mutate()} />
      </View>
    </Screen>
  );
}
