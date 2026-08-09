import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';

import { ConversationsScreen } from '@/features/chat/screens/ConversationsScreen';
import { ChatThreadScreen } from '@/features/chat/screens/ChatThreadScreen';
import type { ChatStackParamList } from '@/navigation/types';
import { useTheme } from '@/theme/ThemeProvider';

const ChatStack = createNativeStackNavigator<ChatStackParamList>();

export function ChatStackNavigator() {
  const theme = useTheme();
  return (
    <ChatStack.Navigator
      screenOptions={{
        headerShown: false,
        contentStyle: { backgroundColor: theme.colors.background },
        animation: 'slide_from_right',
      }}
    >
      <ChatStack.Screen name="Conversations" component={ConversationsScreen} />
      <ChatStack.Screen name="ChatThread" component={ChatThreadScreen} />
    </ChatStack.Navigator>
  );
}
