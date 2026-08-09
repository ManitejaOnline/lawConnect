import React from 'react';
import { View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { Button } from '@/components/ui/Button';
import { Rating } from '@/components/ui/Rating';
import { Text } from '@/components/ui/Text';
import { TextField } from '@/components/ui/TextField';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { useEmergencySession } from '@/features/emergency/hooks/useEmergencySession';
import type { EmergencyStackParamList } from '@/navigation/types';
import { useEmergencyStore } from '@/store/emergencyStore';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<EmergencyStackParamList, 'Rating'>;

export function RatingScreen({ navigation }: Props) {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const { teardown } = useEmergencySession();
  const lawyer = useEmergencyStore((s) => s.assignedLawyer);
  const rating = useEmergencyStore((s) => s.rating);
  const setRating = useEmergencyStore((s) => s.setRating);
  const [comment, setComment] = React.useState('');

  const finish = async () => {
    showToast({
      title: rating > 0 ? 'Thanks for your feedback' : 'Session closed',
      message: 'Your emergency history has been updated.',
      variant: 'success',
    });
    await teardown();
    navigation.getParent()?.goBack();
  };

  return (
    <Screen
      edges={['top', 'left', 'right', 'bottom']}
      footer={
        <View style={{ paddingHorizontal: theme.layout.gutter }}>
          <Button title="Done" variant="accent" onPress={finish} />
        </View>
      }
    >
      <View style={{ paddingHorizontal: theme.layout.gutter, gap: theme.spacing.lg, flex: 1 }}>
        <ScreenHeader title="How was the help?" subtitle="Optional feedback" />
        <Text variant="body" color="secondary">
          {lawyer
            ? `Rate ${lawyer.firstName} ${lawyer.lastName} for this emergency session.`
            : 'Rate your emergency session.'}
        </Text>
        <View style={{ alignItems: 'center', gap: theme.spacing.sm }}>
          <Rating value={rating} interactive size={32} onChange={setRating} />
        </View>
        <TextField
          label="Comment"
          placeholder="What helped most?"
          value={comment}
          onChangeText={setComment}
          multiline
        />
      </View>
    </Screen>
  );
}
