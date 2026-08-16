import React from 'react';
import { RefreshControl, ScrollView, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { ErrorState } from '@/components/feedback/ErrorState';
import { Shimmer } from '@/components/feedback/Shimmer';
import { Badge } from '@/components/ui/Badge';
import { Card } from '@/components/ui/Card';
import { Rating } from '@/components/ui/Rating';
import { Text } from '@/components/ui/Text';
import { Icon } from '@/components/icons/Icon';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { TrustScoreBreakdownList } from '@/components/lawyer/TrustScoreBreakdownList';
import { useLawyerProfileQuery, useLawyerTrustScoreQuery } from '@/features/discovery/hooks/useDiscoveryQueries';
import type { SearchStackParamList } from '@/navigation/types';
import { useTheme } from '@/theme/ThemeProvider';
import { trustLevelTone } from '@/utils/trustLevel';

type Props = NativeStackScreenProps<SearchStackParamList, 'TrustScoreDetail'>;

export function TrustScoreDetailScreen({ navigation, route }: Props) {
  const theme = useTheme();
  const { lawyerId } = route.params;
  const profile = useLawyerProfileQuery(lawyerId);
  const trustScore = useLawyerTrustScoreQuery(lawyerId);

  const loading = profile.isLoading || trustScore.isLoading;
  const failed = profile.isError || trustScore.isError || !profile.data || !trustScore.data;

  if (loading) {
    return (
      <Screen edges={['top', 'left', 'right', 'bottom']}>
        <ScreenHeader title="Trust Score" onBack={() => navigation.goBack()} />
        <View style={{ gap: theme.spacing.sm }}>
          <Shimmer height={140} radius={theme.radius.lg} />
          <Shimmer height={220} radius={theme.radius.lg} />
        </View>
      </Screen>
    );
  }

  if (failed) {
    return (
      <Screen edges={['top', 'left', 'right', 'bottom']}>
        <ScreenHeader title="Trust Score" onBack={() => navigation.goBack()} />
        <ErrorState
          message="Could not load trust score."
          onRetry={() => {
            void profile.refetch();
            void trustScore.refetch();
          }}
        />
      </Screen>
    );
  }

  const lawyer = profile.data!;
  const score = trustScore.data!;
  const name = `${lawyer.firstName} ${lawyer.lastName}`.trim();

  return (
    <Screen edges={['top', 'left', 'right', 'bottom']} padded={false}>
      <ScrollView
        refreshControl={
          <RefreshControl
            refreshing={trustScore.isRefetching}
            onRefresh={() => trustScore.refetch()}
            tintColor={theme.colors.accent}
          />
        }
        contentContainerStyle={{
          paddingHorizontal: theme.layout.gutter,
          paddingBottom: theme.spacing['3xl'],
          gap: theme.spacing.md,
        }}
      >
        <ScreenHeader title="Trust Score" subtitle={name} onBack={() => navigation.goBack()} />

        <Card>
          <View style={{ alignItems: 'center', gap: theme.spacing.sm, paddingVertical: theme.spacing.sm }}>
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: theme.spacing.xs }}>
              <Icon name="shield" size={28} color={theme.colors.accent} />
              <Text variant="title">{score.score}</Text>
              <Text variant="body" color="secondary">
                /100
              </Text>
            </View>
            <Badge label={score.level} tone={trustLevelTone(score.level)} />
            <Rating value={score.rating} showValue size={16} />
            <Text variant="caption" color="secondary">
              Based on {score.reviewCount} review{score.reviewCount === 1 ? '' : 's'}
            </Text>
          </View>
        </Card>

        <View
          style={{
            backgroundColor: theme.colors.surfaceMuted,
            borderRadius: theme.radius.md,
            padding: theme.spacing.sm,
          }}
        >
          <Text variant="caption" color="secondary">
            This score reflects platform-observed reliability only — client ratings, appointment
            history, responsiveness, and case activity on LawConnect. It is not a prediction of
            case outcomes or a guarantee of quality.
          </Text>
        </View>

        <Card>
          <View style={{ gap: theme.spacing.md }}>
            <Text variant="bodyMedium">Score breakdown</Text>
            <TrustScoreBreakdownList breakdown={score.breakdown} />
          </View>
        </Card>
      </ScrollView>
    </Screen>
  );
}
