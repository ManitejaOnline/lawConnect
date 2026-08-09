export type AuthStackParamList = {
  Onboarding: undefined;
  RoleSelect: undefined;
  Login: undefined;
  CustomerSignup: undefined;
  LawyerSignup: undefined;
  Phone: undefined;
  Otp: { phoneNumber: string };
};

export type HomeStackParamList = {
  Home: undefined;
  Categories: undefined;
  CategoryLawyers: { specializationId: string; title: string };
  NearbyLawyers: undefined;
  TopRated: undefined;
  Notifications: undefined;
};

export type SearchStackParamList = {
  Search:
    | {
        query?: string;
        specializationId?: string;
        title?: string;
      }
    | undefined;
  Filters: undefined;
  LawyerProfile: { lawyerId: string };
  LawyerReviews: { lawyerId: string };
  LawyerAvailability: { lawyerId: string };
};

export type AppointmentsStackParamList = {
  Appointments: undefined;
  AppointmentDetail: { appointmentId: string };
};

export type ChatStackParamList = {
  Conversations: undefined;
  ChatThread: {
    conversationId: string;
    title: string;
    otherUserId: string;
    otherIsOnline?: boolean;
    otherProfileImageUrl?: string | null;
  };
};

export type ProfileStackParamList = {
  Profile: undefined;
};

export type MainTabParamList = {
  HomeTab: undefined;
  SearchTab:
    | {
        screen?: keyof SearchStackParamList;
        params?: SearchStackParamList[keyof SearchStackParamList];
      }
    | undefined;
  AppointmentsTab: undefined;
  ChatTab:
    | {
        screen?: keyof ChatStackParamList;
        params?: ChatStackParamList[keyof ChatStackParamList];
      }
    | undefined;
  ProfileTab: undefined;
};

export type LawyerHomeStackParamList = {
  LawyerHome: undefined;
  LawyerAvailabilityEdit: undefined;
};

export type LawyerAppointmentsStackParamList = {
  LawyerAppointments: undefined;
  LawyerAppointmentDetail: { appointmentId: string };
};

export type LawyerEmergencyStackParamList = {
  LawyerEmergencyInbox: undefined;
  LawyerEmergencyActive: { requestId: string };
};

export type LawyerProfileStackParamList = {
  LawyerAccount: undefined;
};

export type LawyerTabParamList = {
  LawyerHomeTab: undefined;
  LawyerAppointmentsTab:
    | {
        screen?: keyof LawyerAppointmentsStackParamList;
        params?: LawyerAppointmentsStackParamList[keyof LawyerAppointmentsStackParamList];
      }
    | undefined;
  LawyerChatTab:
    | {
        screen?: keyof ChatStackParamList;
        params?: ChatStackParamList[keyof ChatStackParamList];
      }
    | undefined;
  LawyerEmergencyTab:
    | {
        screen?: keyof LawyerEmergencyStackParamList;
        params?: LawyerEmergencyStackParamList[keyof LawyerEmergencyStackParamList];
      }
    | undefined;
  LawyerProfileTab: undefined;
};

export type RootStackParamList = {
  Auth: undefined;
  Main: undefined;
  LawyerMain:
    | {
        screen?: keyof LawyerTabParamList;
        params?: LawyerTabParamList[keyof LawyerTabParamList];
      }
    | undefined;
  LawyerOnboarding: undefined;
  LawyerPending: undefined;
  AccessBlocked: undefined;
  Emergency:
    | {
        screen?: keyof EmergencyStackParamList;
        params?: EmergencyStackParamList[keyof EmergencyStackParamList];
      }
    | undefined;
};

export type EmergencyStackParamList = {
  EmergencyHome: undefined;
  CategorySelect: undefined;
  LocationConfirm: {
    categoryId: string;
    categoryTitle: string;
    notes?: string;
  };
  LocationPermission: undefined;
  Searching: {
    categoryId: string;
    categoryTitle: string;
    latitude: number;
    longitude: number;
    notes?: string;
    requestId?: string;
  };
  Countdown: undefined;
  LawyerFound: undefined;
  LawyerAccepted: undefined;
  Rating: undefined;
  EmergencyHistory: undefined;
  EmergencyError: {
    kind: import('@/types/emergency').EmergencyErrorKind;
    message: string;
  };
};
