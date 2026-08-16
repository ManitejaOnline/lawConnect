export type AppointmentStatus = 'Scheduled' | 'Cancelled' | 'Completed' | 'NoShow' | string;

export type Appointment = {
  id: string;
  customerId: string;
  customerUserId: string;
  customerFirstName: string;
  customerLastName: string;
  lawyerId: string;
  lawyerUserId: string;
  lawyerFirstName: string;
  lawyerLastName: string;
  startUtc: string;
  endUtc: string;
  status: AppointmentStatus;
  notes?: string | null;
  cancellationReason?: string | null;
  cancelledAtUtc?: string | null;
  consultationFeeSnapshot: number;
  timeZoneId: string;
  /** Only populated by the appointment-detail fetch; null on list endpoints. */
  hasRating?: boolean | null;
};

export type AppointmentsPage = {
  items: Appointment[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

export function appointmentPartyName(item: Appointment, perspective: 'lawyer' | 'customer'): string {
  if (perspective === 'lawyer') {
    return `${item.customerFirstName} ${item.customerLastName}`.trim() || 'Customer';
  }
  return `${item.lawyerFirstName} ${item.lawyerLastName}`.trim() || 'Lawyer';
}

export function formatAppointmentWhen(iso: string, timeZoneId?: string): string {
  const date = new Date(iso);
  try {
    return date.toLocaleString(undefined, {
      weekday: 'short',
      day: 'numeric',
      month: 'short',
      hour: 'numeric',
      minute: '2-digit',
      timeZone: timeZoneId || undefined,
    });
  } catch {
    return date.toLocaleString(undefined, {
      weekday: 'short',
      day: 'numeric',
      month: 'short',
      hour: 'numeric',
      minute: '2-digit',
    });
  }
}

export function isUpcomingScheduled(item: Appointment, now = Date.now()): boolean {
  return String(item.status).toLowerCase() === 'scheduled' && new Date(item.startUtc).getTime() >= now;
}

export function isPastOrCompleted(item: Appointment, now = Date.now()): boolean {
  const status = String(item.status).toLowerCase();
  if (status === 'completed') return true;
  if (status === 'cancelled') return false;
  return new Date(item.endUtc).getTime() < now;
}

export function isCancelled(item: Appointment): boolean {
  return String(item.status).toLowerCase() === 'cancelled';
}
