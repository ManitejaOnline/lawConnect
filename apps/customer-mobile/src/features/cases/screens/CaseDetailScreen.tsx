import React, { useState } from 'react';
import { View } from 'react-native';
import * as DocumentPicker from 'expo-document-picker';
import { useQuery } from '@tanstack/react-query';

import { ErrorState } from '@/components/feedback/ErrorState';
import { LoadingState } from '@/components/feedback/LoadingState';
import { Badge, type BadgeProps } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Chip } from '@/components/ui/Chip';
import { AppModal } from '@/components/ui/Modal';
import { Rating } from '@/components/ui/Rating';
import { Text } from '@/components/ui/Text';
import { TextField } from '@/components/ui/TextField';
import { Icon } from '@/components/icons/Icon';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { CaseTimeline } from '@/components/case/CaseTimeline';
import {
  useCaseDetailQuery,
  useCaseDocumentsQuery,
  useCaseHearingsQuery,
  useCaseMilestonesQuery,
  useCaseTimelineQuery,
  useChangeCaseStatusMutation,
  useCompleteMilestoneMutation,
  useCreateCaseUpdateMutation,
  useCreateHearingMutation,
  useCreateMilestoneMutation,
  useDeleteCaseDocumentMutation,
  useRecordHearingOutcomeMutation,
  useSubmitCaseRatingMutation,
  useUploadCaseDocumentMutation,
} from '@/features/cases/hooks/useCaseQueries';
import { ratingsApi } from '@/services/api/ratingsApi';
import type { CaseStatus, CaseUpdateType, Hearing, HearingStatus } from '@/types/case';
import { useAuthStore } from '@/store/authStore';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';

type Section = 'overview' | 'timeline' | 'hearings' | 'milestones' | 'documents';

const CASE_STATUSES: CaseStatus[] = ['Pending', 'Active', 'OnHold', 'Won', 'Lost', 'Settled', 'Closed', 'Cancelled'];
const UPDATE_TYPES: CaseUpdateType[] = [
  'General', 'ClientCommunication', 'DocumentFiled', 'DocumentReceived', 'HearingUpdate', 'CourtUpdate', 'Deadline', 'Other',
];
const RATABLE_STATUSES: CaseStatus[] = ['Won', 'Lost', 'Settled', 'Closed'];
const DOCUMENT_PICKER_TYPES = [
  'application/pdf',
  'application/msword',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'image/jpeg',
  'image/png',
];

function statusTone(status: string): NonNullable<BadgeProps['tone']> {
  switch (status) {
    case 'Won':
    case 'Settled':
    case 'Closed':
    case 'Completed':
      return 'success';
    case 'Lost':
    case 'Cancelled':
    case 'Overdue':
      return 'danger';
    case 'OnHold':
    case 'Adjourned':
      return 'warning';
    case 'Active':
    case 'Scheduled':
      return 'accent';
    default:
      return 'neutral';
  }
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  const theme = useTheme();
  return (
    <View
      style={{
        borderWidth: 1, borderColor: theme.colors.border, borderRadius: theme.radius.lg,
        padding: theme.spacing.md, gap: theme.spacing.sm, backgroundColor: theme.colors.surface,
      }}
    >
      <Text variant="bodyMedium">{title}</Text>
      {children}
    </View>
  );
}

export function CaseDetailScreen({ caseId, perspective, onBack }: { caseId: string; perspective: 'lawyer' | 'customer'; onBack?: () => void }) {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const isLawyer = perspective === 'lawyer';
  const userId = useAuthStore((s) => s.session?.userId);

  const [section, setSection] = useState<Section>('overview');
  const [showStatusModal, setShowStatusModal] = useState(false);
  const [showUpdateModal, setShowUpdateModal] = useState(false);
  const [showHearingModal, setShowHearingModal] = useState(false);
  const [showMilestoneModal, setShowMilestoneModal] = useState(false);
  const [showRatingModal, setShowRatingModal] = useState(false);
  const [outcomeHearing, setOutcomeHearing] = useState<Hearing | null>(null);
  const [uploadProgressPct, setUploadProgressPct] = useState<number | null>(null);

  const detail = useCaseDetailQuery(caseId);
  const timeline = useCaseTimelineQuery(caseId);
  const hearings = useCaseHearingsQuery(caseId);
  const milestones = useCaseMilestonesQuery(caseId);
  const documents = useCaseDocumentsQuery(caseId);

  const changeStatus = useChangeCaseStatusMutation(caseId);
  const createUpdate = useCreateCaseUpdateMutation(caseId);
  const createHearing = useCreateHearingMutation(caseId);
  const recordOutcome = useRecordHearingOutcomeMutation(caseId);
  const createMilestone = useCreateMilestoneMutation(caseId);
  const completeMilestone = useCompleteMilestoneMutation(caseId);
  const uploadDocument = useUploadCaseDocumentMutation(caseId);
  const deleteDocument = useDeleteCaseDocumentMutation(caseId);
  const submitRating = useSubmitCaseRatingMutation(caseId);

  const [statusChoice, setStatusChoice] = useState<CaseStatus>('Active');
  const [statusNote, setStatusNote] = useState('');
  const [updateType, setUpdateType] = useState<CaseUpdateType>('General');
  const [updateTitle, setUpdateTitle] = useState('');
  const [updateDescription, setUpdateDescription] = useState('');
  const [hearingDate, setHearingDate] = useState('');
  const [hearingCourt, setHearingCourt] = useState('');
  const [hearingType, setHearingType] = useState('');
  const [hearingPurpose, setHearingPurpose] = useState('');
  const [outcomeStatus, setOutcomeStatus] = useState<HearingStatus>('Completed');
  const [outcomeText, setOutcomeText] = useState('');
  const [outcomeNotes, setOutcomeNotes] = useState('');
  const [outcomeNextAction, setOutcomeNextAction] = useState('');
  const [milestoneTitle, setMilestoneTitle] = useState('');
  const [milestoneDescription, setMilestoneDescription] = useState('');
  const [milestoneDueDate, setMilestoneDueDate] = useState('');
  const [ratingStars, setRatingStars] = useState(5);
  const [ratingReview, setRatingReview] = useState('');

  const lawyerId = detail.data?.case.lawyerId;
  const lawyerRatingSummary = useQuery({
    queryKey: ['lawyer-rating-summary', lawyerId],
    queryFn: () => ratingsApi.getRating(lawyerId!),
    enabled: Boolean(lawyerId),
  });

  if (detail.isLoading) {
    return <LoadingState fullScreen label="Loading case" />;
  }
  if (detail.isError || !detail.data) {
    return <ErrorState message="Could not load this case." onRetry={() => detail.refetch()} />;
  }

  const c = detail.data.case;
  const upcoming = detail.data.upcomingAction;
  const signals = detail.data.activitySignals;
  const rating = detail.data.rating;
  const isRatable = RATABLE_STATUSES.includes(c.status);

  const uploadForCase = async () => {
    const picked = await DocumentPicker.getDocumentAsync({ type: DOCUMENT_PICKER_TYPES, copyToCacheDirectory: true });
    if (picked.canceled || !picked.assets?.[0]) return;
    const file = picked.assets[0];
    setUploadProgressPct(0);
    uploadDocument.mutate(
      {
        file: { uri: file.uri, name: file.name || 'document.pdf', mimeType: file.mimeType || 'application/pdf', file: file.file },
        onUploadProgress: (event) => {
          if (event.total) setUploadProgressPct(Math.round((event.loaded / event.total) * 100));
        },
      },
      {
        onSuccess: () => { setUploadProgressPct(null); showToast({ title: 'Document uploaded', variant: 'success' }); },
        onError: (error) => { setUploadProgressPct(null); showToast({ title: 'Upload failed', message: getErrorMessage(error, 'Check the file type and size'), variant: 'error' }); },
      },
    );
  };

  const removeDocument = (documentId: string) => {
    deleteDocument.mutate(documentId, {
      onSuccess: () => showToast({ title: 'Document deleted', variant: 'success' }),
      onError: (error) => showToast({ title: 'Could not delete document', message: getErrorMessage(error), variant: 'error' }),
    });
  };

  return (
    <Screen edges={['top', 'left', 'right']} scroll padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter, gap: theme.spacing.md, paddingBottom: theme.spacing['3xl'] }}>
        <ScreenHeader title={c.title} subtitle={isLawyer ? c.customerName : `Adv. ${c.lawyerName}`} onBack={onBack} />

        <Card>
          <View style={{ gap: 6 }}>
            <View style={{ flexDirection: 'row', gap: theme.spacing.xs, flexWrap: 'wrap' }}>
              <Badge label={c.status} tone={statusTone(c.status)} />
              <Badge label={c.priority} tone="neutral" />
              {c.caseNumber ? <Badge label={c.caseNumber} tone="neutral" /> : null}
            </View>
            <Text variant="caption" color="secondary">{c.caseType}{c.courtName ? ` · ${c.courtName}` : ''}</Text>
            {c.description ? <Text variant="body">{c.description}</Text> : null}
          </View>
        </Card>

        <View style={{ backgroundColor: theme.colors.accentMuted, borderRadius: theme.radius.lg, padding: theme.spacing.md, gap: 4 }}>
          <Text variant="label" color="accent">{upcoming ? `NEXT ${upcoming.kind.toUpperCase()}` : 'UPCOMING'}</Text>
          {upcoming ? (
            <>
              <Text variant="bodyMedium">{upcoming.title}</Text>
              <Text variant="caption" color="secondary">
                {new Date(upcoming.when).toLocaleDateString()}{upcoming.location ? ` · ${upcoming.location}` : ''} · {upcoming.status}
              </Text>
            </>
          ) : (
            <Text variant="body" color="secondary">No upcoming hearing or milestone scheduled.</Text>
          )}
        </View>

        <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs }}>
          {(['overview', 'timeline', 'hearings', 'milestones', 'documents'] as Section[]).map((s) => (
            <Chip key={s} label={s[0].toUpperCase() + s.slice(1)} selected={section === s} onPress={() => setSection(s)} />
          ))}
        </View>

        {section === 'overview' ? (
          <View style={{ gap: theme.spacing.md }}>
            <Section title="Activity">
              <Text variant="body">Last Update: {signals.daysSinceLastUpdate == null ? 'never' : `${signals.daysSinceLastUpdate}d ago`}</Text>
              <Text variant="body">
                Last lawyer update: {signals.daysSinceLastMeaningfulLawyerActivity == null ? 'never' : `${signals.daysSinceLastMeaningfulLawyerActivity}d ago`}
              </Text>
              <Text variant="body">Overdue Milestones: {signals.overdueMilestoneCount}</Text>
              <Text variant="body">Adjourned Hearings: {signals.missedHearingCount}</Text>
              <Text variant="body">Pending Actions: {signals.pendingActionsCount}</Text>
              {signals.stallingStatus === 'Stalling' || signals.stallingStatus === 'SevereStalling' ? (
                <View style={{ backgroundColor: theme.colors.warningMuted, borderRadius: theme.radius.md, padding: theme.spacing.sm, marginTop: theme.spacing.xs }}>
                  <Text variant="caption">
                    ⚠ Lawyer has not provided meaningful progress for {signals.daysSinceLastMeaningfulLawyerActivity} days.
                  </Text>
                </View>
              ) : null}
            </Section>
            {isLawyer ? (
              <View style={{ gap: theme.spacing.sm }}>
                <Button title="Add Update" variant="secondary" leftSlot={<Icon name="edit" size={16} color={theme.colors.textPrimary} />} onPress={() => setShowUpdateModal(true)} />
                <Button title="Change Status" variant="outline" onPress={() => { setStatusChoice(c.status); setShowStatusModal(true); }} />
              </View>
            ) : null}
            <Section title="Rating">
              {rating ? (
                <View style={{ gap: 4 }}>
                  <Rating value={rating.stars} />
                  {rating.reviewText ? <Text variant="body" color="secondary">{rating.reviewText}</Text> : null}
                  <Text variant="caption" color="secondary">Rated {new Date(rating.createdAt).toLocaleDateString()}</Text>
                </View>
              ) : !isLawyer && isRatable ? (
                <Button title="Rate this case" variant="secondary" leftSlot={<Icon name="star" size={16} color={theme.colors.textPrimary} />} onPress={() => { setRatingStars(5); setRatingReview(''); setShowRatingModal(true); }} />
              ) : !isLawyer ? (
                <Text variant="caption" color="secondary">You can rate this case once it concludes.</Text>
              ) : (
                <Text variant="caption" color="secondary">Not yet rated by the customer.</Text>
              )}
              {lawyerRatingSummary.data ? (
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: theme.spacing.xs, marginTop: theme.spacing.xs }}>
                  <Rating value={lawyerRatingSummary.data.ratingAverage} showValue />
                  <Text variant="caption" color="secondary">({lawyerRatingSummary.data.ratingCount} lawyer rating{lawyerRatingSummary.data.ratingCount === 1 ? '' : 's'})</Text>
                </View>
              ) : null}
            </Section>
          </View>
        ) : null}

        {section === 'timeline' ? (
          timeline.isLoading ? <LoadingState label="Loading timeline" /> : <CaseTimeline entries={timeline.data ?? []} />
        ) : null}

        {section === 'hearings' ? (
          <View style={{ gap: theme.spacing.sm }}>
            {isLawyer ? <Button title="Schedule Hearing" variant="secondary" onPress={() => setShowHearingModal(true)} /> : null}
            {(hearings.data ?? []).map((h) => (
              <Card key={h.id}>
                <View style={{ gap: 4 }}>
                  <View style={{ flexDirection: 'row', justifyContent: 'space-between' }}>
                    <Text variant="bodyMedium">{h.hearingType ?? 'Hearing'}</Text>
                    <Badge label={h.status} tone={statusTone(h.status)} />
                  </View>
                  <Text variant="caption" color="secondary">{new Date(h.hearingDate).toLocaleString()}{h.courtName ? ` · ${h.courtName}` : ''}</Text>
                  {h.outcome ? <Text variant="caption">Outcome: {h.outcome}</Text> : null}
                  {h.nextAction ? <Text variant="caption" color="accent">Next: {h.nextAction}</Text> : null}
                  {isLawyer && h.status === 'Scheduled' ? (
                    <Button title="Record Outcome" variant="outline" size="sm" onPress={() => { setOutcomeHearing(h); setOutcomeStatus('Completed'); setOutcomeText(''); setOutcomeNotes(''); setOutcomeNextAction(''); }} />
                  ) : null}
                </View>
              </Card>
            ))}
            {(hearings.data ?? []).length === 0 ? <Text variant="caption" color="secondary">No hearings yet.</Text> : null}
          </View>
        ) : null}

        {section === 'milestones' ? (
          <View style={{ gap: theme.spacing.sm }}>
            {isLawyer ? <Button title="Add Milestone" variant="secondary" onPress={() => setShowMilestoneModal(true)} /> : null}
            {(milestones.data ?? []).map((m) => (
              <Card key={m.id}>
                <View style={{ gap: 4 }}>
                  <View style={{ flexDirection: 'row', justifyContent: 'space-between' }}>
                    <Text variant="bodyMedium">{m.title}</Text>
                    <Badge label={m.status} tone={statusTone(m.status)} />
                  </View>
                  {m.description ? <Text variant="caption" color="secondary">{m.description}</Text> : null}
                  {m.dueDate ? <Text variant="caption" color="secondary">Due {new Date(m.dueDate).toLocaleDateString()}</Text> : null}
                  {isLawyer && (m.status === 'Pending' || m.status === 'InProgress') ? (
                    <Button title="Mark Completed" variant="outline" size="sm" loading={completeMilestone.isPending} onPress={() => completeMilestone.mutate(m.id)} />
                  ) : null}
                </View>
              </Card>
            ))}
            {(milestones.data ?? []).length === 0 ? <Text variant="caption" color="secondary">No milestones yet.</Text> : null}
          </View>
        ) : null}

        {section === 'documents' ? (
          <View style={{ gap: theme.spacing.sm }}>
            <Button title="Upload Document" variant="secondary" leftSlot={<Icon name="paperclip" size={16} color={theme.colors.textPrimary} />} loading={uploadDocument.isPending} onPress={uploadForCase} />
            {uploadProgressPct != null ? <Text variant="caption" color="secondary">Uploading… {uploadProgressPct}%</Text> : null}
            {documents.isLoading ? <LoadingState label="Loading documents" /> : null}
            {(documents.data ?? []).map((d) => {
              const canDelete = isLawyer || d.uploadedByUserId === userId;
              return (
                <Card key={d.id}>
                  <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', gap: theme.spacing.sm }}>
                    <View style={{ gap: 2, flex: 1 }}>
                      <Text variant="bodyMedium">{d.fileName}</Text>
                      <Text variant="caption" color="secondary">{(d.fileSizeBytes / 1024).toFixed(0)} KB · {new Date(d.createdAt).toLocaleDateString()}</Text>
                    </View>
                    {canDelete ? (
                      <Button
                        title="Delete"
                        variant="danger"
                        size="sm"
                        loading={deleteDocument.isPending}
                        onPress={() => removeDocument(d.id)}
                      />
                    ) : null}
                  </View>
                </Card>
              );
            })}
            {!documents.isLoading && (documents.data ?? []).length === 0 ? <Text variant="caption" color="secondary">No documents attached.</Text> : null}
          </View>
        ) : null}
      </View>

      <AppModal visible={showStatusModal} onClose={() => setShowStatusModal(false)} title="Change status"
        footer={<>
          <Button title="Save" variant="primary" loading={changeStatus.isPending} onPress={() => changeStatus.mutate({ status: statusChoice, note: statusNote.trim() || undefined }, {
            onSuccess: () => { setShowStatusModal(false); setStatusNote(''); showToast({ title: 'Status updated', variant: 'success' }); },
            onError: (error) => showToast({ title: 'Could not update status', message: getErrorMessage(error), variant: 'error' }),
          })} />
          <Button title="Cancel" variant="ghost" onPress={() => setShowStatusModal(false)} />
        </>}>
        <View style={{ gap: theme.spacing.md }}>
          <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs }}>
            {CASE_STATUSES.map((s) => (
              <Chip key={s} label={s} selected={statusChoice === s} onPress={() => setStatusChoice(s)} />
            ))}
          </View>
          <TextField label="Note (optional)" value={statusNote} onChangeText={setStatusNote} multiline />
        </View>
      </AppModal>

      <AppModal visible={showUpdateModal} onClose={() => setShowUpdateModal(false)} title="Add update"
        footer={<>
          <Button title="Save" variant="primary" disabled={!updateTitle.trim() || !updateDescription.trim()} loading={createUpdate.isPending}
            onPress={() => createUpdate.mutate({ updateType, title: updateTitle.trim(), description: updateDescription.trim() }, {
              onSuccess: () => { setShowUpdateModal(false); setUpdateTitle(''); setUpdateDescription(''); showToast({ title: 'Update added', variant: 'success' }); },
              onError: (error) => showToast({ title: 'Could not add update', message: getErrorMessage(error), variant: 'error' }),
            })} />
          <Button title="Cancel" variant="ghost" onPress={() => setShowUpdateModal(false)} />
        </>}>
        <View style={{ gap: theme.spacing.md }}>
          <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs }}>
            {UPDATE_TYPES.map((t) => (
              <Chip key={t} label={t} selected={updateType === t} onPress={() => setUpdateType(t)} />
            ))}
          </View>
          <TextField label="Title" value={updateTitle} onChangeText={setUpdateTitle} />
          <TextField label="Description" value={updateDescription} onChangeText={setUpdateDescription} multiline />
        </View>
      </AppModal>

      <AppModal visible={showHearingModal} onClose={() => setShowHearingModal(false)} title="Schedule hearing"
        footer={<>
          <Button title="Save" variant="primary" disabled={!hearingDate.trim()} loading={createHearing.isPending}
            onPress={() => createHearing.mutate({ hearingDate: new Date(hearingDate).toISOString(), courtName: hearingCourt.trim() || undefined, hearingType: hearingType.trim() || undefined, purpose: hearingPurpose.trim() || undefined }, {
              onSuccess: () => { setShowHearingModal(false); setHearingDate(''); setHearingCourt(''); setHearingType(''); setHearingPurpose(''); showToast({ title: 'Hearing scheduled', variant: 'success' }); },
              onError: (error) => showToast({ title: 'Could not schedule hearing', message: getErrorMessage(error, 'Check the date format'), variant: 'error' }),
            })} />
          <Button title="Cancel" variant="ghost" onPress={() => setShowHearingModal(false)} />
        </>}>
        <View style={{ gap: theme.spacing.md }}>
          <TextField label="Hearing date & time" placeholder="YYYY-MM-DD HH:mm" value={hearingDate} onChangeText={setHearingDate} hint="e.g. 2026-09-18 10:00" />
          <TextField label="Court" value={hearingCourt} onChangeText={setHearingCourt} />
          <TextField label="Type" value={hearingType} onChangeText={setHearingType} placeholder="e.g. First Hearing" />
          <TextField label="Purpose" value={hearingPurpose} onChangeText={setHearingPurpose} multiline />
        </View>
      </AppModal>

      <AppModal visible={!!outcomeHearing} onClose={() => setOutcomeHearing(null)} title="Record hearing outcome"
        footer={<>
          <Button title="Save" variant="primary" loading={recordOutcome.isPending}
            onPress={() => {
              if (!outcomeHearing) return;
              recordOutcome.mutate({ hearingId: outcomeHearing.id, status: outcomeStatus, outcome: outcomeText.trim() || undefined, notes: outcomeNotes.trim() || undefined, nextAction: outcomeNextAction.trim() || undefined }, {
                onSuccess: () => { setOutcomeHearing(null); showToast({ title: 'Outcome recorded', variant: 'success' }); },
                onError: (error) => showToast({ title: 'Could not record outcome', message: getErrorMessage(error), variant: 'error' }),
              });
            }} />
          <Button title="Cancel" variant="ghost" onPress={() => setOutcomeHearing(null)} />
        </>}>
        <View style={{ gap: theme.spacing.md }}>
          <View style={{ flexDirection: 'row', gap: theme.spacing.xs }}>
            {(['Completed', 'Adjourned', 'Cancelled'] as HearingStatus[]).map((s) => (
              <Chip key={s} label={s} selected={outcomeStatus === s} onPress={() => setOutcomeStatus(s)} />
            ))}
          </View>
          <TextField label="Outcome" value={outcomeText} onChangeText={setOutcomeText} multiline />
          <TextField label="Notes" value={outcomeNotes} onChangeText={setOutcomeNotes} multiline />
          <TextField label="Next action" value={outcomeNextAction} onChangeText={setOutcomeNextAction} placeholder="e.g. Submit response" />
        </View>
      </AppModal>

      <AppModal visible={showMilestoneModal} onClose={() => setShowMilestoneModal(false)} title="Add milestone"
        footer={<>
          <Button title="Save" variant="primary" disabled={!milestoneTitle.trim()} loading={createMilestone.isPending}
            onPress={() => createMilestone.mutate({ title: milestoneTitle.trim(), description: milestoneDescription.trim() || undefined, dueDate: milestoneDueDate.trim() ? new Date(milestoneDueDate).toISOString() : undefined }, {
              onSuccess: () => { setShowMilestoneModal(false); setMilestoneTitle(''); setMilestoneDescription(''); setMilestoneDueDate(''); showToast({ title: 'Milestone added', variant: 'success' }); },
              onError: (error) => showToast({ title: 'Could not add milestone', message: getErrorMessage(error), variant: 'error' }),
            })} />
          <Button title="Cancel" variant="ghost" onPress={() => setShowMilestoneModal(false)} />
        </>}>
        <View style={{ gap: theme.spacing.md }}>
          <TextField label="Title" value={milestoneTitle} onChangeText={setMilestoneTitle} placeholder="e.g. Evidence Submitted" />
          <TextField label="Description (optional)" value={milestoneDescription} onChangeText={setMilestoneDescription} multiline />
          <TextField label="Due date (optional)" value={milestoneDueDate} onChangeText={setMilestoneDueDate} placeholder="YYYY-MM-DD" />
        </View>
      </AppModal>

      <AppModal visible={showRatingModal} onClose={() => setShowRatingModal(false)} title="Rate this case"
        footer={<>
          <Button title="Submit" variant="primary" loading={submitRating.isPending}
            onPress={() => submitRating.mutate({ stars: ratingStars, reviewText: ratingReview.trim() || undefined }, {
              onSuccess: () => { setShowRatingModal(false); showToast({ title: 'Thanks for your rating', variant: 'success' }); },
              onError: (error) => showToast({ title: 'Could not submit rating', message: getErrorMessage(error, 'You may have already rated this case'), variant: 'error' }),
            })} />
          <Button title="Cancel" variant="ghost" onPress={() => setShowRatingModal(false)} />
        </>}>
        <View style={{ gap: theme.spacing.md, alignItems: 'center' }}>
          <Rating value={ratingStars} interactive size={32} onChange={setRatingStars} />
          <TextField label="Review (optional)" value={ratingReview} onChangeText={setRatingReview} multiline />
        </View>
      </AppModal>
    </Screen>
  );
}
