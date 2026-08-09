import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Alert, Pressable, Switch, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { EmptyState } from '@/components/feedback/EmptyState';
import { ErrorState } from '@/components/feedback/ErrorState';
import { LoadingState } from '@/components/feedback/LoadingState';
import { BottomSheet } from '@/components/ui/BottomSheet';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { TimeRangePickerSheet } from '@/features/lawyer/availability/TimeRangePickerSheet';
import {
  copyDaySchedule,
  createDefaultRange,
  emptyWeek,
  formatDisplayTime,
  hydrateWeek,
  serializeWeek,
  snapshotWeek,
  validateWeek,
  weekHasErrors,
  type DayDraft,
  type TimeRangeDraft,
  type WeekDayName,
} from '@/features/lawyer/availability/availabilityWeek';
import {
  useMyAvailabilityQuery,
  useSetMyAvailabilityMutation,
} from '@/features/lawyer/hooks/useLawyerQueries';
import type { LawyerHomeStackParamList } from '@/navigation/types';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';

type Props = NativeStackScreenProps<LawyerHomeStackParamList, 'LawyerAvailabilityEdit'>;

const TIME_ZONE = 'Asia/Kolkata';

export function LawyerAvailabilityEditScreen({ navigation }: Props) {
  const theme = useTheme();
  const query = useMyAvailabilityQuery();
  const saveMutation = useSetMyAvailabilityMutation();
  const [days, setDays] = useState<DayDraft[] | null>(null);
  const [baseline, setBaseline] = useState<string>('');
  const [editing, setEditing] = useState<{ day: WeekDayName; range: TimeRangeDraft } | null>(null);
  const [copyOpen, setCopyOpen] = useState(false);
  const [copyTargets, setCopyTargets] = useState<WeekDayName[]>([]);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [savedFlash, setSavedFlash] = useState(false);

  useEffect(() => {
    if (days || !query.data) return;
    const hydrated = validateWeek(hydrateWeek(query.data));
    setDays(hydrated);
    setBaseline(snapshotWeek(hydrated));
  }, [days, query.data]);

  useEffect(() => {
    if (query.isSuccess && query.data && days === null && query.data.length === 0) {
      const blank = emptyWeek();
      setDays(blank);
      setBaseline(snapshotWeek(blank));
    }
  }, [days, query.data, query.isSuccess]);

  const dirty = Boolean(days && snapshotWeek(days) !== baseline);
  const invalid = days ? weekHasErrors(days) : false;

  useEffect(() => {
    const unsubscribe = navigation.addListener('beforeRemove', (event) => {
      if (!dirty) return;
      event.preventDefault();
      Alert.alert('Unsaved changes', 'You have unsaved availability changes.', [
        { text: 'Stay', style: 'cancel' },
        {
          text: 'Discard',
          style: 'destructive',
          onPress: () => navigation.dispatch(event.data.action),
        },
      ]);
    });
    return unsubscribe;
  }, [dirty, navigation]);

  const updateDays = useCallback((updater: (current: DayDraft[]) => DayDraft[]) => {
    setDays((current) => (current ? validateWeek(updater(current)) : current));
    setSavedFlash(false);
    setSaveError(null);
  }, []);

  const monday = days?.find((day) => day.name === 'Monday');

  const save = async () => {
    if (!days || invalid) return;
    setSaveError(null);
    try {
      const saved = await saveMutation.mutateAsync(serializeWeek(days, TIME_ZONE));
      const hydrated = validateWeek(hydrateWeek(saved));
      setDays(hydrated);
      setBaseline(snapshotWeek(hydrated));
      setSavedFlash(true);
    } catch (error) {
      setSaveError(getErrorMessage(error, "Couldn't save availability"));
    }
  };

  const copyMondayTo = (targets: WeekDayName[]) => {
    if (!monday) return;
    updateDays((current) =>
      current.map((day) => (targets.includes(day.name) ? copyDaySchedule(monday, day) : day)),
    );
    setCopyOpen(false);
  };

  const footerLabel = useMemo(() => {
    if (saveMutation.isPending) return 'Saving...';
    if (savedFlash && !dirty) return 'Saved ✓';
    return 'Save Availability';
  }, [dirty, saveMutation.isPending, savedFlash]);

  if (query.isLoading && !days) {
    return (
      <Screen edges={['top', 'left', 'right']}>
        <ScreenHeader title="Availability" onBack={() => navigation.goBack()} />
        <LoadingState label="Loading availability" />
      </Screen>
    );
  }

  if (query.isError && !days) {
    return (
      <Screen edges={['top', 'left', 'right']}>
        <ScreenHeader title="Availability" onBack={() => navigation.goBack()} />
        <ErrorState message="Could not load availability." onRetry={() => query.refetch()} />
      </Screen>
    );
  }

  return (
    <Screen
      edges={['top', 'left', 'right']}
      scroll
      footer={
        <View style={{ paddingHorizontal: theme.layout.gutter, gap: theme.spacing.xs }}>
          {saveError ? (
            <Text variant="caption" color="danger">
              {saveError}
            </Text>
          ) : null}
          <Button
            title={saveError ? 'Retry' : footerLabel}
            variant="accent"
            loading={saveMutation.isPending}
            disabled={!dirty || invalid}
            onPress={() => void save()}
          />
        </View>
      }
    >
      <ScreenHeader
        title="Availability"
        subtitle="Set the hours when customers can book consultations."
        onBack={() => navigation.goBack()}
      />

      {!days || days.every((day) => !day.enabled) ? (
        <EmptyState
          title="No hours set"
          description="Turn a day ON and add hours. Copy Monday to finish a weekday schedule quickly."
        />
      ) : null}

      <View style={{ gap: theme.spacing.sm, marginTop: theme.spacing.sm }}>
        {days?.map((day) => (
          <Card key={day.name}>
            <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
              <Text variant="headline">{day.name}</Text>
              <View style={{ flexDirection: 'row', alignItems: 'center', gap: theme.spacing.sm }}>
                <Text variant="label" color={day.enabled ? 'accent' : 'secondary'}>
                  {day.enabled ? 'ON' : 'OFF'}
                </Text>
                <Switch
                  value={day.enabled}
                  onValueChange={(enabled) =>
                    updateDays((current) =>
                      current.map((item) =>
                        item.name === day.name
                          ? {
                              ...item,
                              enabled,
                              ranges: enabled && item.ranges.length === 0 ? [createDefaultRange()] : item.ranges,
                            }
                          : item,
                      ),
                    )
                  }
                />
              </View>
            </View>

            {day.enabled ? (
              <View style={{ gap: theme.spacing.sm, marginTop: theme.spacing.md }}>
                {day.ranges.map((range) => (
                  <View key={range.id} style={{ gap: 4 }}>
                    <View style={{ flexDirection: 'row', alignItems: 'center', gap: theme.spacing.sm }}>
                      <Pressable
                        accessibilityRole="button"
                        accessibilityLabel={`${formatDisplayTime(range.startMinutes)} to ${formatDisplayTime(range.endMinutes)}`}
                        onPress={() => setEditing({ day: day.name, range })}
                        style={{
                          flex: 1,
                          minHeight: 48,
                          borderRadius: theme.radius.md,
                          borderWidth: 1,
                          borderColor: range.error ? theme.colors.danger : theme.colors.border,
                          paddingHorizontal: theme.spacing.md,
                          justifyContent: 'center',
                        }}
                      >
                        <Text variant="bodyMedium">
                          {formatDisplayTime(range.startMinutes)}  ─────  {formatDisplayTime(range.endMinutes)}
                        </Text>
                      </Pressable>
                      <Pressable
                        accessibilityRole="button"
                        accessibilityLabel="Delete range"
                        onPress={() =>
                          updateDays((current) =>
                            current.map((item) =>
                              item.name === day.name
                                ? {
                                    ...item,
                                    ranges: item.ranges.filter((r) => r.id !== range.id),
                                    enabled: item.ranges.filter((r) => r.id !== range.id).length > 0,
                                  }
                                : item,
                            ),
                          )
                        }
                        style={{
                          minHeight: 44,
                          minWidth: 44,
                          alignItems: 'center',
                          justifyContent: 'center',
                        }}
                      >
                        <Text variant="caption" color="danger">
                          Delete
                        </Text>
                      </Pressable>
                    </View>
                    {range.error ? (
                      <Text variant="caption" color="danger">
                        {range.error}
                      </Text>
                    ) : null}
                  </View>
                ))}
                <Button
                  title="+ Add hours"
                  variant="ghost"
                  size="sm"
                  onPress={() =>
                    updateDays((current) =>
                      current.map((item) =>
                        item.name === day.name
                          ? {
                              ...item,
                              ranges: [
                                ...item.ranges,
                                createDefaultRange(14 * 60, 18 * 60),
                              ],
                            }
                          : item,
                      ),
                    )
                  }
                />
              </View>
            ) : null}
          </Card>
        ))}

        <Button
          title="Copy Monday to..."
          variant="secondary"
          disabled={!monday}
          onPress={() => {
            setCopyTargets([]);
            setCopyOpen(true);
          }}
        />
      </View>

      <TimeRangePickerSheet
        visible={Boolean(editing)}
        startMinutes={editing?.range.startMinutes ?? 9 * 60}
        endMinutes={editing?.range.endMinutes ?? 13 * 60}
        onClose={() => setEditing(null)}
        onSave={(startMinutes, endMinutes) => {
          if (!editing) return;
          updateDays((current) =>
            current.map((day) =>
              day.name === editing.day
                ? {
                    ...day,
                    ranges: day.ranges.map((range) =>
                      range.id === editing.range.id ? { ...range, startMinutes, endMinutes } : range,
                    ),
                  }
                : day,
            ),
          );
          setEditing(null);
        }}
      />

      <BottomSheet visible={copyOpen} onClose={() => setCopyOpen(false)} title="Copy Monday to..." snapHeight={520}>
        <View style={{ gap: theme.spacing.sm }}>
          {days
            ?.filter((day) => day.name !== 'Monday')
            .map((day) => {
              const selected = copyTargets.includes(day.name);
              return (
                <Pressable
                  key={day.name}
                  onPress={() =>
                    setCopyTargets((current) =>
                      selected ? current.filter((name) => name !== day.name) : [...current, day.name],
                    )
                  }
                  style={{
                    minHeight: 48,
                    borderRadius: theme.radius.md,
                    borderWidth: 1,
                    borderColor: selected ? theme.colors.accent : theme.colors.border,
                    paddingHorizontal: theme.spacing.md,
                    justifyContent: 'center',
                  }}
                >
                  <Text variant="bodyMedium">{day.name}</Text>
                </Pressable>
              );
            })}
          <Button
            title="Copy to selected"
            variant="accent"
            disabled={copyTargets.length === 0}
            onPress={() => copyMondayTo(copyTargets)}
          />
          <Button
            title="Copy to weekdays"
            variant="secondary"
            onPress={() => copyMondayTo(['Tuesday', 'Wednesday', 'Thursday', 'Friday'])}
          />
          <Button
            title="Copy to all days"
            variant="secondary"
            onPress={() =>
              copyMondayTo(['Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'])
            }
          />
        </View>
      </BottomSheet>
    </Screen>
  );
}
