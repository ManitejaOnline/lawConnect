import React from 'react';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { createNativeStackNavigator } from '@react-navigation/native-stack';

import { AppointmentDetailScreen } from '@/features/appointments/screens/AppointmentDetailScreen';
import { AppointmentsListScreen } from '@/features/appointments/screens/AppointmentsListScreen';
import { RescheduleAppointmentScreen } from '@/features/appointments/screens/RescheduleAppointmentScreen';
import { CaseDetailScreen } from '@/features/cases/screens/CaseDetailScreen';
import { CasesListScreen } from '@/features/cases/screens/CasesListScreen';
import { CreateCaseScreen } from '@/features/cases/screens/CreateCaseScreen';
import { LawyerAccountScreen } from '@/features/lawyer/screens/LawyerAccountScreen';
import { LawyerAvailabilityEditScreen } from '@/features/lawyer/screens/LawyerAvailabilityEditScreen';
import { LawyerEmergencyActiveScreen } from '@/features/lawyer/screens/LawyerEmergencyActiveScreen';
import { LawyerEmergencyScreen } from '@/features/lawyer/screens/LawyerEmergencyScreen';
import { LawyerHomeScreen } from '@/features/lawyer/screens/LawyerHomeScreen';
import { useLawyerEmergencyAttention } from '@/features/lawyer/hooks/useLawyerEmergencyAttention';
import { useLawyerWorkspaceRealtime } from '@/features/lawyer/hooks/useLawyerWorkspaceRealtime';
import { ChatStackNavigator } from '@/navigation/ChatStackNavigator';
import { TabIcon } from '@/navigation/TabIcon';
import type {
  LawyerAppointmentsStackParamList,
  LawyerEmergencyStackParamList,
  LawyerHomeStackParamList,
  LawyerProfileStackParamList,
  LawyerTabParamList,
} from '@/navigation/types';
import { useChatStore } from '@/store/chatStore';
import { useTheme } from '@/theme/ThemeProvider';

const Tab = createBottomTabNavigator<LawyerTabParamList>();
const HomeStack = createNativeStackNavigator<LawyerHomeStackParamList>();
const AppointmentsStack = createNativeStackNavigator<LawyerAppointmentsStackParamList>();
const EmergencyStack = createNativeStackNavigator<LawyerEmergencyStackParamList>();
const ProfileStack = createNativeStackNavigator<LawyerProfileStackParamList>();

function stackOptions(background: string) {
  return {
    headerShown: false as const,
    contentStyle: { backgroundColor: background },
    animation: 'slide_from_right' as const,
  };
}

function LawyerHomeStackNavigator() {
  const theme = useTheme();
  return (
    <HomeStack.Navigator screenOptions={stackOptions(theme.colors.background)}>
      <HomeStack.Screen name="LawyerHome" component={LawyerHomeScreen} />
      <HomeStack.Screen name="LawyerAvailabilityEdit" component={LawyerAvailabilityEditScreen} />
    </HomeStack.Navigator>
  );
}

function LawyerAppointmentsStackNavigator() {
  const theme = useTheme();
  return (
    <AppointmentsStack.Navigator screenOptions={stackOptions(theme.colors.background)}>
      <AppointmentsStack.Screen name="LawyerAppointments">
        {({ navigation }) => (
          <AppointmentsListScreen
            perspective="lawyer"
            onOpen={(appointmentId) => navigation.navigate('LawyerAppointmentDetail', { appointmentId })}
            onOpenCases={() => navigation.navigate('LawyerCasesList')}
          />
        )}
      </AppointmentsStack.Screen>
      <AppointmentsStack.Screen name="LawyerAppointmentDetail">
        {({ navigation, route }) => (
          <AppointmentDetailScreen
            appointmentId={route.params.appointmentId}
            perspective="lawyer"
            onBack={() => navigation.goBack()}
            onOpenChat={(args) =>
              navigation.getParent()?.navigate('LawyerChatTab', {
                screen: 'ChatThread',
                params: args,
              })
            }
            onCreateCase={(args) => navigation.navigate('LawyerCreateCase', args)}
            onReschedule={(args) => navigation.navigate('LawyerRescheduleAppointment', args)}
          />
        )}
      </AppointmentsStack.Screen>
      <AppointmentsStack.Screen name="LawyerRescheduleAppointment">
        {({ navigation, route }) => (
          <RescheduleAppointmentScreen
            appointmentId={route.params.appointmentId}
            lawyerId={route.params.lawyerId}
            onBack={() => navigation.goBack()}
          />
        )}
      </AppointmentsStack.Screen>
      <AppointmentsStack.Screen name="LawyerCasesList">
        {({ navigation }) => (
          <CasesListScreen
            perspective="lawyer"
            onOpen={(caseId) => navigation.navigate('LawyerCaseDetail', { caseId })}
            onBack={() => navigation.goBack()}
          />
        )}
      </AppointmentsStack.Screen>
      <AppointmentsStack.Screen name="LawyerCaseDetail">
        {({ navigation, route }) => (
          <CaseDetailScreen caseId={route.params.caseId} perspective="lawyer" onBack={() => navigation.goBack()} />
        )}
      </AppointmentsStack.Screen>
      <AppointmentsStack.Screen name="LawyerCreateCase">
        {({ navigation, route }) => (
          <CreateCaseScreen
            customerId={route.params.customerId}
            customerName={route.params.customerName}
            sourceAppointmentId={route.params.sourceAppointmentId}
            onBack={() => navigation.goBack()}
            onCreated={(caseId) => navigation.replace('LawyerCaseDetail', { caseId })}
          />
        )}
      </AppointmentsStack.Screen>
    </AppointmentsStack.Navigator>
  );
}

function LawyerEmergencyStackNavigator() {
  const theme = useTheme();
  return (
    <EmergencyStack.Navigator screenOptions={stackOptions(theme.colors.background)}>
      <EmergencyStack.Screen name="LawyerEmergencyInbox" component={LawyerEmergencyScreen} />
      <EmergencyStack.Screen name="LawyerEmergencyActive" component={LawyerEmergencyActiveScreen} />
    </EmergencyStack.Navigator>
  );
}

function LawyerProfileStackNavigator() {
  const theme = useTheme();
  return (
    <ProfileStack.Navigator screenOptions={stackOptions(theme.colors.background)}>
      <ProfileStack.Screen name="LawyerAccount" component={LawyerAccountScreen} />
    </ProfileStack.Navigator>
  );
}

export function LawyerTabNavigator() {
  const theme = useTheme();
  useLawyerWorkspaceRealtime();
  const emergencyAttention = useLawyerEmergencyAttention();
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
        name="LawyerHomeTab"
        component={LawyerHomeStackNavigator}
        options={{
          title: 'Home',
          tabBarIcon: ({ color, focused }) => <TabIcon name="home" color={color} focused={focused} />,
        }}
      />
      <Tab.Screen
        name="LawyerAppointmentsTab"
        component={LawyerAppointmentsStackNavigator}
        options={{
          title: 'Appointments',
          tabBarIcon: ({ color, focused }) => <TabIcon name="calendar" color={color} focused={focused} />,
        }}
      />
      <Tab.Screen
        name="LawyerChatTab"
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
        name="LawyerEmergencyTab"
        component={LawyerEmergencyStackNavigator}
        options={{
          title: 'Emergency',
          tabBarIcon: ({ color, focused }) => (
            <TabIcon name="siren" color={color} focused={focused} showDot={emergencyAttention} />
          ),
        }}
      />
      <Tab.Screen
        name="LawyerProfileTab"
        component={LawyerProfileStackNavigator}
        options={{
          title: 'Profile',
          tabBarIcon: ({ color, focused }) => <TabIcon name="user" color={color} focused={focused} />,
        }}
      />
    </Tab.Navigator>
  );
}
