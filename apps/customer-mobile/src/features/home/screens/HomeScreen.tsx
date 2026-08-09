import React, { useCallback, useMemo, useState } from 'react';
import {
  FlatList,
  Pressable,
  RefreshControl,
  ScrollView,
  View,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useQueryClient } from '@tanstack/react-query';

import { CategoryCard } from '@/components/home/CategoryCard';
import { EmergencyBanner } from '@/components/home/EmergencyBanner';
import { HomeSkeleton } from '@/components/home/HomeSkeleton';
import { LawyerCard } from '@/components/home/LawyerCard';
import { SectionHeader } from '@/components/home/SectionHeader';
import { Avatar } from '@/components/ui/Avatar';
import { Badge } from '@/components/ui/Badge';
import { SearchBar } from '@/components/ui/SearchBar';
import { Text } from '@/components/ui/Text';
import { Icon } from '@/components/icons/Icon';
import { Screen } from '@/components/layout/Screen';
import { HOME_PRACTICE_AREA_IDS, PRACTICE_AREA_CATEGORIES } from '@/constants/practiceAreas';
import { useOpenLawyerProfile } from '@/features/discovery/hooks/useOpenLawyerProfile';
import { useUpcomingAppointmentsQuery } from '@/features/appointments/hooks/useAppointmentQueries';
import { AppointmentCard } from '@/features/appointments/components/AppointmentCard';
import {
  homeQueryKeys,
  useFeaturedLawyerQuery,
  useNearbyLawyersQuery,
  useNotificationsQuery,
  useTopRatedLawyersQuery,
  useUserProfileQuery,
} from '@/features/home/hooks/useHomeQueries';
import { useCustomerEmergencyAttention } from '@/features/emergency/hooks/useCustomerEmergencyAttention';
import { emergencyKeys } from '@/features/emergency/hooks/emergencyKeys';
import { useDeviceLocation } from '@/hooks/useDeviceLocation';
import { useLocationLabel } from '@/hooks/useLocationLabel';
import type { HomeStackParamList, MainTabParamList, RootStackParamList } from '@/navigation/types';
import { useRecentLawyersStore } from '@/store/recentLawyersStore';
import { useTheme } from '@/theme/ThemeProvider';
import type { LawyerSearchItem } from '@/types/home';
import type { BottomTabNavigationProp } from '@react-navigation/bottom-tabs';
import type { CompositeNavigationProp } from '@react-navigation/native';

type Nav = CompositeNavigationProp<
  NativeStackNavigationProp<HomeStackParamList, 'Home'>,
  CompositeNavigationProp<
    BottomTabNavigationProp<MainTabParamList>,
    NativeStackNavigationProp<RootStackParamList>
  >
>;

export function HomeScreen() {
  const theme = useTheme();
  const navigation = useNavigation<Nav>();
  const queryClient = useQueryClient();
  const recent = useRecentLawyersStore((s) => s.recent);
  const addRecent = useRecentLawyersStore((s) => s.addRecent);
  const openLawyerProfile = useOpenLawyerProfile();
  const { location, error: locationError } = useDeviceLocation();

  const featured = useFeaturedLawyerQuery();
  const topRated = useTopRatedLawyersQuery(8);
  const nearby = useNearbyLawyersQuery(location, 8);
  const notifications = useNotificationsQuery();
  const profile = useUserProfileQuery();
  const upcoming = useUpcomingAppointmentsQuery();
  const emergencyAttention = useCustomerEmergencyAttention();
  const nextAppointment = upcoming.data?.items?.[0];

  const [refreshing, setRefreshing] = useState(false);
  const [searchDraft, setSearchDraft] = useState('');

  const unreadCount = useMemo(
    () => notifications.data?.items.filter((n) => !n.isRead).length ?? 0,
    [notifications.data],
  );

  const displayName =
    [profile.data?.firstName, profile.data?.lastName].filter(Boolean).join(' ').trim() || 'there';

  const categoriesPreview = HOME_PRACTICE_AREA_IDS.map(
    (id) => PRACTICE_AREA_CATEGORIES.find((item) => item.id === id),
  ).filter((item): item is (typeof PRACTICE_AREA_CATEGORIES)[number] => Boolean(item));
  const locationLabel = useLocationLabel(location);

  const openLawyer = (lawyer: LawyerSearchItem) => {
    addRecent(lawyer);
    openLawyerProfile(lawyer.lawyerId);
  };

  const onRefresh = useCallback(async () => {
    setRefreshing(true);
    try {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: homeQueryKeys.all }),
        queryClient.invalidateQueries({ queryKey: emergencyKeys.customerActive() }),
      ]);
    } finally {
      setRefreshing(false);
    }
  }, [queryClient]);

  const openEmergency = () => {
    navigation.navigate('Emergency');
  };

  const loading =
    featured.isLoading || topRated.isLoading || nearby.isLoading || profile.isLoading;

  return (
    <Screen edges={['top', 'left', 'right']} padded={false}>
      <ScrollView
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={theme.colors.accent} />
        }
        contentContainerStyle={{
          paddingHorizontal: theme.layout.gutter,
          paddingBottom: theme.spacing['3xl'],
          gap: theme.spacing.md,
        }}
        showsVerticalScrollIndicator={false}
        keyboardShouldPersistTaps="handled"
      >
        <View
          style={{
            flexDirection: 'row',
            alignItems: 'center',
            gap: theme.spacing.sm,
            paddingTop: theme.spacing.md,
          }}
        >
          <View style={{ flex: 1, gap: 2 }}>
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: 4 }}>
              <Icon name="map-pin" size={14} color={theme.colors.primary} />
              <Text variant="caption" color="secondary" numberOfLines={1}>
                {locationLabel}
              </Text>
            </View>
            <Text variant="title" numberOfLines={1}>
              Hi, {displayName.split(' ')[0]}
            </Text>
          </View>
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Notifications"
            onPress={() => navigation.navigate('Notifications')}
            style={{
              width: 40,
              height: 40,
              borderRadius: 20,
              backgroundColor: theme.colors.surface,
              borderWidth: 1,
              borderColor: theme.colors.border,
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <Icon name="bell" size={18} color={theme.colors.textPrimary} />
            {unreadCount > 0 ? (
              <View style={{ position: 'absolute', top: 6, right: 6 }}>
                <Badge label={unreadCount > 9 ? '9+' : unreadCount} tone="danger" />
              </View>
            ) : null}
          </Pressable>
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Open profile"
            onPress={() => navigation.navigate('ProfileTab')}
          >
            <Avatar uri={profile.data?.profileImageUrl} name={displayName} size="md" />
          </Pressable>
        </View>

        <SearchBar
          value={searchDraft}
          onChangeText={setSearchDraft}
          placeholder="Search lawyers, practice areas…"
          onSubmit={() =>
            navigation.navigate('SearchTab', {
              screen: 'Search',
              params: { query: searchDraft.trim() || undefined },
            })
          }
          onFilterPress={() =>
            navigation.navigate('SearchTab', {
              screen: 'Filters',
            })
          }
        />

        {loading && !featured.data && !topRated.data ? (
          <HomeSkeleton />
        ) : (
          <>
            <EmergencyBanner onPress={openEmergency} showDot={emergencyAttention} />

            <View>
              <SectionHeader title="Practice areas" onAction={() => navigation.navigate('Categories')} />
              <View style={{ flexDirection: 'row', justifyContent: 'space-between' }}>
                {categoriesPreview.map((item) => (
                  <CategoryCard
                    key={item.id}
                    category={item}
                    onPress={() =>
                      navigation.navigate('CategoryLawyers', {
                        specializationId: item.id,
                        title: item.name,
                      })
                    }
                  />
                ))}
                <CategoryCard
                  category={{ id: 'more', name: 'More', shortName: 'More', icon: 'more' }}
                  onPress={() => navigation.navigate('Categories')}
                />
              </View>
            </View>

            {nextAppointment ? (
              <View>
                <SectionHeader
                  title="Upcoming appointment"
                  onAction={() => navigation.navigate('AppointmentsTab')}
                />
                <AppointmentCard
                  item={nextAppointment}
                  perspective="customer"
                  onPress={() => navigation.navigate('AppointmentsTab')}
                />
              </View>
            ) : null}

            <View>
              <SectionHeader title="Recommended lawyers" onAction={() => navigation.navigate('NearbyLawyers')} />
              {locationError ? (
                <Text variant="caption" color="tertiary" style={{ marginBottom: theme.spacing.xs }}>
                  {locationError}
                </Text>
              ) : null}
              <FlatList
                horizontal
                data={nearby.data?.items ?? []}
                keyExtractor={(item) => item.lawyerId}
                showsHorizontalScrollIndicator={false}
                contentContainerStyle={{ gap: theme.spacing.sm }}
                ListEmptyComponent={
                  nearby.isLoading ? null : (
                    <Text variant="body" color="secondary">
                      No lawyers found nearby yet.
                    </Text>
                  )
                }
                renderItem={({ item }) => (
                  <LawyerCard compact lawyer={item} onPress={() => openLawyer(item)} style={{ width: 280 }} />
                )}
              />
            </View>

            <View>
              <SectionHeader title="Top rated" onAction={() => navigation.navigate('TopRated')} />
              <View style={{ gap: theme.spacing.sm }}>
                {(topRated.data?.items ?? []).slice(0, 5).map((lawyer) => (
                  <LawyerCard key={lawyer.lawyerId} lawyer={lawyer} onPress={() => openLawyer(lawyer)} />
                ))}
              </View>
            </View>

            {recent.length > 0 ? (
              <View>
                <SectionHeader title="Recent" actionLabel="Clear" onAction={() => useRecentLawyersStore.getState().clearRecent()} />
                <FlatList
                  horizontal
                  data={recent}
                  keyExtractor={(item) => item.lawyerId}
                  showsHorizontalScrollIndicator={false}
                  contentContainerStyle={{ gap: theme.spacing.sm }}
                  renderItem={({ item }) => (
                    <LawyerCard compact lawyer={item} onPress={() => openLawyer(item)} style={{ width: 280 }} />
                  )}
                />
              </View>
            ) : null}
          </>
        )}
      </ScrollView>
    </Screen>
  );
}
