import type { LawyerWeeklyAvailability } from '@/types/home';

export const WEEK_DAYS = [
  { name: 'Monday', flag: 1 },
  { name: 'Tuesday', flag: 2 },
  { name: 'Wednesday', flag: 3 },
  { name: 'Thursday', flag: 4 },
  { name: 'Friday', flag: 5 },
  { name: 'Saturday', flag: 6 },
  { name: 'Sunday', flag: 0 },
] as const;

export type WeekDayName = (typeof WEEK_DAYS)[number]['name'];

export type TimeRangeDraft = {
  id: string;
  startMinutes: number;
  endMinutes: number;
  error?: string;
};

export type DayDraft = {
  name: WeekDayName;
  flag: number;
  enabled: boolean;
  ranges: TimeRangeDraft[];
};

const DEFAULT_START = 9 * 60;
const DEFAULT_END = 13 * 60;

export function createRangeId(): string {
  return `r-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 7)}`;
}

export function createDefaultRange(start = DEFAULT_START, end = DEFAULT_END): TimeRangeDraft {
  return { id: createRangeId(), startMinutes: start, endMinutes: end };
}

export function minutesToApiTime(minutes: number): string {
  const hours = Math.floor(minutes / 60);
  const mins = minutes % 60;
  return `${String(hours).padStart(2, '0')}:${String(mins).padStart(2, '0')}:00`;
}

export function formatDisplayTime(minutes: number): string {
  const hours24 = Math.floor(minutes / 60);
  const mins = minutes % 60;
  const period = hours24 >= 12 ? 'PM' : 'AM';
  const hours12 = hours24 % 12 === 0 ? 12 : hours24 % 12;
  return `${String(hours12).padStart(2, '0')}:${String(mins).padStart(2, '0')} ${period}`;
}

export function parseApiTime(value: string): number {
  const match = /^(\d{1,2}):(\d{2})/.exec(value);
  if (!match) return DEFAULT_START;
  return Number(match[1]) * 60 + Number(match[2]);
}

function parseDayName(raw: string | number): WeekDayName | null {
  if (typeof raw === 'number' || /^\d+$/.test(String(raw))) {
    return WEEK_DAYS.find((d) => d.flag === Number(raw))?.name ?? null;
  }
  const hit = WEEK_DAYS.find((d) => d.name.toLowerCase() === String(raw).toLowerCase());
  return hit?.name ?? null;
}

export function emptyWeek(): DayDraft[] {
  return WEEK_DAYS.map((day) => ({
    name: day.name,
    flag: day.flag,
    enabled: false,
    ranges: [],
  }));
}

export function hydrateWeek(slots: LawyerWeeklyAvailability[]): DayDraft[] {
  const week = emptyWeek();
  for (const slot of slots) {
    const name = parseDayName(slot.dayOfWeek);
    if (!name) continue;
    const day = week.find((d) => d.name === name);
    if (!day) continue;
    if (!slot.isAvailable) continue;
    day.enabled = true;
    day.ranges.push({
      id: slot.id || createRangeId(),
      startMinutes: parseApiTime(slot.startTime),
      endMinutes: parseApiTime(slot.endTime),
    });
  }
  for (const day of week) {
    day.ranges.sort((a, b) => a.startMinutes - b.startMinutes);
  }
  return week;
}

export function validateDay(day: DayDraft): DayDraft {
  if (!day.enabled) {
    return { ...day, ranges: day.ranges.map((range) => ({ ...range, error: undefined })) };
  }

  const next = day.ranges.map((range) => {
    if (range.endMinutes <= range.startMinutes) {
      return { ...range, error: 'End time must be after start time' };
    }
    return { ...range, error: undefined };
  });

  const ordered = [...next].sort((a, b) => a.startMinutes - b.startMinutes);
  for (let i = 1; i < ordered.length; i += 1) {
    if (ordered[i].startMinutes < ordered[i - 1].endMinutes) {
      const current = next.find((r) => r.id === ordered[i].id);
      const previous = next.find((r) => r.id === ordered[i - 1].id);
      if (current && !current.error) current.error = 'Overlaps another range';
      if (previous && !previous.error) previous.error = 'Overlaps another range';
    }
    if (
      ordered[i].startMinutes === ordered[i - 1].startMinutes &&
      ordered[i].endMinutes === ordered[i - 1].endMinutes
    ) {
      const current = next.find((r) => r.id === ordered[i].id);
      if (current) current.error = 'Duplicate range';
    }
  }

  return { ...day, ranges: next };
}

export function validateWeek(days: DayDraft[]): DayDraft[] {
  return days.map(validateDay);
}

export function weekHasErrors(days: DayDraft[]): boolean {
  return days.some((day) => day.enabled && day.ranges.some((range) => range.error));
}

export function serializeWeek(days: DayDraft[], timeZoneId: string) {
  return days.flatMap((day) => {
    if (!day.enabled) return [];
    return day.ranges.map((range) => ({
      dayOfWeek: day.flag,
      startTime: minutesToApiTime(range.startMinutes),
      endTime: minutesToApiTime(range.endMinutes),
      isAvailable: true,
      timeZoneId,
    }));
  });
}

export function snapshotWeek(days: DayDraft[]): string {
  return JSON.stringify(
    days.map((day) => ({
      name: day.name,
      enabled: day.enabled,
      ranges: day.ranges.map((range) => ({ start: range.startMinutes, end: range.endMinutes })),
    })),
  );
}

export function copyDaySchedule(source: DayDraft, target: DayDraft): DayDraft {
  if (!source.enabled) {
    return { ...target, enabled: false, ranges: [] };
  }
  return {
    ...target,
    enabled: true,
    ranges: source.ranges.map((range) => ({
      id: createRangeId(),
      startMinutes: range.startMinutes,
      endMinutes: range.endMinutes,
    })),
  };
}
