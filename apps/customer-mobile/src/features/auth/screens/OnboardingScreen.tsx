import React, { useCallback, useRef, useState } from 'react';
import {
  Dimensions,
  FlatList,
  View,
  type ListRenderItemInfo,
  type NativeScrollEvent,
  type NativeSyntheticEvent,
} from 'react-native';
import Animated, { FadeInRight } from 'react-native-reanimated';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { Button } from '@/components/ui/Button';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { Icon, type IconName } from '@/components/icons/Icon';
import type { AuthStackParamList } from '@/navigation/types';
import { useAuthStore } from '@/store/authStore';
import { useTheme } from '@/theme/ThemeProvider';
import { useResponsiveLayout } from '@/hooks/useResponsiveLayout';

type Props = NativeStackScreenProps<AuthStackParamList, 'Onboarding'>;

type Slide = {
  key: string;
  title: string;
  body: string;
  icon: IconName;
};

const SLIDES: Slide[] = [
  {
    key: 'find',
    title: 'Find the right lawyer',
    body: 'Search verified professionals by practice area, rating, and location — in seconds.',
    icon: 'search',
  },
  {
    key: 'book',
    title: 'Book with confidence',
    body: 'Schedule consultations, chat securely, and keep every detail in one calm workspace.',
    icon: 'calendar',
  },
  {
    key: 'sos',
    title: 'Help when it matters',
    body: 'Emergency SOS connects you to available lawyers nearby when you need urgent support.',
    icon: 'siren',
  },
];

export function OnboardingScreen({ navigation }: Props) {
  const theme = useTheme();
  const { width } = useResponsiveLayout();
  const listRef = useRef<FlatList<Slide>>(null);
  const [index, setIndex] = useState(0);
  const setOnboardingCompleted = useAuthStore((s) => s.setOnboardingCompleted);
  const pageWidth = Math.min(width, theme.layout.maxContentWidth);

  const onScroll = useCallback(
    (e: NativeSyntheticEvent<NativeScrollEvent>) => {
      const next = Math.round(e.nativeEvent.contentOffset.x / Math.max(pageWidth, 1));
      setIndex(next);
    },
    [pageWidth],
  );

  const finish = () => {
    setOnboardingCompleted(true);
    navigation.replace('RoleSelect');
  };

  const goNext = () => {
    if (index >= SLIDES.length - 1) {
      finish();
      return;
    }
    listRef.current?.scrollToIndex({ index: index + 1, animated: true });
  };

  const renderItem = ({ item }: ListRenderItemInfo<Slide>) => (
    <View
      style={{
        width: pageWidth,
        paddingHorizontal: theme.spacing.lg,
        justifyContent: 'center',
        gap: theme.spacing.lg,
      }}
    >
      <Animated.View
        entering={FadeInRight.duration(theme.animation.duration.normal)}
        style={{
          width: 88,
          height: 88,
          borderRadius: theme.radius['2xl'],
          backgroundColor: theme.colors.accentSoft,
          alignItems: 'center',
          justifyContent: 'center',
          ...theme.elevation.sm,
        }}
      >
        <Icon name={item.icon} size={36} color={theme.colors.accent} strokeWidth={1.8} />
      </Animated.View>
      <View style={{ gap: theme.spacing.sm }}>
        <Text variant="title">{item.title}</Text>
        <Text variant="body" color="secondary">
          {item.body}
        </Text>
      </View>
    </View>
  );

  return (
    <Screen
      edges={['top', 'left', 'right', 'bottom']}
      padded={false}
      footer={
        <View style={{ gap: theme.spacing.md, paddingHorizontal: theme.spacing.lg }}>
          <View style={{ flexDirection: 'row', justifyContent: 'center', gap: theme.spacing.xs }}>
            {SLIDES.map((slide, i) => (
              <View
                key={slide.key}
                accessibilityLabel={i === index ? `Slide ${i + 1} of ${SLIDES.length}` : undefined}
                style={{
                  width: i === index ? 22 : 8,
                  height: 8,
                  borderRadius: theme.radius.full,
                  backgroundColor: i === index ? theme.colors.brand : theme.colors.borderStrong,
                }}
              />
            ))}
          </View>
          <Button
            title={index === SLIDES.length - 1 ? 'Get started' : 'Continue'}
            variant="accent"
            onPress={goNext}
            accessibilityHint={index === SLIDES.length - 1 ? 'Choose user or lawyer' : 'Next slide'}
          />
          {index < SLIDES.length - 1 ? (
            <Button title="Skip" variant="ghost" onPress={finish} />
          ) : null}
        </View>
      }
    >
      <View style={{ flex: 1, paddingTop: theme.spacing.xl }}>
        <View style={{ paddingHorizontal: theme.spacing.lg, marginBottom: theme.spacing.md }}>
          <Text variant="label" color="accent">
            LAWCONNECT
          </Text>
        </View>
        <FlatList
          ref={listRef}
          data={SLIDES}
          keyExtractor={(item) => item.key}
          renderItem={renderItem}
          horizontal
          pagingEnabled
          showsHorizontalScrollIndicator={false}
          onScroll={onScroll}
          scrollEventThrottle={16}
          bounces={false}
          getItemLayout={(_, i) => ({ length: pageWidth, offset: pageWidth * i, index: i })}
          style={{ flexGrow: 0, height: Dimensions.get('window').height * 0.52 }}
        />
      </View>
    </Screen>
  );
}
