export type ApiProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
};

export type AuthTokenResponse = {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
};

export type AuthLoginResponse = {
  userId: string;
  phoneNumber: string;
  roles: string[];
  isNewUser: boolean;
  tokens: AuthTokenResponse;
};

export type RequestOtpResponse = {
  destination: string;
  expiresAtUtc: string;
  expiresInSeconds: number;
};

export type CurrentAuthUserResponse = {
  userId: string;
  phoneNumber: string;
  firstName: string;
  lastName: string;
  roles: string[];
};

export type RequestOtpPayload = {
  phoneNumber: string;
};

export type VerifyOtpPayload = {
  phoneNumber: string;
  code: string;
  deviceIdentifier: string;
  platform: number;
  fcmToken?: string | null;
  appVersion?: string | null;
  osVersion?: string | null;
};

export type RefreshTokenPayload = {
  refreshToken: string;
  deviceIdentifier: string;
  platform: number;
  fcmToken?: string | null;
  appVersion?: string | null;
  osVersion?: string | null;
};

export type VerificationStatus = 'Pending' | 'UnderReview' | 'Approved' | 'Rejected' | 'Suspended';

export type AdminLawyerApplicationSummary = {
  lawyerId: string;
  userId: string;
  fullName: string;
  email: string | null;
  phoneNumber: string;
  stateBarCouncil: string | null;
  enrollmentNumber: string | null;
  verificationStatus: VerificationStatus;
  applicationSubmittedAtUtc: string | null;
  createdAtUtc: string;
  cityName: string | null;
  stateName: string | null;
  countryName: string | null;
  yearsOfExperience: number;
  practiceAreas: string[];
  verifiedAtUtc: string | null;
  reviewedAtUtc: string | null;
  reviewReason: string | null;
};

export type AdminLawyerDocumentItem = {
  documentId: string;
  documentType: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  verificationStatus: string;
  downloadPath: string;
};

export type AdminLawyerApplicationDetail = {
  lawyerId: string;
  userId: string;
  firstName: string;
  lastName: string;
  email: string | null;
  phoneNumber: string;
  profileImageUrl: string | null;
  stateBarCouncil: string | null;
  enrollmentNumber: string | null;
  bio: string | null;
  yearsOfExperience: number;
  consultationFee: number;
  cityName: string | null;
  stateName: string | null;
  countryName: string | null;
  verificationStatus: VerificationStatus;
  isVerified: boolean;
  applicationSubmittedAtUtc: string | null;
  verifiedAtUtc: string | null;
  reviewReason: string | null;
  reviewedAtUtc: string | null;
  createdAtUtc: string;
  practiceAreas: string[];
  languages: string[];
  education: string[];
  documents: AdminLawyerDocumentItem[];
};

// --- Lawyer Rating + Trust Score ---

export type LawyerRatingSubmittedResponse = {
  ratingId: string;
  lawyerId: string;
  stars: number;
  reviewText: string | null;
  createdAt: string;
};

export type LawyerRatingSummaryResponse = {
  ratingAverage: number;
  ratingCount: number;
  bayesianScore: number;
};

export type TrustScoreBreakdown = {
  clientRating: number;
  appointmentReliability: number;
  responsiveness: number;
  caseUpdates: number;
  caseManagement: number;
  verifiedComplaints: number;
};

export type LawyerTrustScoreResponse = {
  score: number;
  level: string;
  rating: number;
  reviewCount: number;
  breakdown: TrustScoreBreakdown;
  caseManagementNote: string | null;
};

export type ComplaintStatus = 'Pending' | 'Verified' | 'Dismissed';

/**
 * Minor = 0, Serious = 1 on the wire — the API has no string-enum converter configured, so
 * request bodies (admin verify) must send the numeric value, not the string.
 */
export type ComplaintSeverity = 0 | 1;

export type LawyerComplaintSubmittedResponse = {
  complaintId: string;
  lawyerId: string;
  category: string;
  status: ComplaintStatus;
  createdAt: string;
};

export type AdminComplaintSummary = {
  complaintId: string;
  lawyerId: string;
  lawyerName: string;
  clientId: string;
  clientName: string;
  appointmentId: string | null;
  category: string;
  description: string;
  status: ComplaintStatus;
  severity: 'Minor' | 'Serious' | null;
  adminNotes: string | null;
  reviewedAtUtc: string | null;
  createdAtUtc: string;
};

export type TrustScoreHistoryEntry = {
  score: number;
  level: string;
  breakdown: TrustScoreBreakdown;
  caseManagementNote: string | null;
  computedAtUtc: string;
};

export type AdminLawyerTrustScoreResponse = {
  current: LawyerTrustScoreResponse;
  history: TrustScoreHistoryEntry[];
  complaints: AdminComplaintSummary[];
};
