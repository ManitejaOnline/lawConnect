import React from 'react';
import { RefreshControl, ScrollView, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { EmptyState } from '@/components/feedback/EmptyState';
import { ErrorState } from '@/components/feedback/ErrorState';
import { Shimmer } from '@/components/feedback/Shimmer';
import { Card } from '@/components/ui/Card';
import { Rating } from '@/components/ui/Rating';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { useLawyerProfileQuery } from '@/features/discovery/hooks/useDiscoveryQueries';
import type { SearchStackParamList } from '@/navigation/types';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<SearchStackParamList, 'LawyerReviews'>;

/**
 * Reviews screen — backend exposes aggregate rating on profile; per-review list API is not available yet.
 */
export function LawyerReviewsScreen({ navigation, route }: Props) {
  const theme = useTheme();
  const profile = useLawyerProfileQuery(route.params.lawyerId);

  if (profile.isLoading) {
    return (
      <Screen edges={['top', 'left', 'right', 'bottom']}>
        <ScreenHeader title="Reviews" onBack={() => navigation.goBack()} />
        <View style={{ gap: theme.spacing.sm }}>
          <Shimmer height={100} radius={theme.radius.lg} />
          <Shimmer height={72} radius={theme.radius.lg} />
          <Shimmer height={72} radius={theme.radius.lg} />
        </View>
      </Screen>
    );
  }

  if (profile.isError || !profile.data) {
    return (
      <Screen edges={['top', 'left', 'right', 'bottom']}>
        <ScreenHeader title="Reviews" onBack={() => navigation.goBack()} />
        <ErrorState message="Could not load ratings." onRetry={() => profile.refetch()} />
      </Screen>
    );
  }

  const lawyer = profile.data;

  return (
    <Screen edges={['top', 'left', 'right', 'bottom']} padded={false}>
      <ScrollView
        refreshControl={
          <RefreshControl
            refreshing={profile.isRefetching}
            onRefresh={() => profile.refetch()}
            tintColor={theme.colors.accent}
          />
        }
        contentContainerStyle={{
          paddingHorizontal: theme.layout.gutter,
          paddingBottom: theme.spacing['3xl'],
          gap: theme.spacing.md,
        }}
      >
        <ScreenHeader
          title="Reviews"
          subtitle={`${lawyer.firstName} ${lawyer.lastName}`}
          onBack={() => navigation.goBack()}
        />

        <Card>
          <View style={{ alignItems: 'center', gap: theme.spacing.sm, paddingVertical: theme.spacing.sm }}>
            <Text variant="title">{lawyer.ratingAverage.toFixed(1)}</Text>
            <Rating value={lawyer.ratingAverage} size={22} />
            <Text variant="body" color="secondary">
              Based on {lawyer.ratingCount} review{lawyer.ratingCount === 1 ? '' : 's'}
            </Text>
          </View>
        </Card>

        <EmptyState
          variant="inbox"
          title="Detailed reviews coming soon"
          description="Individual review comments are not exposed by the API yet. Aggregate ratings are live from the lawyer profile."
        />
      </ScrollView>
    </Screen>
  );
}
