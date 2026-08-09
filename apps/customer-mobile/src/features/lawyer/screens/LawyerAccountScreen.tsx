import React, { useEffect, useRef, useState } from 'react';
import { View } from 'react-native';
import { useMutation } from '@tanstack/react-query';
import { useNavigation } from '@react-navigation/native';
import type { CompositeNavigationProp } from '@react-navigation/native';
import type { BottomTabNavigationProp } from '@react-navigation/bottom-tabs';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

import { ErrorState } from '@/components/feedback/ErrorState';
import { LoadingState } from '@/components/feedback/LoadingState';
import { Avatar } from '@/components/ui/Avatar';
import { Badge } from '@/components/ui/Badge';
import { BottomSheet } from '@/components/ui/BottomSheet';
import { Button } from '@/components/ui/Button';
import { Chip } from '@/components/ui/Chip';
import { ProfileMenuItem } from '@/components/ui/ProfileMenuItem';
import { Text } from '@/components/ui/Text';
import { TextField } from '@/components/ui/TextField';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import {
  useMyLawyerProfileQuery,
  useUpdateLawyerFeesMutation,
  useUpdateLawyerProfileMutation,
} from '@/features/lawyer/hooks/useLawyerQueries';
import type { LawyerHomeStackParamList, LawyerProfileStackParamList, LawyerTabParamList } from '@/navigation/types';
import { authApi } from '@/services/api/authApi';
import type { ThemeMode } from '@/theme';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';

type Section = 'practice' | 'experience' | 'fees' | 'documents' | 'settings' | null;
const modes: ThemeMode[] = ['system', 'light', 'dark'];

type Nav = CompositeNavigationProp<
  NativeStackNavigationProp<LawyerProfileStackParamList, 'LawyerAccount'>,
  CompositeNavigationProp<
    BottomTabNavigationProp<LawyerTabParamList>,
    NativeStackNavigationProp<LawyerHomeStackParamList>
  >
>;

export function LawyerAccountScreen() {
  const theme = useTheme();
  const navigation = useNavigation<Nav>();
  const showToast = useToastStore((s) => s.show);
  const mine = useMyLawyerProfileQuery();
  const updateFees = useUpdateLawyerFeesMutation();
  const updateProfile = useUpdateLawyerProfileMutation();
  const profile = mine.data;
  const [consultationFee, setConsultationFee] = useState('');
  const [emergencyFee, setEmergencyFee] = useState('');
  const [bio, setBio] = useState('');
  const [section, setSection] = useState<Section>(null);
  const hydrated = useRef(false);

  useEffect(() => {
    if (!profile || hydrated.current) return;
    hydrated.current = true;
    setConsultationFee(String(profile.consultationFee ?? ''));
    setEmergencyFee(profile.emergencyFee != null ? String(profile.emergencyFee) : '');
    setBio(profile.bio ?? '');
  }, [profile]);

  const logout = useMutation({
    mutationFn: () => authApi.logout(),
  });

  const name = `${profile?.firstName ?? ''} ${profile?.lastName ?? ''}`.trim() || 'Lawyer';
  const verificationTone =
    (profile?.verificationStatus ?? '').toLowerCase() === 'approved' || profile?.isVerified
      ? 'success'
      : (profile?.verificationStatus ?? '').toLowerCase() === 'rejected'
        ? 'danger'
        : 'warning';

  return (
    <Screen edges={['top', 'left', 'right', 'bottom']} scroll>
      <ScreenHeader title="Profile" />

      {mine.isLoading && !profile ? (
        <LoadingState label="Loading profile" />
      ) : mine.isError && !profile ? (
        <ErrorState message="Could not load profile." onRetry={() => mine.refetch()} />
      ) : (
        <View style={{ gap: theme.spacing.md }}>
          <View style={{ alignItems: 'center', gap: 6 }}>
            <Avatar name={name} uri={profile?.profileImageUrl} size="lg" />
            <Text variant="headline">{name}</Text>
            <Badge label={profile?.verificationStatus ?? 'Unknown'} tone={verificationTone} />
            <Text variant="caption" color="secondary">
              {profile?.barCouncilNumber || 'Bar council number not set'}
            </Text>
          </View>

          <View
            style={{
              borderWidth: 1,
              borderColor: theme.colors.border,
              borderRadius: theme.radius.lg,
              overflow: 'hidden',
            }}
          >
            <ProfileMenuItem icon="briefcase" label="Practice Areas" onPress={() => setSection('practice')} />
            <ProfileMenuItem icon="clock" label="Experience" onPress={() => setSection('experience')} />
            <ProfileMenuItem icon="credit-card" label="Fees" onPress={() => setSection('fees')} />
            <ProfileMenuItem
              icon="calendar"
              label="Availability"
              onPress={() =>
                navigation.navigate('LawyerHomeTab', {
                  screen: 'LawyerAvailabilityEdit',
                } as never)
              }
            />
            <ProfileMenuItem icon="document" label="Documents" onPress={() => setSection('documents')} />
            <ProfileMenuItem icon="settings" label="Settings" onPress={() => setSection('settings')} />
            <ProfileMenuItem icon="logout" label="Logout" danger onPress={() => logout.mutate()} />
          </View>
        </View>
      )}

      <BottomSheet visible={section === 'practice'} onClose={() => setSection(null)} title="Practice Areas">
        <Text variant="body" color="secondary">
          {profile?.practiceAreas.map((a) => a.name).join(', ') || 'No practice areas listed.'}
        </Text>
        <Text variant="caption" color="secondary" style={{ marginTop: theme.spacing.sm }}>
          Languages: {profile?.languages.map((l) => l.languageName).join(', ') || 'None listed'}
        </Text>
      </BottomSheet>

      <BottomSheet visible={section === 'experience'} onClose={() => setSection(null)} title="Experience">
        <Text variant="body" color="secondary">
          {profile?.yearsOfExperience ?? 0} years
        </Text>
        {profile?.educations[0] ? (
          <Text variant="caption" color="secondary" style={{ marginTop: theme.spacing.sm }}>
            {profile.educations[0].degree}, {profile.educations[0].institutionName}
          </Text>
        ) : null}
        <TextField label="Bio" value={bio} onChangeText={setBio} multiline containerStyle={{ marginTop: theme.spacing.md }} />
        <Button
          title="Save bio"
          variant="primary"
          size="sm"
          style={{ marginTop: theme.spacing.sm }}
          loading={updateProfile.isPending}
          onPress={() =>
            updateProfile.mutate(
              {
                bio,
                barCouncilNumber: profile?.barCouncilNumber,
                yearsOfExperience: profile?.yearsOfExperience ?? 0,
                isAcceptingAppointments: profile?.isAcceptingAppointments ?? true,
              },
              {
                onSuccess: () => showToast({ title: 'Profile saved', variant: 'success' }),
                onError: (error) =>
                  showToast({
                    title: 'Save failed',
                    message: getErrorMessage(error, 'Could not update profile'),
                    variant: 'error',
                  }),
              },
            )
          }
        />
      </BottomSheet>

      <BottomSheet visible={section === 'fees'} onClose={() => setSection(null)} title="Fees">
        <TextField label="Consultation fee" value={consultationFee} onChangeText={setConsultationFee} keyboardType="numeric" />
        <TextField
          label="Emergency fee"
          value={emergencyFee}
          onChangeText={setEmergencyFee}
          keyboardType="numeric"
          containerStyle={{ marginTop: theme.spacing.sm }}
        />
        <Button
          title="Save fees"
          variant="primary"
          size="sm"
          style={{ marginTop: theme.spacing.md }}
          loading={updateFees.isPending}
          onPress={() =>
            updateFees.mutate(
              {
                consultationFee: Number(consultationFee) || 0,
                emergencyFee: emergencyFee ? Number(emergencyFee) : null,
              },
              {
                onSuccess: () => showToast({ title: 'Fees saved', variant: 'success' }),
                onError: (error) =>
                  showToast({
                    title: 'Fees failed',
                    message: getErrorMessage(error, 'Could not update fees'),
                    variant: 'error',
                  }),
              },
            )
          }
        />
      </BottomSheet>

      <BottomSheet visible={section === 'documents'} onClose={() => setSection(null)} title="Documents">
        <Text variant="body" color="secondary">
          Verification documents were submitted with your application. They are not publicly downloadable.
        </Text>
      </BottomSheet>

      <BottomSheet visible={section === 'settings'} onClose={() => setSection(null)} title="Settings">
        <Text variant="label" color="secondary">
          APPEARANCE
        </Text>
        <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs, marginTop: theme.spacing.sm }}>
          {modes.map((mode) => (
            <Chip
              key={mode}
              label={mode[0].toUpperCase() + mode.slice(1)}
              selected={theme.mode === mode}
              onPress={() => theme.setMode(mode)}
            />
          ))}
        </View>
      </BottomSheet>
    </Screen>
  );
}
