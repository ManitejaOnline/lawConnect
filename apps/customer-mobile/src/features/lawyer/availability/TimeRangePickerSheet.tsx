import React, { useEffect, useMemo, useState } from 'react';
import { Pressable, ScrollView, View } from 'react-native';

import { BottomSheet } from '@/components/ui/BottomSheet';
import { Button } from '@/components/ui/Button';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

type Props = {
  visible: boolean;
  title?: string;
  startMinutes: number;
  endMinutes: number;
  onClose: () => void;
  onSave: (startMinutes: number, endMinutes: number) => void;
};

const HOURS = Array.from({ length: 12 }, (_, i) => i + 1);
const MINUTES = [0, 15, 30, 45];

function toParts(total: number) {
  const hours24 = Math.floor(total / 60) % 24;
  const minutes = total % 60;
  const period = hours24 >= 12 ? 'PM' : 'AM';
  const hour12 = hours24 % 12 === 0 ? 12 : hours24 % 12;
  const snapped = MINUTES.includes(minutes) ? minutes : MINUTES.reduce((best, n) =>
    Math.abs(n - minutes) < Math.abs(best - minutes) ? n : best,
  0);
  return { hour12, minutes: snapped, period: period as 'AM' | 'PM' };
}

function toMinutes(hour12: number, minutes: number, period: 'AM' | 'PM'): number {
  let hours24 = hour12 % 12;
  if (period === 'PM') hours24 += 12;
  return hours24 * 60 + minutes;
}

function WheelColumn<T extends string | number>({
  values,
  selected,
  onSelect,
  format,
}: {
  values: T[];
  selected: T;
  onSelect: (value: T) => void;
  format?: (value: T) => string;
}) {
  const theme = useTheme();
  return (
    <ScrollView
      style={{ maxHeight: 180, flex: 1 }}
      contentContainerStyle={{ gap: 4, paddingVertical: 8 }}
      showsVerticalScrollIndicator={false}
    >
      {values.map((value) => {
        const active = value === selected;
        return (
          <Pressable
            key={String(value)}
            onPress={() => onSelect(value)}
            accessibilityRole="button"
            accessibilityState={{ selected: active }}
            style={{
              minHeight: 44,
              borderRadius: theme.radius.md,
              alignItems: 'center',
              justifyContent: 'center',
              backgroundColor: active ? theme.colors.accentSoft : 'transparent',
            }}
          >
            <Text variant="bodyMedium" color={active ? 'accent' : 'secondary'}>
              {format ? format(value) : String(value).padStart(2, '0')}
            </Text>
          </Pressable>
        );
      })}
    </ScrollView>
  );
}

function TimeField({
  label,
  minutes,
  onChange,
}: {
  label: string;
  minutes: number;
  onChange: (value: number) => void;
}) {
  const theme = useTheme();
  const parts = useMemo(() => toParts(minutes), [minutes]);

  return (
    <View style={{ gap: theme.spacing.xs, flex: 1 }}>
      <Text variant="label" color="secondary">
        {label}
      </Text>
      <View style={{ flexDirection: 'row', gap: theme.spacing.xs }}>
        <WheelColumn
          values={HOURS}
          selected={parts.hour12}
          onSelect={(hour12) => onChange(toMinutes(hour12, parts.minutes, parts.period))}
        />
        <WheelColumn
          values={MINUTES}
          selected={parts.minutes}
          format={(value) => String(value).padStart(2, '0')}
          onSelect={(mins) => onChange(toMinutes(parts.hour12, mins, parts.period))}
        />
        <WheelColumn
          values={['AM', 'PM'] as const}
          selected={parts.period}
          onSelect={(period) => onChange(toMinutes(parts.hour12, parts.minutes, period))}
        />
      </View>
    </View>
  );
}

export function TimeRangePickerSheet({
  visible,
  title = 'Edit hours',
  startMinutes,
  endMinutes,
  onClose,
  onSave,
}: Props) {
  const theme = useTheme();
  const [start, setStart] = useState(startMinutes);
  const [end, setEnd] = useState(endMinutes);

  useEffect(() => {
    if (visible) {
      setStart(startMinutes);
      setEnd(endMinutes);
    }
  }, [visible, startMinutes, endMinutes]);

  const invalid = end <= start;

  return (
    <BottomSheet visible={visible} onClose={onClose} title={title} snapHeight={560}>
      <View style={{ gap: theme.spacing.md, flex: 1 }}>
        <View style={{ flexDirection: 'row', gap: theme.spacing.md }}>
          <TimeField label="Start time" minutes={start} onChange={setStart} />
          <TimeField label="End time" minutes={end} onChange={setEnd} />
        </View>
        {invalid ? (
          <Text variant="caption" color="danger">
            End time must be after start time
          </Text>
        ) : null}
        <View style={{ flexDirection: 'row', gap: theme.spacing.sm, marginTop: 'auto' }}>
          <View style={{ flex: 1 }}>
            <Button title="Cancel" variant="secondary" onPress={onClose} />
          </View>
          <View style={{ flex: 1 }}>
            <Button title="Save" variant="accent" disabled={invalid} onPress={() => onSave(start, end)} />
          </View>
        </View>
      </View>
    </BottomSheet>
  );
}
