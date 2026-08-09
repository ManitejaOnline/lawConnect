import React, { useEffect, useState } from 'react';
import { Pressable, RefreshControl, ScrollView, Switch, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { EmptyState } from '@/components/feedback/EmptyState';
import { ErrorState } from '@/components/feedback/ErrorState';
import { LoadingState } from '@/components/feedback/LoadingState';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import {
  useAcceptSosMutation,
  useDeclineSosMutation,
  useIncomingSosQuery,
  useLawyerActiveSosQuery,
  useLawyerEmergencyHistoryQuery,
  useMyLawyerProfileQuery,
  useSetEmergencyAvailabilityMutation,
} from '@/features/lawyer/hooks/useLawyerQueries';
import type { LawyerEmergencyStackParamList } from '@/navigation/types';
import { useLawyerEmergencyStore } from '@/store/lawyerEmergencyStore';
import { useToastStore } from '@/store/toastStore';
import type { IncomingEmergencySos } from '@/types/emergency';
import { isAssignedStatus } from '@/types/emergency';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<LawyerEmergencyStackParamList, 'LawyerEmergencyInbox'>;
type TabKey = 'incoming' | 'active' | 'history';

function receivedLabel(item: IncomingEmergencySos): string {
  const receivedAt = item.notifiedAtUtc
    ? new Date(item.notifiedAtUtc)
    : new Date(new Date(item.expiresAtUtc).getTime() - Math.max(item.remainingSeconds, 1) * 1000);
  return receivedAt.toLocaleString(undefined, { hour: 'numeric', minute: '2-digit' });
}

export function LawyerEmergencyScreen({ navigation }: Props) {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const mine = useMyLawyerProfileQuery();
  const incoming = useIncomingSosQuery();
  const activeQuery = useLawyerActiveSosQuery();
  const history = useLawyerEmergencyHistoryQuery();
  const accept = useAcceptSosMutation();
  const decline = useDeclineSosMutation();
  const setEmergency = useSetEmergencyAvailabilityMutation();
  const setActive = useLawyerEmergencyStore((s) => s.setActiveRequest);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [tab, setTab] = useState<TabKey>('incoming');

  useEffect(() => {
    if (activeQuery.data) setActive(activeQuery.data);
  }, [activeQuery.data, setActive]);

  const onAccept = (item: IncomingEmergencySos) => {
    setBusyId(item.assignmentId);
    accept.mutate(item.assignmentId, {
      onSuccess: (request) => {
        setActive(request);
        showToast({ title: 'Emergency accepted', variant: 'success' });
        navigation.navigate('LawyerEmergencyActive', { requestId: request.requestId });
      },
      onError: () => {
        showToast({
          title: 'Accept failed',
          message: 'This emergency is no longer available.',
          variant: 'error',
        });
        void incoming.refetch();
      },
      onSettled: () => setBusyId(null),
    });
  };

  const onDecline = (item: IncomingEmergencySos) => {
    setBusyId(item.assignmentId);
    decline.mutate(item.assignmentId, {
      onSuccess: () => showToast({ title: 'Declined', variant: 'info' }),
      onError: () =>
        showToast({
          title: 'Decline failed',
          message: 'Could not decline this request. Please try again.',
          variant: 'error',
        }),
      onSettled: () => setBusyId(null),
    });
  };

  const tabs: { key: TabKey; label: string }[] = [
    { key: 'incoming', label: 'Incoming' },
    { key: 'active', label: 'Active' },
    { key: 'history', label: 'History' },
  ];

  return (
    <Screen edges={['top', 'left', 'right']} padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter }}>
        <ScreenHeader title="Emergency" subtitle="Incoming requests appear here" />
      </View>

      {incoming.isLoading && !incoming.data ? (
        <LoadingState label="Checking incoming SOS" fullScreen />
      ) : incoming.isError && !incoming.data ? (
        <ErrorState message="Could not load incoming SOS." onRetry={() => incoming.refetch()} />
      ) : (
        <ScrollView
          refreshControl={
            <RefreshControl
              refreshing={incoming.isRefetching || Boolean(activeQuery.isRefetching)}
              onRefresh={() => {
                void incoming.refetch();
                void activeQuery.refetch();
                void history.refetch();
              }}
            />
          }
          contentContainerStyle={{
            paddingHorizontal: theme.layout.gutter,
            paddingBottom: theme.spacing['3xl'],
            gap: theme.spacing.md,
            flexGrow: 1,
          }}
        >
          <Card>
            <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}>
              <View style={{ flex: 1, paddingRight: theme.spacing.sm }}>
                <Text variant="bodyMedium">Available for emergency</Text>
                <Text variant="caption" color="secondary">
                  Fee ₹{mine.data?.emergencyFee ?? mine.data?.consultationFee ?? 0}
                </Text>
              </View>
              <Switch
                value={Boolean(mine.data?.isAvailableForEmergency)}
                disabled={setEmergency.isPending}
                onValueChange={(value) => setEmergency.mutate(value)}
              />
            </View>
          </Card>

          <View style={{ flexDirection: 'row', gap: theme.spacing.xs }}>
            {tabs.map((item) => {
              const selected = tab === item.key;
              return (
                <Pressable
                  key={item.key}
                  onPress={() => setTab(item.key)}
                  style={{
                    flex: 1,
                    paddingVertical: theme.spacing.sm,
                    borderRadius: theme.radius.lg,
                    backgroundColor: selected ? theme.colors.primaryMuted : theme.colors.surface,
                    borderWidth: 1,
                    borderColor: selected ? theme.colors.primary : theme.colors.border,
                    alignItems: 'center',
                  }}
                >
                  <Text variant="bodyMedium" style={{ color: selected ? theme.colors.primary : theme.colors.textPrimary }}>
                    {item.label}
                  </Text>
                </Pressable>
              );
            })}
          </View>

          {tab === 'incoming'
            ? (incoming.data ?? []).length === 0
              ? (
                <EmptyState
                  variant="inbox"
                  title="No incoming SOS"
                  description="Emergency requests appear here when you are online and available for emergency."
                />
              )
              : (incoming.data ?? []).map((item) => {
                  const busy = busyId === item.assignmentId;
                  return (
                    <Card key={item.assignmentId}>
                      <View style={{ gap: theme.spacing.xs }}>
                        <Text variant="label" color="emergency">
                          Emergency Request
                        </Text>
                        <Text variant="headline">{item.categoryName}</Text>
                        {item.cityName ? (
                          <Text variant="body" color="secondary">
                            {item.cityName}
                          </Text>
                        ) : null}
                        <Text variant="caption" color="secondary">
                          Received {receivedLabel(item)}
                        </Text>
                        <View style={{ flexDirection: 'row', gap: theme.spacing.xs, marginTop: theme.spacing.sm }}>
                          <Button
                            title="Decline"
                            variant="outline"
                            fullWidth={false}
                            style={{ flex: 1 }}
                            disabled={busy}
                            loading={decline.isPending && busyId === item.assignmentId}
                            onPress={() => onDecline(item)}
                          />
                          <Button
                            title="Accept"
                            variant="danger"
                            fullWidth={false}
                            style={{ flex: 1 }}
                            disabled={busy}
                            loading={accept.isPending && busyId === item.assignmentId}
                            onPress={() => onAccept(item)}
                          />
                        </View>
                      </View>
                    </Card>
                  );
                })
            : null}

          {tab === 'active' ? (
            activeQuery.data && isAssignedStatus(activeQuery.data.status) ? (
              <Card
                onPress={() =>
                  navigation.navigate('LawyerEmergencyActive', { requestId: activeQuery.data!.requestId })
                }
              >
                <Text variant="label" color="emergency">
                  Active emergency
                </Text>
                <Text variant="headline" style={{ marginTop: theme.spacing.xs }}>
                  {activeQuery.data.categoryName}
                </Text>
                <Button
                  title="Open"
                  variant="danger"
                  style={{ marginTop: theme.spacing.sm }}
                  onPress={() =>
                    navigation.navigate('LawyerEmergencyActive', { requestId: activeQuery.data!.requestId })
                  }
                />
              </Card>
            ) : (
              <EmptyState
                variant="inbox"
                title="No active emergency"
                description="Accepted SOS consultations appear here until they are completed."
              />
            )
          ) : null}

          {tab === 'history' ? (
            (history.data?.items ?? []).length === 0 ? (
              <EmptyState variant="inbox" title="No emergency history" description="Completed and cancelled SOS will appear here." />
            ) : (
              (history.data?.items ?? []).map((item) => (
                <Card key={item.historyId}>
                  <Text variant="bodyMedium">{item.finalStatus}</Text>
                  <Text variant="caption" color="secondary">
                    {new Date(item.closedAtUtc).toLocaleString()}
                  </Text>
                </Card>
              ))
            )
          ) : null}
        </ScrollView>
      )}
    </Screen>
  );
}
