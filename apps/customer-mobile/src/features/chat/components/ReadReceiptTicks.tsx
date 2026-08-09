import React from 'react';
import { View } from 'react-native';

import { Icon } from '@/components/icons/Icon';
import { useTheme } from '@/theme/ThemeProvider';
import type { ChatDeliveryState } from '@/types/chat';

export function ReadReceiptTicks({
  state,
  pending,
}: {
  state: ChatDeliveryState;
  pending?: boolean;
}) {
  const theme = useTheme();
  if (pending) {
    return <Icon name="clock" size={14} color={theme.colors.textTertiary} />;
  }
  const color =
    state === 'Read' ? theme.colors.accent : theme.colors.textTertiary;

  return (
    <View style={{ flexDirection: 'row', alignItems: 'center' }} accessibilityLabel={`Message ${state}`}>
      <Icon name={state === 'Sent' ? 'check' : 'checks'} size={14} color={color} />
    </View>
  );
}
