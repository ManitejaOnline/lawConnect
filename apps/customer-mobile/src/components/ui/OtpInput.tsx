import React, { useMemo, useRef } from 'react';
import {
  Pressable,
  TextInput,
  View,
  type StyleProp,
  type ViewStyle,
} from 'react-native';

import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export type OtpInputProps = {
  value: string;
  onChange: (value: string) => void;
  length?: number;
  error?: string;
  autoFocus?: boolean;
  editable?: boolean;
  style?: StyleProp<ViewStyle>;
};

/**
 * Accessible OTP entry — single hidden field + visual digit cells.
 */
export function OtpInput({
  value,
  onChange,
  length = 6,
  error,
  autoFocus = true,
  editable = true,
  style,
}: OtpInputProps) {
  const theme = useTheme();
  const inputRef = useRef<TextInput>(null);
  const digits = useMemo(() => {
    const cleaned = value.replace(/\D/g, '').slice(0, length);
    return Array.from({ length }, (_, i) => cleaned[i] ?? '');
  }, [length, value]);

  const focusedIndex = Math.min(value.replace(/\D/g, '').length, length - 1);

  return (
    <View style={[{ gap: theme.spacing.xs }, style]}>
      <Pressable
        accessibilityRole="none"
        onPress={() => inputRef.current?.focus()}
        style={{ flexDirection: 'row', gap: theme.spacing.xs, justifyContent: 'space-between' }}
      >
        {digits.map((digit, index) => {
          const active = editable && index === focusedIndex;
          return (
            <View
              key={index}
              accessibilityElementsHidden
              importantForAccessibility="no-hide-descendants"
              style={{
                flex: 1,
                maxWidth: 56,
                height: 56,
                borderRadius: theme.radius.md,
                borderWidth: 1.5,
                borderColor: error
                  ? theme.colors.danger
                  : active
                    ? theme.colors.focusRing
                    : theme.colors.borderStrong,
                backgroundColor: theme.colors.surface,
                alignItems: 'center',
                justifyContent: 'center',
                ...theme.elevation.sm,
              }}
            >
              <Text variant="headline">{digit}</Text>
            </View>
          );
        })}
      </Pressable>

      <TextInput
        ref={inputRef}
        value={value.replace(/\D/g, '').slice(0, length)}
        onChangeText={(text) => onChange(text.replace(/\D/g, '').slice(0, length))}
        keyboardType="number-pad"
        textContentType="oneTimeCode"
        autoComplete="sms-otp"
        importantForAutofill="yes"
        maxLength={length}
        autoFocus={autoFocus}
        editable={editable}
        caretHidden
        accessibilityLabel={`One-time code, ${length} digits`}
        style={{
          position: 'absolute',
          opacity: 0.02,
          height: 56,
          width: '100%',
          color: theme.colors.textPrimary,
        }}
      />

      {error ? (
        <Text variant="caption" color="danger">
          {error}
        </Text>
      ) : null}
    </View>
  );
}
