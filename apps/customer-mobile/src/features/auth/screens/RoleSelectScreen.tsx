import React from 'react';
import { View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { BrandMark } from '@/components/brand/BrandMark';
import { Button } from '@/components/ui/Button';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import type { AppAudience } from '@/auth/roles';
import type { AuthStackParamList } from '@/navigation/types';
import { useAuthStore } from '@/store/authStore';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<AuthStackParamList, 'RoleSelect'>;

export function RoleSelectScreen({ navigation }: Props) {
  const theme = useTheme();
  const setIntendedAudience = useAuthStore((s) => s.setIntendedAudience);

  const choose = (audience: AppAudience) => {
    setIntendedAudience(audience);
    navigation.navigate(audience === 'lawyer' ? 'LawyerSignup' : 'CustomerSignup');
  };

  return (
    <Screen edges={['top', 'left', 'right', 'bottom']}>
      <View style={{ flex: 1, justifyContent: 'center', gap: theme.spacing.xl }}>
        <BrandMark size={72} />
        <View style={{ gap: theme.spacing.sm }}>
          <Button title="Continue as User" variant="primary" onPress={() => choose('customer')} />
          <Button title="Continue as Lawyer" variant="outline" onPress={() => choose('lawyer')} />
          <Button title="Sign in" variant="ghost" onPress={() => navigation.navigate('Login')} />
        </View>
        <Text variant="caption" color="secondary" align="center">
          Customer and lawyer apps are separate. Admin access is only on the web portal.
        </Text>
      </View>
    </Screen>
  );
}
