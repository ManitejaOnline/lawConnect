import React from 'react';
import { View } from 'react-native';
import { useMutation } from '@tanstack/react-query';

import { hasRole, AppRoles, type AppSurface } from '@/auth/roles';
import { Button } from '@/components/ui/Button';
import { Icon } from '@/components/icons/Icon';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { authApi } from '@/services/api/authApi';
import { useAuthStore } from '@/store/authStore';
import { useTheme } from '@/theme/ThemeProvider';

type Props = {
  surface: Extract<AppSurface, 'wrong-app'>;
};

export function AccessBlockedScreen({ surface: _surface }: Props) {
  const theme = useTheme();
  const roles = useAuthStore((s) => s.profile?.roles ?? s.session?.roles ?? []);
  const setIntendedAudience = useAuthStore((s) => s.setIntendedAudience);

  const logout = useMutation({
    mutationFn: () => authApi.logout(),
  });

  const title = hasRole(roles, AppRoles.Admin)
    ? 'Admin accounts are not supported in this app'
    : 'Wrong app for this account';
  const body = hasRole(roles, AppRoles.Admin)
    ? 'Use the LawConnect admin portal in a browser. This Play Store app is for customers and lawyers only.'
    : hasRole(roles, AppRoles.Lawyer)
    ? 'This number is registered as a lawyer. Continue as Lawyer, or sign out and use a customer number.'
    : 'This number is a customer account. Continue as User, or sign out and use a lawyer number.';

  return (
    <Screen edges={['top', 'left', 'right', 'bottom']}>
      <View style={{ flex: 1, justifyContent: 'center', gap: theme.spacing.lg }}>
        <View
          style={{
            width: 72,
            height: 72,
            borderRadius: theme.radius.xl,
            backgroundColor: theme.colors.accentSoft,
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <Icon name="alert" size={32} color={theme.colors.accent} />
        </View>
        <View style={{ gap: theme.spacing.sm }}>
          <Text variant="title">{title}</Text>
          <Text variant="body" color="secondary">
            {body}
          </Text>
        </View>

        <View style={{ gap: theme.spacing.sm, marginTop: theme.spacing.md }}>
          {hasRole(roles, AppRoles.Customer) ? (
            <Button title="Continue as User" variant="accent" onPress={() => setIntendedAudience('customer')} />
          ) : null}
          {hasRole(roles, AppRoles.Lawyer) ? (
            <Button title="Continue as Lawyer" variant="accent" onPress={() => setIntendedAudience('lawyer')} />
          ) : null}
          <Button title="Sign out" variant="ghost" loading={logout.isPending} onPress={() => logout.mutate()} />
        </View>
      </View>
    </Screen>
  );
}
