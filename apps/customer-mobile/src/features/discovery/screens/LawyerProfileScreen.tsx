import React, { useMemo } from 'react';
import { Image, Pressable, RefreshControl, ScrollView, Share, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { Shimmer } from '@/components/feedback/Shimmer';
import { ErrorState } from '@/components/feedback/ErrorState';
import { IconButton } from '@/components/ui/IconButton';
import { Avatar } from '@/components/ui/Avatar';
import { Badge } from '@/components/ui/Badge';
import { BottomActionBar } from '@/components/ui/BottomActionBar';
import { Button } from '@/components/ui/Button';
import { Rating } from '@/components/ui/Rating';
import { Text } from '@/components/ui/Text';
import { Icon } from '@/components/icons/Icon';
import { Screen } from '@/components/layout/Screen';
import { callLawyerById } from '@/features/calls/openNativeCall';
import { useLawyerProfileQuery, useLawyerTrustScoreQuery } from '@/features/discovery/hooks/useDiscoveryQueries';
import type { SearchStackParamList } from '@/navigation/types';
import { chatApi } from '@/services/api/chatApi';
import { useChatStore } from '@/store/chatStore';
import { useFavoriteLawyersStore } from '@/store/favoriteLawyersStore';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';
import { isProvisionalTrustLevel, trustLevelTone } from '@/utils/trustLevel';

type Props = NativeStackScreenProps<SearchStackParamList, 'LawyerProfile'>;

function formatFee(value: number) {
  return `₹${Math.round(value).toLocaleString('en-IN')}`;
}

function formatTime(value: string) {
  const parts = value.split(':');
  if (parts.length < 2) return value;
  const hours = Number(parts[0]);
  const minutes = parts[1];
  const suffix = hours >= 12 ? 'PM' : 'AM';
  const hour12 = ((hours + 11) % 12) + 1;
  return `${hour12}:${minutes} ${suffix}`;
}

function formatDay(value: string | number) {
  if (typeof value === 'number') {
    return ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'][value] ?? String(value);
  }
  return value;
}

export function LawyerProfileScreen({ navigation, route }: Props) {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const upsertConversation = useChatStore((s) => s.upsertConversation);
  const { lawyerId } = route.params;
  const profile = useLawyerProfileQuery(lawyerId);
  const trustScore = useLawyerTrustScoreQuery(lawyerId);
  const favorite = useFavoriteLawyersStore((s) => s.ids.includes(lawyerId));
  const toggleFavorite = useFavoriteLawyersStore((s) => s.toggle);
  const [messaging, setMessaging] = React.useState(false);
  const [calling, setCalling] = React.useState(false);

  const name = useMemo(() => {
    if (!profile.data) return 'Lawyer';
    return `${profile.data.firstName} ${profile.data.lastName}`.trim();
  }, [profile.data]);

  if (profile.isLoading) {
    return (
      <Screen edges={['top', 'left', 'right', 'bottom']}>
        <View style={{ gap: theme.spacing.md }}>
          <Shimmer height={200} radius={theme.radius.xl} />
          <Shimmer height={24} width="60%" />
          <Shimmer height={80} radius={theme.radius.lg} />
        </View>
      </Screen>
    );
  }

  if (profile.isError || !profile.data) {
    return (
      <Screen edges={['top', 'left', 'right', 'bottom']}>
        <View style={{ flexDirection: 'row', marginBottom: theme.spacing.sm }}>
          <IconButton name="chevron-left" accessibilityLabel="Go back" onPress={() => navigation.goBack()} />
        </View>
        <ErrorState message="Could not load this lawyer." onRetry={() => profile.refetch()} />
      </Screen>
    );
  }

  const lawyer = profile.data;
  const weeklyHours = lawyer.availabilities.filter((a) => a.isAvailable);
  const displayName = name.startsWith('Adv.') ? name : `Adv. ${name}`;
  const primaryArea = lawyer.practiceAreas.find((a) => a.isPrimary)?.name || lawyer.practiceAreas[0]?.name;

  return (
    <Screen
      edges={['top', 'left', 'right']}
      footer={
        <BottomActionBar>
          <Button
            title="Call"
            variant="outline"
            size="sm"
            fullWidth={false}
            loading={calling}
            style={{ flex: 1 }}
            leftSlot={<Icon name="phone" size={14} color={theme.colors.primary} />}
            onPress={async () => {
              setCalling(true);
              try {
                await callLawyerById(lawyer.lawyerId);
              } finally {
                setCalling(false);
              }
            }}
          />
          <Button
            title="Message"
            variant="outline"
            size="sm"
            fullWidth={false}
            loading={messaging}
            style={{ flex: 1 }}
            leftSlot={<Icon name="chat" size={14} color={theme.colors.primary} />}
            onPress={async () => {
              setMessaging(true);
              try {
                const conv = await chatApi.openConversation(lawyer.userId);
                upsertConversation(conv);
                navigation.getParent()?.navigate('ChatTab', {
                  screen: 'ChatThread',
                  params: {
                    conversationId: conv.conversationId,
                    title: name,
                    otherUserId: conv.otherUserId,
                    otherIsOnline: conv.otherIsOnline,
                    otherProfileImageUrl: conv.otherProfileImageUrl,
                  },
                });
              } catch (error) {
                showToast({
                  title: 'Could not open chat',
                  message: getErrorMessage(error),
                  variant: 'error',
                });
              } finally {
                setMessaging(false);
              }
            }}
          />
          <Button
            title="Book Appointment"
            variant="primary"
            size="sm"
            fullWidth={false}
            disabled={!lawyer.isAcceptingAppointments}
            style={{ flex: 1.35 }}
            onPress={() => navigation.navigate('LawyerAvailability', { lawyerId })}
          />
        </BottomActionBar>
      }
    >
      <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: theme.spacing.sm }}>
        <IconButton name="chevron-left" accessibilityLabel="Go back" onPress={() => navigation.goBack()} />
        <View style={{ flexDirection: 'row', gap: theme.spacing.xs }}>
          <IconButton
            name="share"
            accessibilityLabel="Share profile"
            onPress={() =>
              void Share.share({
                message: `${displayName} on LawConnect${primaryArea ? ` · ${primaryArea}` : ''}`,
              })
            }
          />
          <IconButton
            name={favorite ? 'heart-fill' : 'heart'}
            accessibilityLabel={favorite ? 'Remove favorite' : 'Save favorite'}
            color={favorite ? theme.colors.primary : theme.colors.textPrimary}
            onPress={() => toggleFavorite(lawyerId)}
          />
        </View>
      </View>

      <ScrollView
        style={{ flex: 1 }}
        keyboardShouldPersistTaps="handled"
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl
            refreshing={profile.isRefetching}
            onRefresh={() => profile.refetch()}
            tintColor={theme.colors.accent}
          />
        }
        contentContainerStyle={{ gap: theme.spacing.md, paddingBottom: theme.spacing.lg }}
      >
        <View style={{ alignItems: 'center', gap: theme.spacing.xs }}>
          {lawyer.profileImageUrl ? (
            <Image
              source={{ uri: lawyer.profileImageUrl }}
              style={{ width: 88, height: 88, borderRadius: 44 }}
              accessibilityIgnoresInvertColors
            />
          ) : (
            <Avatar name={name} size="lg" />
          )}
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6, flexWrap: 'wrap', justifyContent: 'center' }}>
            <Text variant="headline">{displayName}</Text>
            {lawyer.isVerified ? <Badge label="Verified" tone="success" /> : null}
          </View>
          {primaryArea ? (
            <Text variant="caption" color="secondary">
              {primaryArea}
            </Text>
          ) : null}
          <Rating value={lawyer.ratingAverage} showValue size={14} />
        </View>

        <View
          style={{
            flexDirection: 'row',
            borderWidth: 1,
            borderColor: theme.colors.border,
            borderRadius: theme.radius.lg,
            overflow: 'hidden',
          }}
        >
          <StatCell label="Reviews" value={String(lawyer.ratingCount)} />
          <View style={{ width: 1, backgroundColor: theme.colors.border }} />
          <StatCell label="Experience" value={`${lawyer.yearsOfExperience}+ yrs`} />
          <View style={{ width: 1, backgroundColor: theme.colors.border }} />
          <StatCell label="Fee" value={formatFee(lawyer.consultationFee)} />
        </View>

        {trustScore.data ? (
          <Pressable
            accessibilityRole="button"
            onPress={() => navigation.navigate('TrustScoreDetail', { lawyerId })}
            style={{
              flexDirection: 'row',
              alignItems: 'center',
              gap: theme.spacing.sm,
              borderWidth: 1,
              borderColor: theme.colors.border,
              borderRadius: theme.radius.lg,
              padding: theme.spacing.md,
            }}
          >
            <Icon name="shield" size={22} color={theme.colors.accent} />
            <View style={{ flex: 1, gap: 2 }}>
              <Text variant="bodyMedium">
                {isProvisionalTrustLevel(trustScore.data.level)
                  ? `${trustScore.data.level} · ${trustScore.data.score}/100`
                  : `Trust Score: ${trustScore.data.score}/100`}
              </Text>
              <Text variant="caption" color="secondary">
                {isProvisionalTrustLevel(trustScore.data.level)
                  ? 'Not enough ratings, completed appointments, or cases for an earned Trusted level'
                  : 'Platform-observed reliability — tap for details'}
              </Text>
            </View>
            <Badge label={trustScore.data.level} tone={trustLevelTone(trustScore.data.level)} />
          </Pressable>
        ) : null}

        {lawyer.bio ? (
          <View style={{ gap: 6 }}>
            <Text variant="bodyMedium">About</Text>
            <Text variant="body" color="secondary">
              {lawyer.bio}
            </Text>
          </View>
        ) : null}

        <View style={{ gap: 8 }}>
          <Text variant="bodyMedium">Practice Areas</Text>
          {lawyer.practiceAreas.length === 0 ? (
            <Text variant="caption" color="secondary">
              Not listed
            </Text>
          ) : (
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 6 }}>
              {lawyer.practiceAreas.map((area) => (
                <View
                  key={area.specializationId}
                  style={{
                    borderWidth: 1,
                    borderColor: theme.colors.primary,
                    borderRadius: theme.radius.full,
                    paddingHorizontal: 10,
                    paddingVertical: 4,
                  }}
                >
                  <Text variant="caption" color="accent">
                    {area.name}
                  </Text>
                </View>
              ))}
            </View>
          )}
        </View>

        <View style={{ gap: 8 }}>
          <Text variant="bodyMedium">Languages</Text>
          <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 6 }}>
            {lawyer.languages.length === 0 ? (
              <Text variant="caption" color="secondary">
                Not listed
              </Text>
            ) : (
              lawyer.languages.map((lang) => (
                <Badge
                  key={lang.id}
                  label={lang.proficiency ? `${lang.languageName} (${lang.proficiency})` : lang.languageName}
                  tone="neutral"
                />
              ))
            )}
          </View>
        </View>

        <View style={{ gap: 8 }}>
          <Text variant="bodyMedium">Education</Text>
          {lawyer.educations.length === 0 ? (
            <Text variant="caption" color="secondary">
              No education listed
            </Text>
          ) : (
            lawyer.educations.map((item) => (
              <View key={item.id} style={{ gap: 2 }}>
                <Text variant="bodyMedium">{item.degree}</Text>
                <Text variant="caption" color="secondary">
                  {item.institutionName}
                  {item.fieldOfStudy ? ` · ${item.fieldOfStudy}` : ''}
                </Text>
              </View>
            ))
          )}
        </View>

        <View style={{ gap: 8 }}>
          <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}>
            <Text variant="bodyMedium">Availability</Text>
            <Pressable accessibilityRole="button" onPress={() => navigation.navigate('LawyerAvailability', { lawyerId })}>
              <Text variant="caption" color="link">
                Book
              </Text>
            </Pressable>
          </View>
          {weeklyHours.length === 0 ? (
            <Text variant="caption" color="secondary">
              No weekly hours published
            </Text>
          ) : (
            weeklyHours.map((slot) => (
              <View key={slot.id} style={{ flexDirection: 'row', justifyContent: 'space-between' }}>
                <Text variant="body">{formatDay(slot.dayOfWeek)}</Text>
                <Text variant="caption" color="secondary">
                  {formatTime(slot.startTime)} – {formatTime(slot.endTime)}
                </Text>
              </View>
            ))
          )}
        </View>

        <View style={{ gap: 8 }}>
          <Text variant="bodyMedium">Reviews</Text>
          {lawyer.ratingCount > 0 ? (
            <Pressable accessibilityRole="button" onPress={() => navigation.navigate('LawyerReviews', { lawyerId })}>
              <Rating value={lawyer.ratingAverage} showValue size={16} />
              <Text variant="caption" color="link" style={{ marginTop: 4 }}>
                See all {lawyer.ratingCount} review{lawyer.ratingCount === 1 ? '' : 's'}
              </Text>
            </Pressable>
          ) : (
            <Text variant="caption" color="secondary">
              No reviews yet
            </Text>
          )}
        </View>
      </ScrollView>
    </Screen>
  );
}

function StatCell({ label, value }: { label: string; value: string }) {
  return (
    <View style={{ flex: 1, alignItems: 'center', paddingVertical: 10, gap: 2 }}>
      <Text variant="bodyMedium">{value}</Text>
      <Text variant="caption" color="secondary">
        {label}
      </Text>
    </View>
  );
}
