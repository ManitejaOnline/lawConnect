import React from 'react';
import { View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { Button } from '@/components/ui/Button';
import { Text } from '@/components/ui/Text';
import { Icon } from '@/components/icons/Icon';
import { Screen } from '@/components/layout/Screen';
import type { EmergencyErrorKind } from '@/types/emergency';
import type { EmergencyStackParamList } from '@/navigation/types';
import { useEmergencyStore } from '@/store/emergencyStore';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<EmergencyStackParamList, 'EmergencyError'>;

const COPY: Record<EmergencyErrorKind, { title: string; action: string }> = {
  location: { title: 'Location unavailable', action: 'Try again' },
  no_lawyers: { title: 'No lawyers available', action: 'Retry SOS' },
  timeout: { title: 'Search timed out', action: 'Retry SOS' },
  network: { title: 'Network lost', action: 'Retry' },
  lawyer_cancelled: { title: 'Lawyer unavailable', action: 'Find another lawyer' },
  generic: { title: 'Something went wrong', action: 'Try again' },
};

export function EmergencyErrorScreen({ navigation, route }: Props) {
  const theme = useTheme();
  const resetSession = useEmergencyStore((s) => s.resetSession);
  const kind = route.params.kind;
  const copy = COPY[kind] ?? COPY.generic;

  return (
    <Screen
      edges={['top', 'left', 'right', 'bottom']}
      footer={
        <View style={{ paddingHorizontal: theme.layout.gutter, gap: theme.spacing.sm }}>
          <Button
            title={copy.action}
            variant="danger"
            onPress={() => {
              resetSession();
              if (kind === 'location') {
                const categoryId = useEmergencyStore.getState().selectedCategoryId;
                const categoryTitle = useEmergencyStore.getState().selectedCategoryTitle;
                if (categoryId && categoryTitle) {
                  navigation.replace('LocationConfirm', { categoryId, categoryTitle });
                } else {
                  navigation.replace('CategorySelect');
                }
              } else {
                navigation.replace('EmergencyHome');
              }
            }}
          />
          <Button title="Back to home" variant="ghost" onPress={() => navigation.getParent()?.goBack()} />
        </View>
      }
    >
      <View
        style={{
          flex: 1,
          paddingHorizontal: theme.layout.gutter,
          alignItems: 'center',
          justifyContent: 'center',
          gap: theme.spacing.md,
        }}
      >
        <View
          style={{
            width: 72,
            height: 72,
            borderRadius: 36,
            backgroundColor: theme.colors.dangerMuted,
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <Icon name="alert" size={32} color={theme.colors.danger} />
        </View>
        <Text variant="title" align="center">
          {copy.title}
        </Text>
        <Text variant="body" color="secondary" align="center">
          {route.params.message}
        </Text>
      </View>
    </Screen>
  );
}
