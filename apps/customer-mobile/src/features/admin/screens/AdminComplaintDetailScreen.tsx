import React, { useState } from 'react';
import { View } from 'react-native';
import { useRoute, useNavigation, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { AppModal } from '@/components/ui/Modal';
import { Text } from '@/components/ui/Text';
import { TextField } from '@/components/ui/TextField';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { useDismissComplaintMutation, useVerifyComplaintMutation } from '@/features/admin/hooks/useAdminQueries';
import type { AdminComplaintsStackParamList } from '@/navigation/types';
import type { ComplaintSeverity } from '@/types/api';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';

type Route = RouteProp<AdminComplaintsStackParamList, 'AdminComplaintDetail'>;
type Nav = NativeStackNavigationProp<AdminComplaintsStackParamList, 'AdminComplaintDetail'>;

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

const SEVERITIES: { label: string; value: ComplaintSeverity }[] = [
  { label: 'Minor', value: 0 },
  { label: 'Serious', value: 1 },
];

export function AdminComplaintDetailScreen() {
  const theme = useTheme();
  const navigation = useNavigation<Nav>();
  const { params } = useRoute<Route>();
  const showToast = useToastStore((s) => s.show);
  const { complaint } = params;

  const [showVerifyModal, setShowVerifyModal] = useState(false);
  const [severity, setSeverity] = useState<ComplaintSeverity>(1);
  const [verifyNotes, setVerifyNotes] = useState('');
  const [showDismissModal, setShowDismissModal] = useState(false);
  const [dismissNotes, setDismissNotes] = useState('');

  const verify = useVerifyComplaintMutation();
  const dismiss = useDismissComplaintMutation();

  const isPending = complaint.status === 'Pending';

  return (
    <Screen edges={['top', 'left', 'right']} scroll padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter, gap: theme.spacing.md, paddingBottom: theme.spacing['3xl'] }}>
        <ScreenHeader title={complaint.category} onBack={() => navigation.goBack()} />

        <Badge
          label={complaint.status}
          tone={complaint.status === 'Pending' ? 'warning' : complaint.status === 'Verified' ? 'danger' : 'neutral'}
        />

        <Section title="Complaint">
          <Field label="Against lawyer" value={complaint.lawyerName} />
          <Field label="Filed by" value={complaint.clientName} />
          <Field label="Category" value={complaint.category} />
          <Field label="Description" value={complaint.description} />
          <Field label="Filed" value={new Date(complaint.createdAtUtc).toLocaleString()} />
        </Section>

        {complaint.status !== 'Pending' ? (
          <Section title="Admin review">
            <Field label="Severity" value={complaint.severity ?? '—'} />
            <Field label="Notes" value={complaint.adminNotes} />
            <Field
              label="Reviewed"
              value={complaint.reviewedAtUtc ? new Date(complaint.reviewedAtUtc).toLocaleString() : null}
            />
          </Section>
        ) : null}

        {isPending ? (
          <View style={{ gap: theme.spacing.sm }}>
            <Button title="Verify complaint" variant="danger" onPress={() => setShowVerifyModal(true)} />
            <Button title="Dismiss complaint" variant="outline" onPress={() => setShowDismissModal(true)} />
          </View>
        ) : null}
      </View>

      <AppModal
        visible={showVerifyModal}
        onClose={() => setShowVerifyModal(false)}
        title="Verify complaint"
        footer={
          <>
            <Button
              title="Verify"
              variant="danger"
              loading={verify.isPending}
              onPress={() =>
                verify.mutate(
                  { complaintId: complaint.complaintId, severity, notes: verifyNotes.trim() || undefined },
                  {
                    onSuccess: () => {
                      setShowVerifyModal(false);
                      showToast({ title: 'Complaint verified', message: 'Trust score has been recalculated.', variant: 'success' });
                      navigation.goBack();
                    },
                    onError: (error) =>
                      showToast({
                        title: 'Verify failed',
                        message: getErrorMessage(error, 'Could not verify complaint'),
                        variant: 'error',
                      }),
                  },
                )
              }
            />
            <Button title="Cancel" variant="ghost" onPress={() => setShowVerifyModal(false)} />
          </>
        }
      >
        <View style={{ gap: theme.spacing.md }}>
          <Text variant="body" color="secondary">
            A verified complaint reduces this lawyer's Trust Score. Choose how severe it was.
          </Text>
          <View style={{ flexDirection: 'row', gap: theme.spacing.sm }}>
            {SEVERITIES.map((s) => (
              <Button
                key={s.value}
                title={s.label}
                variant={severity === s.value ? 'primary' : 'outline'}
                size="sm"
                style={{ flex: 1 }}
                onPress={() => setSeverity(s.value)}
              />
            ))}
          </View>
          <TextField label="Notes (optional)" value={verifyNotes} onChangeText={setVerifyNotes} multiline />
        </View>
      </AppModal>

      <AppModal
        visible={showDismissModal}
        onClose={() => setShowDismissModal(false)}
        title="Dismiss complaint"
        footer={
          <>
            <Button
              title="Dismiss"
              variant="primary"
              loading={dismiss.isPending}
              onPress={() =>
                dismiss.mutate(
                  { complaintId: complaint.complaintId, notes: dismissNotes.trim() || undefined },
                  {
                    onSuccess: () => {
                      setShowDismissModal(false);
                      showToast({ title: 'Complaint dismissed', variant: 'info' });
                      navigation.goBack();
                    },
                    onError: (error) =>
                      showToast({
                        title: 'Dismiss failed',
                        message: getErrorMessage(error, 'Could not dismiss complaint'),
                        variant: 'error',
                      }),
                  },
                )
              }
            />
            <Button title="Cancel" variant="ghost" onPress={() => setShowDismissModal(false)} />
          </>
        }
      >
        <View style={{ gap: theme.spacing.md }}>
          <Text variant="body" color="secondary">
            Dismissing this complaint will not affect the lawyer's Trust Score.
          </Text>
          <TextField label="Notes (optional)" value={dismissNotes} onChangeText={setDismissNotes} multiline />
        </View>
      </AppModal>
    </Screen>
  );
}
