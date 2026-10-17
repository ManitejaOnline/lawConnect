import React, { useState } from 'react';
import { Linking, View } from 'react-native';
import { useMutation } from '@tanstack/react-query';
import { useNavigation } from '@react-navigation/native';
import type { BottomTabNavigationProp } from '@react-navigation/bottom-tabs';
import type { CompositeNavigationProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

import { Avatar } from '@/components/ui/Avatar';
import { BottomSheet } from '@/components/ui/BottomSheet';
import { Button } from '@/components/ui/Button';
import { Chip } from '@/components/ui/Chip';
import { ProfileMenuItem } from '@/components/ui/ProfileMenuItem';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { useUserProfileQuery } from '@/features/home/hooks/useHomeQueries';
import type { MainTabParamList, ProfileStackParamList } from '@/navigation/types';
import { authApi } from '@/services/api/authApi';
import type { ThemeMode } from '@/theme';
import { useTheme } from '@/theme/ThemeProvider';
import { useAuthStore } from '@/store/authStore';
import { useToastStore } from '@/store/toastStore';
import { getErrorMessage } from '@/utils/errors';

const modes: ThemeMode[] = ['system', 'light', 'dark'];
const PRIVACY_POLICY_URL = 'https://lawconnect-uddw.onrender.com/privacy';

type Nav = CompositeNavigationProp<
  NativeStackNavigationProp<ProfileStackParamList, 'Profile'>,
  BottomTabNavigationProp<MainTabParamList>
>;

export function ProfileShellScreen() {
  const theme = useTheme();
  const navigation = useNavigation<Nav>();
  const session = useAuthStore((s) => s.session);
  const authProfile = useAuthStore((s) => s.profile);
  const userProfile = useUserProfileQuery();
  const showToast = useToastStore((s) => s.show);
  const [aboutOpen, setAboutOpen] = useState(false);
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [logoutOpen, setLogoutOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const profile = userProfile.data;
  const displayName =
    [profile?.firstName, profile?.lastName].filter(Boolean).join(' ').trim() ||
    [authProfile?.firstName, authProfile?.lastName].filter(Boolean).join(' ').trim() ||
    (session ? 'Signed in' : 'Guest');
  const phone = profile?.phoneNumber ?? session?.phoneNumber ?? authProfile?.phoneNumber;
  const email = profile?.email;

  const deleteAccount = useMutation({
    mutationFn: async () => authApi.deleteAccount(),
    onSuccess: () => {
      setDeleteOpen(false);
      showToast({ title: 'Account deleted', message: 'Your sign-in details have been removed.', variant: 'info' });
    },
    onError: (error) => {
      showToast({ title: 'Could not delete account', message: getErrorMessage(error), variant: 'error' });
    },
  });

  const logout = useMutation({
    mutationFn: async () => authApi.logout(),
    onSuccess: () => {
      setLogoutOpen(false);
      showToast({ title: 'Signed out', message: 'See you soon.', variant: 'info' });
    },
    onError: (error) => {
      setLogoutOpen(false);
      showToast({
        title: 'Signed out',
        message: getErrorMessage(error, 'Local session cleared.'),
        variant: 'warning',
      });
    },
  });

  return (
    <Screen scroll>
      <ScreenHeader title="Profile" />
      <View style={{ alignItems: 'center', gap: 6, marginBottom: theme.spacing.md }}>
        <Avatar name={displayName} uri={profile?.profileImageUrl} size="lg" />
        <Text variant="headline">{displayName}</Text>
        {phone ? (
          <Text variant="caption" color="secondary">
            {phone}
          </Text>
        ) : null}
        {email ? (
          <Text variant="caption" color="secondary">
            {email}
          </Text>
        ) : null}
      </View>

      <View
        style={{
          borderWidth: 1,
          borderColor: theme.colors.border,
          borderRadius: theme.radius.lg,
          overflow: 'hidden',
        }}
      >
        <ProfileMenuItem icon="edit" label="Edit Profile" onPress={() => navigation.navigate('EditProfile')} />
        <ProfileMenuItem
          icon="calendar"
          label="My Appointments"
          onPress={() => navigation.navigate('AppointmentsTab')}
        />
        <ProfileMenuItem
          icon="credit-card"
          label="Payment Methods"
          onPress={() =>
            showToast({
              title: 'Payments',
              message: 'Consultation fees are shown when you book. Card checkout is not available in this app yet.',
              variant: 'info',
            })
          }
        />
        <ProfileMenuItem
          icon="bell"
          label="Notifications"
          onPress={() => navigation.navigate('HomeTab', { screen: 'Notifications' } as never)}
        />
        <ProfileMenuItem icon="help" label="Help & Support" onPress={() => setAboutOpen(true)} />
        <ProfileMenuItem icon="settings" label="Settings" onPress={() => setSettingsOpen(true)} />
        <ProfileMenuItem
          icon="help"
          label="Privacy Policy"
          onPress={() => void Linking.openURL(PRIVACY_POLICY_URL)}
        />
        {session ? (
          <ProfileMenuItem icon="logout" label="Delete account" danger onPress={() => setDeleteOpen(true)} />
        ) : null}
        {session ? (
          <ProfileMenuItem icon="logout" label="Logout" danger onPress={() => setLogoutOpen(true)} />
        ) : null}
      </View>

      <BottomSheet visible={aboutOpen} onClose={() => setAboutOpen(false)} title="Help & Support">
        <Text variant="body" color="secondary">
          LawConnect connects you with verified lawyers for consultations and urgent support.
        </Text>
      </BottomSheet>

      <BottomSheet visible={settingsOpen} onClose={() => setSettingsOpen(false)} title="Settings">
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

      <BottomSheet visible={deleteOpen} onClose={() => setDeleteOpen(false)} title="Delete account?">
        <Text variant="body" color="secondary">
          This disables sign-in and removes your name, email, phone number, and password. Appointments and cases already shared with a lawyer stay on their record. Phone documents on this device are not uploaded.
        </Text>
        <View style={{ marginTop: theme.spacing.lg, gap: theme.spacing.sm }}>
          <Button
            title="Delete account"
            variant="danger"
            size="sm"
            loading={deleteAccount.isPending}
            onPress={() => deleteAccount.mutate()}
          />
          <Button title="Cancel" variant="ghost" size="sm" disabled={deleteAccount.isPending} onPress={() => setDeleteOpen(false)} />
        </View>
      </BottomSheet>

      <BottomSheet visible={logoutOpen} onClose={() => setLogoutOpen(false)} title="Log out?">
        <Text variant="body" color="secondary">
          You’ll need to sign in again to continue.
        </Text>
        <View style={{ marginTop: theme.spacing.lg, gap: theme.spacing.sm }}>
          <Button title="Log out" variant="danger" size="sm" loading={logout.isPending} onPress={() => logout.mutate()} />
          <Button title="Cancel" variant="ghost" size="sm" disabled={logout.isPending} onPress={() => setLogoutOpen(false)} />
        </View>
      </BottomSheet>
    </Screen>
  );
}
