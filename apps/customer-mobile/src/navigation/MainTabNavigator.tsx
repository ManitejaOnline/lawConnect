import React from 'react';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { createNativeStackNavigator } from '@react-navigation/native-stack';

import { AppointmentsListScreen } from '@/features/appointments/screens/AppointmentsListScreen';
import { AppointmentDetailScreen } from '@/features/appointments/screens/AppointmentDetailScreen';
import { RescheduleAppointmentScreen } from '@/features/appointments/screens/RescheduleAppointmentScreen';
import { CaseDetailScreen } from '@/features/cases/screens/CaseDetailScreen';
import { CasesListScreen } from '@/features/cases/screens/CasesListScreen';
import { CategoriesScreen } from '@/features/home/screens/CategoriesScreen';
import { CategoryLawyersScreen } from '@/features/home/screens/CategoryLawyersScreen';
import { HomeScreen } from '@/features/home/screens/HomeScreen';
import { NearbyLawyersScreen } from '@/features/home/screens/NearbyLawyersScreen';
import { NotificationsScreen } from '@/features/home/screens/NotificationsScreen';
import { TopRatedScreen } from '@/features/home/screens/TopRatedScreen';
import { FiltersScreen } from '@/features/discovery/screens/FiltersScreen';
import { LawyerAvailabilityScreen } from '@/features/discovery/screens/LawyerAvailabilityScreen';
import { LawyerProfileScreen } from '@/features/discovery/screens/LawyerProfileScreen';
import { LawyerReviewsScreen } from '@/features/discovery/screens/LawyerReviewsScreen';
import { SearchScreen } from '@/features/discovery/screens/SearchScreen';
import { TrustScoreDetailScreen } from '@/features/discovery/screens/TrustScoreDetailScreen';
import { EditProfileScreen } from '@/features/profile/screens/EditProfileScreen';
import { ProfileShellScreen } from '@/features/profile/screens/ProfileShellScreen';
import { useChatInboxRealtime } from '@/features/chat/hooks/useConversations';
import { useCustomerEmergencyRealtime } from '@/features/emergency/hooks/useCustomerEmergencyAttention';
import { ChatStackNavigator } from '@/navigation/ChatStackNavigator';
import { TabIcon } from '@/navigation/TabIcon';
import type {
  AppointmentsStackParamList,
  HomeStackParamList,
  MainTabParamList,
  ProfileStackParamList,
  SearchStackParamList,
} from '@/navigation/types';
import { useChatStore } from '@/store/chatStore';
import { useTheme } from '@/theme/ThemeProvider';

const Tab = createBottomTabNavigator<MainTabParamList>();
const HomeStack = createNativeStackNavigator<HomeStackParamList>();
const SearchStack = createNativeStackNavigator<SearchStackParamList>();
const AppointmentsStack = createNativeStackNavigator<AppointmentsStackParamList>();
const ProfileStack = createNativeStackNavigator<ProfileStackParamList>();

function stackScreenOptions(background: string) {
  return {
    headerShown: false as const,
    contentStyle: { backgroundColor: background },
    animation: 'slide_from_right' as const,
  };
}

function HomeStackNavigator() {
  const theme = useTheme();
  return (
    <HomeStack.Navigator screenOptions={stackScreenOptions(theme.colors.background)}>
      <HomeStack.Screen name="Home" component={HomeScreen} />
      <HomeStack.Screen name="Categories" component={CategoriesScreen} />
      <HomeStack.Screen name="CategoryLawyers" component={CategoryLawyersScreen} />
      <HomeStack.Screen name="NearbyLawyers" component={NearbyLawyersScreen} />
      <HomeStack.Screen name="TopRated" component={TopRatedScreen} />
      <HomeStack.Screen name="Notifications" component={NotificationsScreen} />
    </HomeStack.Navigator>
  );
}

function SearchStackNavigator() {
  const theme = useTheme();
  return (
    <SearchStack.Navigator screenOptions={stackScreenOptions(theme.colors.background)}>
      <SearchStack.Screen name="Search" component={SearchScreen} />
      <SearchStack.Screen name="Filters" component={FiltersScreen} />
      <SearchStack.Screen name="LawyerProfile" component={LawyerProfileScreen} />
      <SearchStack.Screen name="LawyerReviews" component={LawyerReviewsScreen} />
      <SearchStack.Screen name="LawyerAvailability" component={LawyerAvailabilityScreen} />
      <SearchStack.Screen name="TrustScoreDetail" component={TrustScoreDetailScreen} />
    </SearchStack.Navigator>
  );
}

function AppointmentsStackNavigator() {
  const theme = useTheme();
  return (
    <AppointmentsStack.Navigator screenOptions={stackScreenOptions(theme.colors.background)}>
      <AppointmentsStack.Screen name="Appointments">
        {({ navigation }) => (
          <AppointmentsListScreen
            perspective="customer"
            onOpen={(appointmentId) => navigation.navigate('AppointmentDetail', { appointmentId })}
            onOpenCases={() => navigation.navigate('CasesList')}
          />
        )}
      </AppointmentsStack.Screen>
      <AppointmentsStack.Screen name="AppointmentDetail">
        {({ navigation, route }) => (
          <AppointmentDetailScreen
            appointmentId={route.params.appointmentId}
            perspective="customer"
            onBack={() => navigation.goBack()}
            onOpenChat={(args) =>
              navigation.getParent()?.navigate('ChatTab', {
                screen: 'ChatThread',
                params: args,
              })
            }
            onReschedule={(args) => navigation.navigate('RescheduleAppointment', args)}
          />
        )}
      </AppointmentsStack.Screen>
      <AppointmentsStack.Screen name="RescheduleAppointment">
        {({ navigation, route }) => (
          <RescheduleAppointmentScreen
            appointmentId={route.params.appointmentId}
            lawyerId={route.params.lawyerId}
            onBack={() => navigation.goBack()}
          />
        )}
      </AppointmentsStack.Screen>
      <AppointmentsStack.Screen name="CasesList">
        {({ navigation }) => (
          <CasesListScreen
            perspective="customer"
            onOpen={(caseId) => navigation.navigate('CaseDetail', { caseId })}
            onBack={() => navigation.goBack()}
          />
        )}
      </AppointmentsStack.Screen>
      <AppointmentsStack.Screen name="CaseDetail">
        {({ navigation, route }) => (
          <CaseDetailScreen caseId={route.params.caseId} perspective="customer" onBack={() => navigation.goBack()} />
        )}
      </AppointmentsStack.Screen>
    </AppointmentsStack.Navigator>
  );
}

function ProfileStackNavigator() {
  const theme = useTheme();
  return (
    <ProfileStack.Navigator screenOptions={stackScreenOptions(theme.colors.background)}>
      <ProfileStack.Screen name="Profile" component={ProfileShellScreen} />
      <ProfileStack.Screen name="EditProfile" component={EditProfileScreen} />
    </ProfileStack.Navigator>
  );
}

export function MainTabNavigator() {
  const theme = useTheme();
  useChatInboxRealtime();
  useCustomerEmergencyRealtime();
  const unread = useChatStore((s) =>
    s.conversations.reduce((sum, c) => sum + (c.unreadCount || 0), 0),
  );

  return (
    <Tab.Navigator
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: theme.colors.tabActive,
        tabBarInactiveTintColor: theme.colors.tabInactive,
        tabBarStyle: {
          height: theme.layout.tabBarHeight + 10,
          paddingTop: 6,
          paddingBottom: 8,
          backgroundColor: theme.colors.tabBar,
          borderTopColor: theme.colors.tabBarBorder,
          borderTopWidth: 1,
        },
        tabBarLabelStyle: {
          fontFamily: theme.typography.fontFamily.bodyMedium,
          fontSize: 11,
          marginTop: 2,
        },
      }}
    >
      <Tab.Screen
        name="HomeTab"
        component={HomeStackNavigator}
        options={{
          title: 'Home',
          tabBarIcon: ({ color, focused }) => <TabIcon name="home" color={color} focused={focused} />,
        }}
      />
      <Tab.Screen
        name="SearchTab"
        component={SearchStackNavigator}
        options={{
          title: 'Search',
          tabBarIcon: ({ color, focused }) => <TabIcon name="search" color={color} focused={focused} />,
        }}
      />
      <Tab.Screen
        name="AppointmentsTab"
        component={AppointmentsStackNavigator}
        options={{
          title: 'Appointments',
          tabBarIcon: ({ color, focused }) => <TabIcon name="calendar" color={color} focused={focused} />,
        }}
      />
      <Tab.Screen
        name="ChatTab"
        component={ChatStackNavigator}
        options={{
          title: 'Messages',
          tabBarBadge: unread > 0 ? (unread > 99 ? '99+' : unread) : undefined,
          tabBarBadgeStyle: {
            backgroundColor: theme.colors.danger,
            color: theme.colors.textInverse,
            fontSize: 10,
          },
          tabBarIcon: ({ color, focused }) => <TabIcon name="chat" color={color} focused={focused} />,
        }}
      />
      <Tab.Screen
        name="ProfileTab"
        component={ProfileStackNavigator}
        options={{
          title: 'Profile',
          tabBarIcon: ({ color, focused }) => <TabIcon name="user" color={color} focused={focused} />,
        }}
      />
    </Tab.Navigator>
  );
}
