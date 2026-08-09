import type { IconName } from '@/components/icons/Icon';

export type PagedResult<T> = {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

export type LawyerSearchItem = {
  lawyerId: string;
  userId: string;
  firstName: string;
  lastName: string;
  profileImageUrl?: string | null;
  bio?: string | null;
  yearsOfExperience: number;
  consultationFee: number;
  emergencyFee?: number | null;
  isVerified: boolean;
  isOnline: boolean;
  isAcceptingAppointments: boolean;
  isAvailableForEmergency: boolean;
  ratingAverage: number;
  ratingCount: number;
  verificationStatus: string;
  practiceAreas: string[];
  languages: string[];
  latitude?: number | null;
  longitude?: number | null;
  distanceKm?: number | null;
};

export type LawyerSearchParams = {
  search?: string;
  specializationId?: string;
  languageCode?: string;
  minRating?: number;
  minFee?: number;
  maxFee?: number;
  minExperience?: number;
  isOnline?: boolean;
  isAvailableForEmergency?: boolean;
  isAcceptingAppointments?: boolean;
  originLatitude?: number;
  originLongitude?: number;
  maxDistanceKm?: number;
  sortBy?: 'rating' | 'fee' | 'experience' | 'distance' | 'relevance';
  sortDescending?: boolean;
  pageNumber?: number;
  pageSize?: number;
};

export type PracticeAreaCategory = {
  id: string;
  name: string;
  shortName: string;
  icon: IconName;
};

export type PracticeAreaDetail = {
  specializationId: string;
  name: string;
  yearsOfExperience: number;
  isPrimary: boolean;
};

export type LawyerEducation = {
  id: string;
  institutionName: string;
  degree: string;
  fieldOfStudy?: string | null;
  startYear?: number | null;
  endYear?: number | null;
  description?: string | null;
};

export type LawyerExperience = {
  id: string;
  organizationName: string;
  title: string;
  startDate?: string | null;
  endDate?: string | null;
  isCurrent: boolean;
  description?: string | null;
};

export type LawyerLanguage = {
  id: string;
  languageCode: string;
  languageName: string;
  proficiency?: string | null;
};

export type LawyerWeeklyAvailability = {
  id: string;
  dayOfWeek: string | number;
  startTime: string;
  endTime: string;
  isAvailable: boolean;
  timeZoneId: string;
};

/** Authorized marketplace call payload. Public search/profile omit this. */
export type LawyerContact = {
  phoneNumber: string;
};

export type LawyerProfile = {
  lawyerId: string;
  userId: string;
  firstName: string;
  lastName: string;
  profileImageUrl?: string | null;
  barCouncilNumber?: string | null;
  bio?: string | null;
  yearsOfExperience: number;
  consultationFee: number;
  emergencyFee?: number | null;
  isVerified: boolean;
  isAvailableForEmergency: boolean;
  isAcceptingAppointments: boolean;
  isOnline: boolean;
  lastOnlineAtUtc?: string | null;
  verificationStatus: string;
  applicationSubmittedAtUtc?: string | null;
  stateBarCouncil?: string | null;
  cityName?: string | null;
  stateName?: string | null;
  countryName?: string | null;
  reviewReason?: string | null;
  reviewedAtUtc?: string | null;
  ratingAverage: number;
  ratingCount: number;
  practiceAreas: PracticeAreaDetail[];
  educations: LawyerEducation[];
  experiences: LawyerExperience[];
  languages: LawyerLanguage[];
  availabilities: LawyerWeeklyAvailability[];
};

export type AvailableSlot = {
  startUtc: string;
  endUtc: string;
  timeZoneId: string;
};

export type DiscoveryFilters = {
  specializationId?: string;
  languageCode?: string;
  minRating?: number;
  minFee?: number;
  maxFee?: number;
  minExperience?: number;
  maxDistanceKm?: number;
  isOnline?: boolean;
  isAvailableForEmergency?: boolean;
  sortBy: NonNullable<LawyerSearchParams['sortBy']>;
  sortDescending: boolean;
};

export type NotificationItem = {
  id: string;
  title: string;
  body: string;
  type: string;
  channel: string;
  isRead: boolean;
  createdAt: string;
  dataJson?: string | null;
};

export type EmergencyCategory = {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  specializationId: string;
  specializationName: string;
  displayOrder: number;
};

export type UserProfile = {
  userId: string;
  phoneNumber: string;
  email?: string | null;
  firstName: string;
  lastName: string;
  profileImageUrl?: string | null;
  isPhoneVerified: boolean;
  roles: string[];
};

export type AppointmentBooking = {
  id: string;
  customerId?: string;
  customerUserId?: string;
  customerFirstName?: string;
  customerLastName?: string;
  lawyerId: string;
  lawyerUserId?: string;
  lawyerFirstName?: string;
  lawyerLastName?: string;
  startUtc: string;
  endUtc: string;
  status: string;
  notes?: string | null;
  cancellationReason?: string | null;
  cancelledAtUtc?: string | null;
  consultationFeeSnapshot: number;
  timeZoneId: string;
};
