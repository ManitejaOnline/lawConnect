import React, { useState } from 'react';
import { Pressable, TextInput, View } from 'react-native';

import { Icon } from '@/components/icons/Icon';
import { Text } from '@/components/ui/Text';
import { BottomSheet } from '@/components/ui/BottomSheet';
import { useTheme } from '@/theme/ThemeProvider';
import { useToastStore } from '@/store/toastStore';

export type ChatComposerProps = {
  sending?: boolean;
  onSend: (text: string) => void;
  onChangeDraft?: (text: string) => void;
  onPickImage: () => void;
  onPickPdf: () => void;
};

export function ChatComposer({
  sending,
  onSend,
  onChangeDraft,
  onPickImage,
  onPickPdf,
}: ChatComposerProps) {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const [text, setText] = useState('');
  const [attachOpen, setAttachOpen] = useState(false);

  const update = (value: string) => {
    setText(value);
    onChangeDraft?.(value);
  };

  const submit = () => {
    const value = text.trim();
    if (!value) return;
    onSend(value);
    update('');
  };

  return (
    <>
      <View
        style={{
          flexDirection: 'row',
          alignItems: 'flex-end',
          gap: theme.spacing.xs,
          paddingHorizontal: theme.layout.gutter,
          paddingTop: theme.spacing.xs,
          paddingBottom: theme.spacing.sm,
          borderTopWidth: 1,
          borderTopColor: theme.colors.divider,
          backgroundColor: theme.colors.backgroundElevated,
        }}
      >
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Attach file"
          onPress={() => setAttachOpen(true)}
          style={iconBtn(theme)}
        >
          <Icon name="paperclip" size={18} color={theme.colors.textPrimary} />
        </Pressable>

        <View
          style={{
            flex: 1,
            minHeight: 40,
            maxHeight: 120,
            borderRadius: theme.radius.lg,
            borderWidth: 1,
            borderColor: theme.colors.borderStrong,
            backgroundColor: theme.colors.surface,
            paddingHorizontal: theme.spacing.sm,
            paddingVertical: theme.spacing.xs,
            justifyContent: 'center',
          }}
        >
          <TextInput
            value={text}
            onChangeText={update}
            placeholder="Message"
            placeholderTextColor={theme.colors.textTertiary}
            multiline
            accessibilityLabel="Message input"
            style={{
              color: theme.colors.textPrimary,
              fontFamily: theme.typography.fontFamily.body,
              fontSize: theme.typography.size.md,
              maxHeight: 100,
              padding: 0,
            }}
          />
        </View>

        {text.trim() ? (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Send message"
            disabled={sending}
            onPress={submit}
            style={[iconBtn(theme), { backgroundColor: theme.colors.accent }]}
          >
            <Icon name="send" size={16} color={theme.colors.textInverse} />
          </Pressable>
        ) : (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Voice message coming soon"
            onPress={() =>
              showToast({
                title: 'Voice messages',
                message: 'Recording will ship in a later release. Placeholder only.',
                variant: 'info',
              })
            }
            style={iconBtn(theme)}
          >
            <Icon name="mic" size={22} color={theme.colors.textPrimary} />
          </Pressable>
        )}
      </View>

      <BottomSheet visible={attachOpen} onClose={() => setAttachOpen(false)} title="Attach">
        <View style={{ gap: theme.spacing.sm }}>
          <AttachRow
            icon="image"
            title="Photo"
            subtitle="JPEG, PNG, or WebP"
            onPress={() => {
              setAttachOpen(false);
              onPickImage();
            }}
          />
          <AttachRow
            icon="file"
            title="PDF document"
            subtitle="Up to 10 MB"
            onPress={() => {
              setAttachOpen(false);
              onPickPdf();
            }}
          />
          <AttachRow
            icon="mic"
            title="Voice message"
            subtitle="Coming soon"
            onPress={() => {
              setAttachOpen(false);
              showToast({
                title: 'Voice messages',
                message: 'Placeholder — recording is not enabled yet.',
                variant: 'info',
              });
            }}
          />
        </View>
      </BottomSheet>
    </>
  );
}

function AttachRow({
  icon,
  title,
  subtitle,
  onPress,
}: {
  icon: 'image' | 'file' | 'mic';
  title: string;
  subtitle: string;
  onPress: () => void;
}) {
  const theme = useTheme();
  return (
    <Pressable
      accessibilityRole="button"
      onPress={onPress}
      style={{
        flexDirection: 'row',
        alignItems: 'center',
        gap: theme.spacing.sm,
        paddingVertical: theme.spacing.sm,
        borderBottomWidth: 1,
        borderBottomColor: theme.colors.divider,
      }}
    >
      <View
        style={{
          width: 44,
          height: 44,
          borderRadius: 22,
          backgroundColor: theme.colors.surfaceMuted,
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Icon name={icon} size={22} color={theme.colors.accent} />
      </View>
      <View style={{ flex: 1 }}>
        <Text variant="bodyMedium">{title}</Text>
        <Text variant="caption" color="secondary">
          {subtitle}
        </Text>
      </View>
    </Pressable>
  );
}

function iconBtn(theme: ReturnType<typeof useTheme>) {
  return {
    width: 40,
    height: 40,
    borderRadius: 20,
    alignItems: 'center' as const,
    justifyContent: 'center' as const,
    backgroundColor: theme.colors.surfaceMuted,
  };
}
