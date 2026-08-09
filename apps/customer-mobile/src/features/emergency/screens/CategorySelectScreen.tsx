import React, { useState } from 'react';
import { FlatList, Pressable, TextInput, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { Button } from '@/components/ui/Button';
import { Text } from '@/components/ui/Text';
import { Icon } from '@/components/icons/Icon';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { Shimmer } from '@/components/feedback/Shimmer';
import { EMERGENCY_UI_CATEGORIES } from '@/constants/emergencyCategories';
import { useEmergencyCategoriesQuery } from '@/features/home/hooks/useHomeQueries';
import type { EmergencyStackParamList } from '@/navigation/types';
import { useEmergencyStore } from '@/store/emergencyStore';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<EmergencyStackParamList, 'CategorySelect'>;

export function CategorySelectScreen({ navigation }: Props) {
  const theme = useTheme();
  const apiCategories = useEmergencyCategoriesQuery(true);
  const setCategory = useEmergencyStore((s) => s.setCategory);
  const [selectedKey, setSelectedKey] = useState<string | null>(null);
  const [notes, setNotes] = useState('');

  const resolveBackendId = (fallbackId: string, title: string) => {
    const match = apiCategories.data?.find(
      (c) =>
        c.id === fallbackId ||
        c.name.toLowerCase().includes(title.toLowerCase().split(' ')[0] ?? '') ||
        title.toLowerCase().includes(c.name.toLowerCase().split(' ')[0] ?? ''),
    );
    return match?.id ?? apiCategories.data?.[0]?.id ?? fallbackId;
  };

  const continueFlow = () => {
    const ui = EMERGENCY_UI_CATEGORIES.find((c) => c.key === selectedKey);
    if (!ui) return;
    const categoryId = resolveBackendId(ui.backendCategoryId, ui.title);
    setCategory(categoryId, ui.title);
    navigation.navigate('LocationConfirm', {
      categoryId,
      categoryTitle: ui.title,
      notes: notes.trim() || `SOS: ${ui.title}`,
    });
  };

  return (
    <Screen
      edges={['top', 'left', 'right']}
      keyboard
      footer={
        <View style={{ paddingHorizontal: theme.layout.gutter }}>
          <Button
            title="Continue"
            variant="danger"
            disabled={!selectedKey}
            onPress={continueFlow}
            accessibilityHint="Continues to location confirmation"
          />
        </View>
      }
    >
      <View style={{ paddingHorizontal: theme.layout.gutter, flex: 1, gap: theme.spacing.sm }}>
        <ScreenHeader
          title="Emergency category"
          subtitle="Choose the closest match"
          onBack={() => navigation.goBack()}
        />

        {apiCategories.isLoading ? (
          <View style={{ gap: theme.spacing.sm }}>
            {Array.from({ length: 6 }).map((_, i) => (
              <Shimmer key={i} height={64} radius={theme.radius.lg} />
            ))}
          </View>
        ) : (
          <FlatList
            data={EMERGENCY_UI_CATEGORIES}
            keyExtractor={(item) => item.key}
            contentContainerStyle={{ gap: theme.spacing.xs, paddingBottom: theme.spacing.lg }}
            renderItem={({ item }) => {
              const selected = selectedKey === item.key;
              return (
                <Pressable
                  accessibilityRole="button"
                  accessibilityState={{ selected }}
                  accessibilityLabel={item.title}
                  onPress={() => setSelectedKey(item.key)}
                  style={{
                    minHeight: 64,
                    borderRadius: theme.radius.lg,
                    borderWidth: 1.5,
                    borderColor: selected ? theme.colors.danger : theme.colors.border,
                    backgroundColor: selected ? theme.colors.dangerMuted : theme.colors.surface,
                    padding: theme.spacing.md,
                    flexDirection: 'row',
                    gap: theme.spacing.sm,
                    alignItems: 'center',
                  }}
                >
                  <View
                    style={{
                      width: 44,
                      height: 44,
                      borderRadius: theme.radius.md,
                      backgroundColor: theme.colors.surfaceMuted,
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}
                  >
                    <Icon name={item.icon} size={22} color={theme.colors.danger} />
                  </View>
                  <View style={{ flex: 1 }}>
                    <Text variant="bodyMedium">{item.title}</Text>
                    <Text variant="caption" color="secondary">
                      {item.subtitle}
                    </Text>
                  </View>
                </Pressable>
              );
            }}
            ListFooterComponent={
              <View style={{ marginTop: theme.spacing.md, gap: theme.spacing.xs }}>
                <Text variant="label" color="secondary">
                  OPTIONAL DETAILS
                </Text>
                <TextInput
                  value={notes}
                  onChangeText={setNotes}
                  placeholder="Short note for the lawyer (optional)"
                  placeholderTextColor={theme.colors.textTertiary}
                  multiline
                  accessibilityLabel="Optional emergency notes"
                  style={{
                    minHeight: 88,
                    borderRadius: theme.radius.md,
                    borderWidth: 1,
                    borderColor: theme.colors.borderStrong,
                    padding: theme.spacing.md,
                    color: theme.colors.textPrimary,
                    fontFamily: theme.typography.fontFamily.body,
                    fontSize: theme.typography.size.md,
                    textAlignVertical: 'top',
                  }}
                />
              </View>
            }
          />
        )}
      </View>
    </Screen>
  );
}
