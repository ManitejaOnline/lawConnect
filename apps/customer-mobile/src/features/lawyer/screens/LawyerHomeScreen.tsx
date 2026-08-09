import React from 'react';
import { RefreshControl, ScrollView, Switch, View } from 'react-native';
import type { CompositeNavigationProp } from '@react-navigation/native';
import { useNavigation } from '@react-navigation/native';
import type { BottomTabNavigationProp } from '@react-navigation/bottom-tabs';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

import { isLawyerApproved } from '@/auth/roles';
import { ErrorState } from '@/components/feedback/ErrorState';
import { LoadingState } from '@/components/feedback/LoadingState';
import { Avatar } from '@/components/ui/Avatar';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { SectionHeader } from '@/components/home/SectionHeader';
import { AppointmentCard } from '@/features/appointments/components/AppointmentCard';
import { useUpcomingAppointmentsQuery } from '@/features/appointments/hooks/useAppointmentQueries';
import {
  useIncomingSosQuery,
  useMyLawyerProfileQuery,
  useSetEmergencyAvailabilityMutation,
  useSetOnlineStatusMutation,
} from '@/features/lawyer/hooks/useLawyerQueries';
import type {
  LawyerHomeStackParamList,
  LawyerTabParamList,
} from '@/navigation/types';
import { useChatStore } from '@/store/chatStore';
import { useLawyerEmergencyStore } from '@/store/lawyerEmergencyStore';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';

type Nav = CompositeNavigationProp<
  NativeStackNavigationProp<LawyerHomeStackParamList, 'LawyerHome'>,
  BottomTabNavigationProp<LawyerTabParamList>
>;

function greeting() {
  const hour = new Date().getHours();
  if (hour < 12) return 'Good morning';
  if (hour < 17) return 'Good afternoon';
  return 'Good evening';
}

export function LawyerHomeScreen() {
  const theme = useTheme();
  const navigation = useNavigation<Nav>();
  const showToast = useToastStore((s) => s.show);
  const mine = useMyLawyerProfileQuery();
  const upcoming = useUpcomingAppointmentsQuery();
  const incoming = useIncomingSosQuery();
  const setOnline = useSetOnlineStatusMutation();
  const setEmergency = useSetEmergencyAvailabilityMutation();
  const unread = useChatStore((s) => s.conversations.reduce((sum, c) => sum + (c.unreadCount || 0), 0));
  const activeSos = useLawyerEmergencyStore((s) => s.activeRequest);
  const profile = mine.data;
  const todayIso = new Date().toDateString();
  const todayAppointments = (upcoming.data?.items ?? []).filter(
    (row) => new Date(row.startUtc).toDateString() === todayIso,
  );
  const incomingRequest = incoming.data?.[0];

  const refresh = () => {
    void mine.refetch();
    void upcoming.refetch();
    void incoming.refetch();
  };

  if (mine.isLoading && !profile) {
    return (
      <Screen>
        <LoadingState label="Loading workspace" />
      </Screen>
    );
  }

  if (mine.isError && !profile) {
    return (
      <Screen>
        <ErrorState message="Could not load your lawyer profile." onRetry={() => mine.refetch()} />
      </Screen>
    );
  }

  const name = `${profile?.firstName ?? ''} ${profile?.lastName ?? ''}`.trim() || 'Lawyer';
  const operational = isLawyerApproved(profile?.verificationStatus, profile?.isVerified);

  return (
    <Screen edges={['top', 'left', 'right']} padded={false}>
      <ScrollView
        refreshControl={<RefreshControl refreshing={mine.isRefetching} onRefresh={refresh} />}
        contentContainerStyle={{
          paddingHorizontal: theme.layout.gutter,
          paddingTop: theme.spacing.md,
          paddingBottom: theme.spacing['3xl'],
          gap: theme.spacing.md,
        }}
      >
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: theme.spacing.sm }}>
          <View style={{ flex: 1, gap: 2 }}>
            <Text variant="caption" color="secondary">
              {greeting()}
            </Text>
            <Text variant="title" numberOfLines={1}>
              {name}
            </Text>
          </View>
          <Avatar name={name} uri={profile?.profileImageUrl} size="md" />
        </View>

        <Card>
          <View style={{ gap: theme.spacing.sm }}>
            <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}>
              <View>
                <Text variant="bodyMedium">{profile?.isOnline ? 'Online' : 'Offline'}</Text>
                <Text variant="caption" color="secondary">
                  Visible to customers on search
                </Text>
              </View>
              <Switch
                value={Boolean(profile?.isOnline)}
                disabled={setOnline.isPending || !operational}
                onValueChange={(value) =>
                  setOnline.mutate(value, {
                    onError: (error) =>
                      showToast({
                        title: 'Status failed',
                        message: getErrorMessage(error, 'Could not update online status'),
                        variant: 'error',
                      }),
                  })
                }
              />
            </View>
            <View style={{ height: 1, backgroundColor: theme.colors.border }} />
            <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}>
              <View style={{ flex: 1, paddingRight: theme.spacing.sm }}>
                <Text variant="bodyMedium">Emergency Available</Text>
                <Text variant="caption" color="secondary">
                  Receive emergency SOS requests
                </Text>
              </View>
              <Switch
                value={Boolean(profile?.isAvailableForEmergency)}
                disabled={setEmergency.isPending || !operational}
                onValueChange={(value) =>
                  setEmergency.mutate(value, {
                    onError: (error) =>
                      showToast({
                        title: 'Emergency status failed',
                        message: getErrorMessage(error, 'Could not update emergency availability'),
                        variant: 'error',
                      }),
                  })
                }
              />
            </View>
          </View>
        </Card>

        <View style={{ flexDirection: 'row', gap: theme.spacing.sm }}>
          <SummaryTile
            label="Today"
            value={String(todayAppointments.length)}
            caption="appointments"
            onPress={() => navigation.navigate('LawyerAppointmentsTab')}
          />
          <SummaryTile
            label="Messages"
            value={String(unread)}
            caption="unread"
            onPress={() => navigation.navigate('LawyerChatTab')}
          />
        </View>

        {incomingRequest || activeSos ? (
          <Card onPress={() => navigation.navigate('LawyerEmergencyTab')}>
            <Text variant="label" color="emergency">
              {activeSos ? 'Active emergency' : 'Incoming emergency'}
            </Text>
            <Text variant="bodyMedium" style={{ marginTop: 4 }}>
              {activeSos?.categoryName || incomingRequest?.categoryName}
            </Text>
            {(activeSos?.cityName || incomingRequest?.cityName) ? (
              <Text variant="caption" color="secondary">
                {activeSos?.cityName || incomingRequest?.cityName}
              </Text>
            ) : null}
          </Card>
        ) : null}

        <View>
          <SectionHeader
            title="Today's appointments"
            onAction={() => navigation.navigate('LawyerAppointmentsTab')}
          />
          {todayAppointments.length === 0 ? (
            <Text variant="caption" color="secondary">
              No appointments scheduled for today.
            </Text>
          ) : (
            <View style={{ gap: theme.spacing.sm }}>
              {todayAppointments.slice(0, 3).map((item) => (
                <AppointmentCard
                  key={item.id}
                  item={item}
                  perspective="lawyer"
                  onPress={() =>
                    navigation.navigate('LawyerAppointmentsTab', {
                      screen: 'LawyerAppointmentDetail',
                      params: { appointmentId: item.id },
                    })
                  }
                />
              ))}
            </View>
          )}
        </View>

        <Button
          title="Edit availability"
          variant="outline"
          size="sm"
          onPress={() => navigation.navigate('LawyerAvailabilityEdit')}
        />
      </ScrollView>
    </Screen>
  );
}

function SummaryTile({
  label,
  value,
  caption,
  onPress,
}: {
  label: string;
  value: string;
  caption: string;
  onPress: () => void;
}) {
  const theme = useTheme();
  return (
    <View style={{ flex: 1 }}>
      <Card onPress={onPress}>
        <Text variant="caption" color="secondary">
          {label}
        </Text>
        <Text variant="headline" style={{ marginTop: 2 }}>
          {value}
        </Text>
        <Text variant="caption" color="secondary">
          {caption}
        </Text>
      </Card>
    </View>
  );
}
