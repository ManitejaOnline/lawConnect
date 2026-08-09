import React from 'react';
import { View } from 'react-native';

import { Icon, type IconName } from '@/components/icons/Icon';
import { useTheme } from '@/theme/ThemeProvider';

export function TabIcon({
  name,
  focused,
  color,
  showDot = false,
}: {
  name: IconName;
  focused: boolean;
  color: string;
  /** Small red attention dot — only when there is an actionable SOS. Never tints the glyph red. */
  showDot?: boolean;
}) {
  const theme = useTheme();

  return (
    <View style={{ alignItems: 'center', justifyContent: 'center', width: 40, height: 28 }}>
      <Icon name={name} size={focused ? 22 : 20} color={color} />
      {showDot ? (
        <View
          style={{
            position: 'absolute',
            top: 1,
            right: 6,
            width: 7,
            height: 7,
            borderRadius: 4,
            backgroundColor: theme.colors.emergency,
            borderWidth: 1.5,
            borderColor: theme.colors.tabBar,
          }}
        />
      ) : null}
    </View>
  );
}
