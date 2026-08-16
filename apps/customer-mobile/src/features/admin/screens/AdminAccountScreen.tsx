import React from 'react';
import { View } from 'react-native';
import { useMutation } from '@tanstack/react-query';

import { Icon } from '@/components/icons/Icon';
import { ProfileMenuItem } from '@/components/ui/ProfileMenuItem';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { authApi } from '@/services/api/authApi';
import { useAuthStore } from '@/store/authStore';
import { useTheme } from '@/theme/ThemeProvider';

export function AdminAccountScreen() {
  const theme = useTheme();
  const profile = useAuthStore((s) => s.profile);

  const logout = useMutation({
    mutationFn: () => authApi.logout(),
  });

  return (
    <Screen edges={['top', 'left', 'right', 'bottom']}>
      <ScreenHeader title="Account" />
      <View style={{ alignItems: 'center', gap: theme.spacing.xs, marginBottom: theme.spacing.lg }}>
        <View
          style={{
            width: 64,
            height: 64,
            borderRadius: theme.radius.full,
            backgroundColor: theme.colors.accentSoft,
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <Icon name="shield" size={28} color={theme.colors.accent} />
        </View>
        <Text variant="headline">Admin</Text>
        <Text variant="caption" color="secondary">
          {profile?.phoneNumber ?? 'LawConnect administrator'}
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
        <ProfileMenuItem
          icon="logout"
          label={logout.isPending ? 'Signing out…' : 'Logout'}
          danger
          onPress={() => logout.mutate()}
        />
      </View>
    </Screen>
  );
}
