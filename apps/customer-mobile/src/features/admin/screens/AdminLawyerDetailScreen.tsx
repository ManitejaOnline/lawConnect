import React, { useState } from 'react';
import { View } from 'react-native';
import { useRoute, useNavigation, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

import { ErrorState } from '@/components/feedback/ErrorState';
import { LoadingState } from '@/components/feedback/LoadingState';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { AppModal } from '@/components/ui/Modal';
import { Rating } from '@/components/ui/Rating';
import { Text } from '@/components/ui/Text';
import { TextField } from '@/components/ui/TextField';
import { Icon } from '@/components/icons/Icon';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { TrustScoreBreakdownList } from '@/components/lawyer/TrustScoreBreakdownList';
import {
  useAdminApplicationQuery,
  useAdminLawyerTrustScoreQuery,
  useApproveLawyerMutation,
  useRejectLawyerMutation,
} from '@/features/admin/hooks/useAdminQueries';
import { isReviewable, statusTone } from '@/features/admin/statusTone';
import type { AdminLawyersStackParamList } from '@/navigation/types';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';
import { trustLevelTone } from '@/utils/trustLevel';

type Route = RouteProp<AdminLawyersStackParamList, 'AdminLawyerDetail'>;
type Nav = NativeStackNavigationProp<AdminLawyersStackParamList, 'AdminLawyerDetail'>;

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <View style={{ gap: 2 }}>
      <Text variant="label" color="secondary">
        {label}
      </Text>
      <Text variant="body">{value ?? '—'}</Text>
    </View>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  const theme = useTheme();
  return (
    <View
      style={{
        borderWidth: 1,
        borderColor: theme.colors.border,
        borderRadius: theme.radius.lg,
        padding: theme.spacing.md,
        gap: theme.spacing.sm,
        backgroundColor: theme.colors.surface,
      }}
    >
      <Text variant="bodyMedium">{title}</Text>
      {children}
    </View>
  );
}

export function AdminLawyerDetailScreen() {
  const theme = useTheme();
  const navigation = useNavigation<Nav>();
  const { params } = useRoute<Route>();
  const showToast = useToastStore((s) => s.show);
  const [rejectReason, setRejectReason] = useState('');
  const [showRejectModal, setShowRejectModal] = useState(false);

  const query = useAdminApplicationQuery(params.lawyerId);
  const trustScore = useAdminLawyerTrustScoreQuery(params.lawyerId);
  const approve = useApproveLawyerMutation();
  const reject = useRejectLawyerMutation();

  if (query.isLoading) {
    return <LoadingState fullScreen label="Loading application" />;
  }

  if (query.isError || !query.data) {
    return <ErrorState message="Could not load this application." onRetry={() => query.refetch()} />;
  }

  const app = query.data;
  const canReview = isReviewable(app.verificationStatus);

  return (
    <Screen edges={['top', 'left', 'right']} scroll padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter, gap: theme.spacing.md, paddingBottom: theme.spacing['3xl'] }}>
        <ScreenHeader title={`${app.firstName} ${app.lastName}`.trim()} onBack={() => navigation.goBack()} />

        <View style={{ flexDirection: 'row', alignItems: 'center', gap: theme.spacing.sm }}>
          <Badge label={app.verificationStatus} tone={statusTone(app.verificationStatus)} />
        </View>

        {app.verificationStatus === 'Rejected' && app.reviewReason ? (
          <View
            style={{
              backgroundColor: theme.colors.dangerMuted,
              borderRadius: theme.radius.md,
              padding: theme.spacing.md,
            }}
          >
            <Text variant="bodyMedium" style={{ color: theme.colors.danger }}>
              Rejection reason
            </Text>
            <Text variant="body" style={{ color: theme.colors.danger, marginTop: 2 }}>
              {app.reviewReason}
            </Text>
          </View>
        ) : null}

        <Section title="Contact">
          <Field label="Email" value={app.email} />
          <Field label="Phone" value={app.phoneNumber} />
          <Field label="Location" value={[app.cityName, app.stateName, app.countryName].filter(Boolean).join(', ')} />
        </Section>

        <Section title="Professional details">
          <Field label="Bar Council" value={app.stateBarCouncil} />
          <Field label="Registration Number" value={app.enrollmentNumber} />
          <Field label="Years of experience" value={app.yearsOfExperience} />
          <Field label="Consultation fee" value={`₹${app.consultationFee}`} />
          <Field label="Specializations" value={app.practiceAreas.join(', ')} />
          <Field label="Languages" value={app.languages.join(', ')} />
          <Field label="Education" value={app.education.join('; ')} />
          <Field label="Bio" value={app.bio} />
        </Section>

        <Section title="Application">
          <Field
            label="Submitted"
            value={app.applicationSubmittedAtUtc ? new Date(app.applicationSubmittedAtUtc).toLocaleString() : null}
          />
          <Field label="Reviewed" value={app.reviewedAtUtc ? new Date(app.reviewedAtUtc).toLocaleString() : null} />
          <Field label="Verified" value={app.verifiedAtUtc ? new Date(app.verifiedAtUtc).toLocaleString() : null} />
        </Section>

        {trustScore.data ? (
          <Section title="Trust Score">
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: theme.spacing.sm }}>
              <Icon name="shield" size={22} color={theme.colors.accent} />
              <Text variant="title">{trustScore.data.current.score}/100</Text>
              <Badge label={trustScore.data.current.level} tone={trustLevelTone(trustScore.data.current.level)} />
            </View>
            <Rating value={trustScore.data.current.rating} showValue size={14} />
            <Text variant="caption" color="secondary">
              {trustScore.data.current.reviewCount} review{trustScore.data.current.reviewCount === 1 ? '' : 's'} ·{' '}
              {trustScore.data.complaints.filter((c) => c.status === 'Verified').length} verified complaint
              {trustScore.data.complaints.filter((c) => c.status === 'Verified').length === 1 ? '' : 's'}
            </Text>

            {trustScore.data.current.caseManagementNote ? (
              <View style={{ backgroundColor: theme.colors.warningMuted, borderRadius: theme.radius.md, padding: theme.spacing.sm, marginTop: theme.spacing.xs }}>
                <Text variant="caption">{trustScore.data.current.caseManagementNote}</Text>
              </View>
            ) : null}

            <View style={{ marginTop: theme.spacing.xs }}>
              <TrustScoreBreakdownList breakdown={trustScore.data.current.breakdown} />
            </View>

            {trustScore.data.history.length > 0 ? (
              <View style={{ marginTop: theme.spacing.sm, gap: 6 }}>
                <Text variant="label" color="secondary">
                  Recalculation history
                </Text>
                {trustScore.data.history.slice(0, 5).map((entry, i) => (
                  <View key={i} style={{ flexDirection: 'row', justifyContent: 'space-between' }}>
                    <Text variant="caption" color="secondary">
                      {new Date(entry.computedAtUtc).toLocaleString()}
                    </Text>
                    <Text variant="caption">
                      {entry.score}/100 · {entry.level}
                    </Text>
                  </View>
                ))}
              </View>
            ) : null}

            {trustScore.data.complaints.length > 0 ? (
              <View style={{ marginTop: theme.spacing.sm, gap: 6 }}>
                <Text variant="label" color="secondary">
                  Complaints
                </Text>
                {trustScore.data.complaints.map((c) => (
                  <View key={c.complaintId} style={{ flexDirection: 'row', justifyContent: 'space-between', gap: theme.spacing.sm }}>
                    <Text variant="caption" style={{ flex: 1 }} numberOfLines={1}>
                      {c.category}
                    </Text>
                    <Badge
                      label={c.status}
                      tone={c.status === 'Pending' ? 'warning' : c.status === 'Verified' ? 'danger' : 'neutral'}
                    />
                  </View>
                ))}
              </View>
            ) : null}
          </Section>
        ) : null}

        <Section title={`Documents (${app.documents.length})`}>
          {app.documents.length === 0 ? (
            <Text variant="body" color="secondary">
              No documents uploaded.
            </Text>
          ) : (
            app.documents.map((doc) => (
              <View key={doc.documentId} style={{ gap: 2 }}>
                <Text variant="bodyMedium">{doc.documentType}</Text>
                <Text variant="caption" color="secondary">
                  {doc.fileName} · {(doc.fileSizeBytes / 1024).toFixed(0)} KB
                </Text>
              </View>
            ))
          )}
        </Section>

        {canReview ? (
          <View style={{ gap: theme.spacing.sm, marginTop: theme.spacing.sm }}>
            <Button
              title="Approve Lawyer"
              variant="primary"
              loading={approve.isPending}
              onPress={() =>
                approve.mutate(app.lawyerId, {
                  onSuccess: () => showToast({ title: 'Lawyer approved', variant: 'success' }),
                  onError: (error) =>
                    showToast({
                      title: 'Approve failed',
                      message: getErrorMessage(error, 'Could not approve lawyer'),
                      variant: 'error',
                    }),
                })
              }
            />
            <Button title="Reject Lawyer" variant="danger" onPress={() => setShowRejectModal(true)} />
          </View>
        ) : null}
      </View>

      <AppModal
        visible={showRejectModal}
        onClose={() => setShowRejectModal(false)}
        title="Reject application"
        footer={
          <>
            <Button
              title="Reject"
              variant="danger"
              disabled={!rejectReason.trim()}
              loading={reject.isPending}
              onPress={() =>
                reject.mutate(
                  { lawyerId: app.lawyerId, reason: rejectReason.trim() },
                  {
                    onSuccess: () => {
                      setShowRejectModal(false);
                      setRejectReason('');
                      showToast({ title: 'Lawyer rejected', variant: 'success' });
                    },
                    onError: (error) =>
                      showToast({
                        title: 'Reject failed',
                        message: getErrorMessage(error, 'Could not reject lawyer'),
                        variant: 'error',
                      }),
                  },
                )
              }
            />
            <Button title="Cancel" variant="ghost" onPress={() => setShowRejectModal(false)} />
          </>
        }
      >
        <Text variant="body" color="secondary">
          A reason is required and will be shown to the lawyer.
        </Text>
        <TextField
          label="Reason"
          value={rejectReason}
          onChangeText={setRejectReason}
          multiline
          containerStyle={{ marginTop: theme.spacing.md }}
        />
      </AppModal>
    </Screen>
  );
}
