import React, { useState } from 'react';
import { View } from 'react-native';
import { useQuery } from '@tanstack/react-query';

import { Button } from '@/components/ui/Button';
import { Chip } from '@/components/ui/Chip';
import { Text } from '@/components/ui/Text';
import { TextField } from '@/components/ui/TextField';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { useCreateCaseMutation } from '@/features/cases/hooks/useCaseQueries';
import { lawyersApi } from '@/services/api/lawyersApi';
import type { CasePriority } from '@/types/case';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';

const PRIORITIES: CasePriority[] = ['Low', 'Medium', 'High', 'Urgent'];

export function CreateCaseScreen({
  customerId,
  customerName,
  sourceAppointmentId,
  onCreated,
  onBack,
}: {
  customerId: string;
  customerName: string;
  sourceAppointmentId?: string;
  onCreated: (caseId: string) => void;
  onBack?: () => void;
}) {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const specializations = useQuery({ queryKey: ['specializations'], queryFn: () => lawyersApi.listSpecializations() });
  const createCase = useCreateCaseMutation();

  const [specializationId, setSpecializationId] = useState<string>('');
  const [title, setTitle] = useState('');
  const [caseNumber, setCaseNumber] = useState('');
  const [description, setDescription] = useState('');
  const [courtName, setCourtName] = useState('');
  const [priority, setPriority] = useState<CasePriority>('Medium');

  const canSubmit = title.trim().length > 0 && specializationId.length > 0;

  return (
    <Screen edges={['top', 'left', 'right']} scroll>
      <ScreenHeader title="Create Case" subtitle={`For ${customerName}`} onBack={onBack} />

      <View style={{ gap: theme.spacing.md }}>
        <TextField label="Case title" value={title} onChangeText={setTitle} placeholder="e.g. State vs. Rao — Property Dispute" />
        <TextField label="Case number (optional)" value={caseNumber} onChangeText={setCaseNumber} />

        <View style={{ gap: theme.spacing.xs }}>
          <Text variant="label" color="secondary">Case type</Text>
          <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs }}>
            {specializations.data?.map((s) => (
              <Chip key={s.id} label={s.name} selected={specializationId === s.id} onPress={() => setSpecializationId(s.id)} />
            ))}
          </View>
        </View>

        <TextField label="Court (optional)" value={courtName} onChangeText={setCourtName} />
        <TextField label="Description (optional)" value={description} onChangeText={setDescription} multiline />

        <View style={{ gap: theme.spacing.xs }}>
          <Text variant="label" color="secondary">Priority</Text>
          <View style={{ flexDirection: 'row', gap: theme.spacing.xs }}>
            {PRIORITIES.map((p) => (
              <Chip key={p} label={p} selected={priority === p} onPress={() => setPriority(p)} />
            ))}
          </View>
        </View>

        <Button
          title="Create Case"
          variant="primary"
          disabled={!canSubmit}
          loading={createCase.isPending}
          onPress={() =>
            createCase.mutate(
              {
                customerId,
                specializationId,
                title: title.trim(),
                caseNumber: caseNumber.trim() || undefined,
                description: description.trim() || undefined,
                courtName: courtName.trim() || undefined,
                priority,
                startDate: new Date().toISOString(),
                sourceAppointmentId,
              },
              {
                onSuccess: (created) => {
                  showToast({ title: 'Case created', variant: 'success' });
                  onCreated(created.id);
                },
                onError: (error) =>
                  showToast({
                    title: 'Could not create case',
                    message: getErrorMessage(error, 'You need an appointment relationship with this customer'),
                    variant: 'error',
                  }),
              },
            )
          }
        />
      </View>
    </Screen>
  );
}
