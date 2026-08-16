export type CaseStatus = 'Pending' | 'Active' | 'OnHold' | 'Won' | 'Lost' | 'Settled' | 'Closed' | 'Cancelled';
export type CasePriority = 'Low' | 'Medium' | 'High' | 'Urgent';
export type CaseUpdateType =
  | 'General'
  | 'ClientCommunication'
  | 'DocumentFiled'
  | 'DocumentReceived'
  | 'HearingUpdate'
  | 'CourtUpdate'
  | 'Deadline'
  | 'StatusChange'
  | 'Other';
export type HearingStatus = 'Scheduled' | 'Completed' | 'Adjourned' | 'Cancelled';
export type MilestoneStatus = 'Pending' | 'InProgress' | 'Completed' | 'Overdue' | 'Cancelled';
export type CaseStallingStatus = 'None' | 'Warning' | 'Stalling' | 'SevereStalling';

export type CaseItem = {
  id: string;
  customerId: string;
  customerName: string;
  lawyerId: string;
  lawyerName: string;
  specializationId: string;
  caseType: string;
  title: string;
  caseNumber: string | null;
  description: string | null;
  courtName: string | null;
  status: CaseStatus;
  priority: CasePriority;
  startDate: string;
  closedDate: string | null;
  sourceAppointmentId: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type UpcomingAction = {
  kind: 'Hearing' | 'Milestone';
  when: string;
  title: string;
  location: string | null;
  status: string;
};

export type CaseActivitySignals = {
  daysSinceLastUpdate: number | null;
  daysSinceLastActivity: number | null;
  overdueMilestoneCount: number;
  missedHearingCount: number;
  pendingActionsCount: number;
  lastMeaningfulLawyerActivityAtUtc: string | null;
  daysSinceLastMeaningfulLawyerActivity: number | null;
  stallingStatus: CaseStallingStatus;
};

export type CaseRating = {
  stars: number;
  reviewText: string | null;
  createdAt: string;
};

export type CaseListItem = {
  id: string;
  title: string;
  caseNumber: string | null;
  otherPartyName: string;
  caseType: string;
  status: CaseStatus;
  priority: CasePriority;
  startDate: string;
  upcomingAction: UpcomingAction | null;
  lastActivityAtUtc: string | null;
  daysSinceLastUpdate: number | null;
};

export type CaseDetail = {
  case: CaseItem;
  upcomingAction: UpcomingAction | null;
  activitySignals: CaseActivitySignals;
  rating: CaseRating | null;
};

export type CaseUpdateItem = {
  id: string;
  caseId: string;
  createdByUserId: string;
  updateType: CaseUpdateType;
  title: string;
  description: string;
  createdAt: string;
};

export type Hearing = {
  id: string;
  caseId: string;
  hearingDate: string;
  courtName: string | null;
  hearingType: string | null;
  purpose: string | null;
  status: HearingStatus;
  outcome: string | null;
  notes: string | null;
  nextAction: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type CaseMilestone = {
  id: string;
  caseId: string;
  title: string;
  description: string | null;
  status: MilestoneStatus;
  dueDate: string | null;
  completedDate: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type CaseDocument = {
  id: string;
  caseId: string;
  caseUpdateId: string | null;
  hearingId: string | null;
  milestoneId: string | null;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedByUserId: string;
  createdAt: string;
};

export type CaseTimelineEntry = {
  kind: 'CaseCreated' | 'Update' | 'Hearing' | 'Milestone';
  entityId: string;
  occurredAtUtc: string;
  title: string;
  description: string | null;
  status: string | null;
};

// --- Admin ---

export type AdminCaseListItem = {
  id: string;
  title: string;
  caseNumber: string | null;
  lawyerName: string;
  customerName: string;
  caseType: string;
  status: CaseStatus;
  priority: CasePriority;
  startDate: string;
  upcomingAction: UpcomingAction | null;
  daysSinceLastUpdate: number | null;
  overdueMilestoneCount: number;
  daysSinceLastMeaningfulLawyerActivity: number | null;
  stallingStatus: CaseStallingStatus;
};

export type AdminCaseDetail = {
  case: CaseItem;
  upcomingAction: UpcomingAction | null;
  activitySignals: CaseActivitySignals;
  timeline: CaseTimelineEntry[];
  hearings: Hearing[];
  milestones: CaseMilestone[];
  updates: CaseUpdateItem[];
};

export type LawyerCaseActivitySummary = {
  lawyerId: string;
  totalCases: number;
  activeCases: number;
  updatesPerWeek: number;
  milestoneCompletionRate: number;
  hearingAttendanceRate: number;
  overdueMilestoneCount: number;
  missedHearingCount: number;
  stallingCaseCount: number;
  severelyStallingCaseCount: number;
  trustScoreDeduction: number;
};
