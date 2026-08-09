import React from 'react';
import { ScrollView, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { Button } from '@/components/ui/Button';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { PRACTICE_AREA_CATEGORIES } from '@/constants/practiceAreas';
import {
  FilterSection,
  SelectableChip,
} from '@/features/discovery/components/FilterSection';
import type { SearchStackParamList } from '@/navigation/types';
import {
  DISTANCE_OPTIONS,
  EXPERIENCE_OPTIONS,
  FEE_PRESETS,
  LANGUAGE_OPTIONS,
  RATING_OPTIONS,
  SORT_OPTIONS,
  useDiscoveryFiltersStore,
} from '@/store/discoveryFiltersStore';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<SearchStackParamList, 'Filters'>;

export function FiltersScreen({ navigation }: Props) {
  const theme = useTheme();
  const draft = useDiscoveryFiltersStore((s) => s.draft);
  const setDraft = useDiscoveryFiltersStore((s) => s.setDraft);
  const applyDraft = useDiscoveryFiltersStore((s) => s.applyDraft);
  const resetDraft = useDiscoveryFiltersStore((s) => s.resetDraft);

  const apply = () => {
    applyDraft();
    navigation.goBack();
  };

  return (
    <Screen
      edges={['top', 'left', 'right']}
      padded={false}
      keyboard
      footer={
        <View style={{ flexDirection: 'row', gap: theme.spacing.xs, paddingHorizontal: theme.layout.gutter }}>
          <Button title="Reset" variant="secondary" size="sm" fullWidth={false} style={{ flex: 1 }} onPress={resetDraft} />
          <Button title="Apply" variant="primary" size="sm" fullWidth={false} style={{ flex: 1 }} onPress={apply} />
        </View>
      }
    >
      <View style={{ paddingHorizontal: theme.layout.gutter, flex: 1 }}>
        <ScreenHeader title="Filters" subtitle="Refine discovery" onBack={() => navigation.goBack()} />
        <ScrollView
          showsVerticalScrollIndicator={false}
          contentContainerStyle={{ paddingBottom: theme.spacing.xl }}
          keyboardShouldPersistTaps="handled"
        >
          <FilterSection title="PRACTICE AREA">
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs }}>
              {PRACTICE_AREA_CATEGORIES.map((area) => (
                <SelectableChip
                  key={area.id}
                  label={area.shortName}
                  selected={draft.specializationId === area.id}
                  onPress={() =>
                    setDraft({
                      specializationId: draft.specializationId === area.id ? undefined : area.id,
                    })
                  }
                />
              ))}
            </View>
          </FilterSection>

          <FilterSection title="EXPERIENCE">
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs }}>
              {EXPERIENCE_OPTIONS.map((option) => (
                <SelectableChip
                  key={option.value}
                  label={option.label}
                  selected={draft.minExperience === option.value}
                  onPress={() =>
                    setDraft({
                      minExperience: draft.minExperience === option.value ? undefined : option.value,
                    })
                  }
                />
              ))}
            </View>
          </FilterSection>

          <FilterSection title="RATING">
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs }}>
              {RATING_OPTIONS.map((option) => (
                <SelectableChip
                  key={option.value}
                  label={option.label}
                  selected={draft.minRating === option.value}
                  onPress={() =>
                    setDraft({
                      minRating: draft.minRating === option.value ? undefined : option.value,
                    })
                  }
                />
              ))}
            </View>
          </FilterSection>

          <FilterSection title="FEE">
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs }}>
              {FEE_PRESETS.map((preset) => {
                const selected = draft.minFee === preset.min && draft.maxFee === preset.max;
                return (
                  <SelectableChip
                    key={preset.label}
                    label={preset.label}
                    selected={selected}
                    onPress={() =>
                      setDraft(
                        selected
                          ? { minFee: undefined, maxFee: undefined }
                          : { minFee: preset.min, maxFee: preset.max },
                      )
                    }
                  />
                );
              })}
            </View>
          </FilterSection>

          <FilterSection title="DISTANCE">
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs }}>
              {DISTANCE_OPTIONS.map((option) => (
                <SelectableChip
                  key={option.value}
                  label={option.label}
                  selected={draft.maxDistanceKm === option.value}
                  onPress={() =>
                    setDraft({
                      maxDistanceKm: draft.maxDistanceKm === option.value ? undefined : option.value,
                      sortBy: draft.maxDistanceKm === option.value ? draft.sortBy : 'distance',
                      sortDescending: false,
                    })
                  }
                />
              ))}
            </View>
          </FilterSection>

          <FilterSection title="LANGUAGES">
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs }}>
              {LANGUAGE_OPTIONS.map((option) => (
                <SelectableChip
                  key={option.code}
                  label={option.label}
                  selected={draft.languageCode === option.code}
                  onPress={() =>
                    setDraft({
                      languageCode: draft.languageCode === option.code ? undefined : option.code,
                    })
                  }
                />
              ))}
            </View>
          </FilterSection>

          <FilterSection title="AVAILABILITY">
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs }}>
              <SelectableChip
                label="Online now"
                selected={Boolean(draft.isOnline)}
                onPress={() => setDraft({ isOnline: !draft.isOnline })}
              />
              <SelectableChip
                label="Emergency available"
                selected={Boolean(draft.isAvailableForEmergency)}
                onPress={() =>
                  setDraft({ isAvailableForEmergency: !draft.isAvailableForEmergency })
                }
              />
            </View>
          </FilterSection>

          <FilterSection title="SORT BY">
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs }}>
              {SORT_OPTIONS.map((option) => {
                const selected =
                  draft.sortBy === option.value && draft.sortDescending === option.descending;
                return (
                  <SelectableChip
                    key={`${option.value}-${option.label}`}
                    label={option.label}
                    selected={selected}
                    onPress={() =>
                      setDraft({ sortBy: option.value, sortDescending: option.descending })
                    }
                  />
                );
              })}
            </View>
          </FilterSection>
        </ScrollView>
      </View>
    </Screen>
  );
}
