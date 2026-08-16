import React from 'react';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { createNativeStackNavigator } from '@react-navigation/native-stack';

import { AdminAccountScreen } from '@/features/admin/screens/AdminAccountScreen';
import { AdminCaseDetailScreen } from '@/features/admin/screens/AdminCaseDetailScreen';
import { AdminCasesListScreen } from '@/features/admin/screens/AdminCasesListScreen';
import { AdminComplaintDetailScreen } from '@/features/admin/screens/AdminComplaintDetailScreen';
import { AdminComplaintsListScreen } from '@/features/admin/screens/AdminComplaintsListScreen';
import { AdminDashboardScreen } from '@/features/admin/screens/AdminDashboardScreen';
import { AdminLawyerDetailScreen } from '@/features/admin/screens/AdminLawyerDetailScreen';
import { AdminLawyersListScreen } from '@/features/admin/screens/AdminLawyersListScreen';
import { AWAITING_DECISION_STATUSES } from '@/features/admin/statusTone';
import { TabIcon } from '@/navigation/TabIcon';
import type {
  AdminCasesStackParamList,
  AdminComplaintsStackParamList,
  AdminDashboardStackParamList,
  AdminLawyersStackParamList,
  AdminTabParamList,
} from '@/navigation/types';
import { useTheme } from '@/theme/ThemeProvider';

const Tab = createBottomTabNavigator<AdminTabParamList>();
const DashboardStack = createNativeStackNavigator<AdminDashboardStackParamList>();
const PendingStack = createNativeStackNavigator<AdminLawyersStackParamList>();
const ApprovedStack = createNativeStackNavigator<AdminLawyersStackParamList>();
const RejectedStack = createNativeStackNavigator<AdminLawyersStackParamList>();
const ComplaintsStack = createNativeStackNavigator<AdminComplaintsStackParamList>();
const CasesStack = createNativeStackNavigator<AdminCasesStackParamList>();

function stackOptions(background: string) {
  return {
    headerShown: false as const,
    contentStyle: { backgroundColor: background },
    animation: 'slide_from_right' as const,
  };
}

function AdminDashboardStackNavigator() {
  const theme = useTheme();
  return (
    <DashboardStack.Navigator screenOptions={stackOptions(theme.colors.background)}>
      <DashboardStack.Screen name="AdminDashboard" component={AdminDashboardScreen} />
      <DashboardStack.Screen name="AdminLawyerDetail" component={AdminLawyerDetailScreen} />
    </DashboardStack.Navigator>
  );
}

function AdminPendingStackNavigator() {
  const theme = useTheme();
  return (
    <PendingStack.Navigator screenOptions={stackOptions(theme.colors.background)}>
      <PendingStack.Screen name="AdminLawyersList">
        {() => <AdminLawyersListScreen title="Pending Lawyers" status={AWAITING_DECISION_STATUSES} />}
      </PendingStack.Screen>
      <PendingStack.Screen name="AdminLawyerDetail" component={AdminLawyerDetailScreen} />
    </PendingStack.Navigator>
  );
}

function AdminApprovedStackNavigator() {
  const theme = useTheme();
  return (
    <ApprovedStack.Navigator screenOptions={stackOptions(theme.colors.background)}>
      <ApprovedStack.Screen name="AdminLawyersList">
        {() => <AdminLawyersListScreen title="Approved Lawyers" status="Approved" />}
      </ApprovedStack.Screen>
      <ApprovedStack.Screen name="AdminLawyerDetail" component={AdminLawyerDetailScreen} />
    </ApprovedStack.Navigator>
  );
}

function AdminRejectedStackNavigator() {
  const theme = useTheme();
  return (
    <RejectedStack.Navigator screenOptions={stackOptions(theme.colors.background)}>
      <RejectedStack.Screen name="AdminLawyersList">
        {() => <AdminLawyersListScreen title="Rejected Lawyers" status="Rejected" />}
      </RejectedStack.Screen>
      <RejectedStack.Screen name="AdminLawyerDetail" component={AdminLawyerDetailScreen} />
    </RejectedStack.Navigator>
  );
}

function AdminComplaintsStackNavigator() {
  const theme = useTheme();
  return (
    <ComplaintsStack.Navigator screenOptions={stackOptions(theme.colors.background)}>
      <ComplaintsStack.Screen name="AdminComplaintsList" component={AdminComplaintsListScreen} />
      <ComplaintsStack.Screen name="AdminComplaintDetail" component={AdminComplaintDetailScreen} />
    </ComplaintsStack.Navigator>
  );
}

function AdminCasesStackNavigator() {
  const theme = useTheme();
  return (
    <CasesStack.Navigator screenOptions={stackOptions(theme.colors.background)}>
      <CasesStack.Screen name="AdminCasesList" component={AdminCasesListScreen} />
      <CasesStack.Screen name="AdminCaseDetail" component={AdminCaseDetailScreen} />
    </CasesStack.Navigator>
  );
}

export function AdminTabNavigator() {
  const theme = useTheme();

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
        name="AdminDashboardTab"
        component={AdminDashboardStackNavigator}
        options={{
          title: 'Dashboard',
          tabBarIcon: ({ color, focused }) => <TabIcon name="home" color={color} focused={focused} />,
        }}
      />
      <Tab.Screen
        name="AdminPendingTab"
        component={AdminPendingStackNavigator}
        options={{
          title: 'Pending',
          tabBarIcon: ({ color, focused }) => <TabIcon name="clock" color={color} focused={focused} />,
        }}
      />
      <Tab.Screen
        name="AdminApprovedTab"
        component={AdminApprovedStackNavigator}
        options={{
          title: 'Approved',
          tabBarIcon: ({ color, focused }) => <TabIcon name="checks" color={color} focused={focused} />,
        }}
      />
      <Tab.Screen
        name="AdminRejectedTab"
        component={AdminRejectedStackNavigator}
        options={{
          title: 'Rejected',
          tabBarIcon: ({ color, focused }) => <TabIcon name="close" color={color} focused={focused} />,
        }}
      />
      <Tab.Screen
        name="AdminComplaintsTab"
        component={AdminComplaintsStackNavigator}
        options={{
          title: 'Complaints',
          tabBarIcon: ({ color, focused }) => <TabIcon name="alert" color={color} focused={focused} />,
        }}
      />
      <Tab.Screen
        name="AdminCasesTab"
        component={AdminCasesStackNavigator}
        options={{
          title: 'Cases',
          tabBarIcon: ({ color, focused }) => <TabIcon name="briefcase" color={color} focused={focused} />,
        }}
      />
      <Tab.Screen
        name="AdminAccountTab"
        component={AdminAccountScreen}
        options={{
          title: 'Account',
          tabBarIcon: ({ color, focused }) => <TabIcon name="user" color={color} focused={focused} />,
        }}
      />
    </Tab.Navigator>
  );
}
