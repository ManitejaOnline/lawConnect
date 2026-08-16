import React, { useEffect, useMemo, useState } from 'react';
import { Pressable, View } from 'react-native';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import * as DocumentPicker from 'expo-document-picker';
import * as ImagePicker from 'expo-image-picker';

import { Avatar } from '@/components/ui/Avatar';
import { Button } from '@/components/ui/Button';
import { Chip } from '@/components/ui/Chip';
import { Text } from '@/components/ui/Text';
import { TextField } from '@/components/ui/TextField';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { PRACTICE_AREA_CATEGORIES } from '@/constants/practiceAreas';
import { OnboardingProgress } from '@/features/lawyer/components/OnboardingProgress';
import { lawyerKeys } from '@/features/lawyer/hooks/useLawyerQueries';
import { authApi } from '@/services/api/authApi';
import { lawyersApi } from '@/services/api/lawyersApi';
import { usersApi } from '@/services/api/usersApi';
import { useAuthStore } from '@/store/authStore';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';

const WEEKDAYS = [
  { label: 'Mon', flag: 1, name: 'monday' },
  { label: 'Tue', flag: 2, name: 'tuesday' },
  { label: 'Wed', flag: 3, name: 'wednesday' },
  { label: 'Thu', flag: 4, name: 'thursday' },
  { label: 'Fri', flag: 5, name: 'friday' },
  { label: 'Sat', flag: 6, name: 'saturday' },
  { label: 'Sun', flag: 0, name: 'sunday' },
] as const;

function dayFlag(value: string | number): number | null {
  if (typeof value === 'number' || /^\d+$/.test(String(value))) {
    const numeric = Number(value);
    return Number.isNaN(numeric) ? null : numeric;
  }
  return WEEKDAYS.find((day) => day.name === String(value).toLowerCase())?.flag ?? null;
}

const DOCUMENT_TYPE_VALUES: Record<string, number> = {
  BarCouncilCertificate: 0,
  GovernmentId: 1,
  DegreeCertificate: 2,
  ProfilePhoto: 3,
  CertificateOfPractice: 4,
  Other: 99,
};

const DOCUMENT_SLOTS = [
  {
    type: 'BarCouncilCertificate',
    label: 'Bar Council enrollment',
    required: true,
    hint: 'Enrollment / registration certificate',
  },
  {
    type: 'CertificateOfPractice',
    label: 'Certificate of Practice',
    required: false,
    hint: 'Upload if applicable for your council',
  },
  {
    type: 'GovernmentId',
    label: 'Government identity',
    required: true,
    hint: 'Aadhaar, passport, or other government ID',
  },
] as const;

type Draft = {
  stateBarCouncil: string;
  barCouncilNumber: string;
  specializationIds: string[];
  yearsOfExperience: string;
  languages: string;
  education: string;
  consultationFee: string;
  cityName: string;
  stateName: string;
  countryName: string;
  bio: string;
  availabilityFlags: number[];
  photoUri: string | null;
};

const emptyDraft: Draft = {
  stateBarCouncil: '',
  barCouncilNumber: '',
  specializationIds: [],
  yearsOfExperience: '',
  languages: '',
  education: '',
  consultationFee: '',
  cityName: '',
  stateName: 'Karnataka',
  countryName: 'India',
  bio: '',
  availabilityFlags: [1, 2, 3, 4, 5],
  photoUri: null,
};

export function LawyerOnboardingScreen() {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const queryClient = useQueryClient();
  const userId = useAuthStore((s) => s.session?.userId);
  const profileName = useAuthStore((s) => `${s.profile?.firstName ?? ''} ${s.profile?.lastName ?? ''}`.trim());
  const [step, setStep] = useState(2);
  const [draft, setDraft] = useState<Draft>(emptyDraft);
  const [hydrated, setHydrated] = useState(false);

  const mine = useQuery({
    queryKey: lawyerKeys.me(userId),
    queryFn: lawyersApi.getMine,
    enabled: Boolean(userId),
  });

  const specializations = useQuery({
    queryKey: ['lawyers', 'specializations'],
    queryFn: lawyersApi.listSpecializations,
  });

  const documents = useQuery({
    queryKey: [...lawyerKeys.me(userId), 'documents'],
    queryFn: lawyersApi.listDocuments,
    enabled: Boolean(userId),
  });

  useEffect(() => {
    if (hydrated || !mine.data) return;
    const profile = mine.data;
    setDraft((current) => ({
      ...current,
      stateBarCouncil: profile.stateBarCouncil ?? current.stateBarCouncil,
      barCouncilNumber: profile.barCouncilNumber ?? current.barCouncilNumber,
      specializationIds:
        profile.practiceAreas?.map((area) => area.specializationId) ?? current.specializationIds,
      yearsOfExperience:
        profile.yearsOfExperience > 0 ? String(profile.yearsOfExperience) : current.yearsOfExperience,
      languages: profile.languages?.map((row) => row.languageName).join(', ') || current.languages,
      education: profile.educations?.[0]?.institutionName ?? current.education,
      consultationFee: profile.consultationFee > 0 ? String(profile.consultationFee) : current.consultationFee,
      cityName: profile.cityName ?? current.cityName,
      stateName: profile.stateName ?? current.stateName,
      countryName: profile.countryName ?? current.countryName,
      bio: profile.bio ?? current.bio,
      photoUri: profile.profileImageUrl ?? current.photoUri,
      availabilityFlags:
        profile.availabilities?.length > 0
          ? profile.availabilities
              .map((slot) => dayFlag(slot.dayOfWeek))
              .filter((flag): flag is number => flag != null)
          : current.availabilityFlags,
    }));
    setHydrated(true);
  }, [hydrated, mine.data]);

  const practiceOptions = useMemo(() => {
    if (specializations.data?.length) {
      return specializations.data.map((row) => ({ id: row.id, name: row.name }));
    }
    return PRACTICE_AREA_CATEGORIES.map((row) => ({ id: row.id, name: row.shortName }));
  }, [specializations.data]);

  const uploadedTypes = (documents.data ?? []).map((doc) => String(doc.documentType).toLowerCase());
  const hasDocument = (type: string) =>
    uploadedTypes.includes(type.toLowerCase()) || uploadedTypes.includes(String(DOCUMENT_TYPE_VALUES[type]));

  const save = useMutation({
    mutationFn: () =>
      lawyersApi.saveApplication({
        stateBarCouncil: draft.stateBarCouncil.trim(),
        barCouncilNumber: draft.barCouncilNumber.trim(),
        specializationIds: draft.specializationIds,
        yearsOfExperience: Number(draft.yearsOfExperience || 0),
        languages: draft.languages.trim(),
        education: draft.education.trim(),
        consultationFee: Number(draft.consultationFee || 0),
        cityName: draft.cityName.trim(),
        stateName: draft.stateName.trim(),
        countryName: draft.countryName.trim(),
        bio: draft.bio.trim(),
        availability: draft.availabilityFlags.map((dayOfWeek) => ({
          dayOfWeek,
          startTime: '10:00:00',
          endTime: '18:00:00',
          isAvailable: true,
          timeZoneId: 'Asia/Kolkata',
        })),
      }),
    onSuccess: (profile) => {
      queryClient.setQueryData(lawyerKeys.me(userId), profile);
    },
  });

  const submit = useMutation({
    mutationFn: lawyersApi.submitApplication,
    onSuccess: (profile) => {
      queryClient.setQueryData(lawyerKeys.me(userId), profile);
      showToast({ title: 'Application submitted', variant: 'success' });
    },
    onError: (error) => {
      showToast({
        title: 'Could not submit',
        message: getErrorMessage(error, 'Complete required fields and documents first'),
        variant: 'error',
      });
    },
  });

  const uploadDoc = useMutation({
    mutationFn: async (documentType: string) => {
      const picked = await DocumentPicker.getDocumentAsync({
        type: ['application/pdf', 'image/jpeg', 'image/png'],
        copyToCacheDirectory: true,
      });
      if (picked.canceled || !picked.assets?.[0]) return null;
      const file = picked.assets[0];
      return lawyersApi.uploadDocument(documentType, {
        uri: file.uri,
        name: file.name || `${documentType}.pdf`,
        mimeType: file.mimeType || 'application/pdf',
        file: file.file,
      });
    },
    onSuccess: async (result) => {
      if (!result) return;
      await documents.refetch();
      showToast({ title: 'Document uploaded', variant: 'success' });
    },
    onError: (error) => {
      showToast({
        title: 'Upload failed',
        message: getErrorMessage(error, 'Could not upload document'),
        variant: 'error',
      });
    },
  });

  const uploadPhoto = useMutation({
    mutationFn: async () => {
      const permission = await ImagePicker.requestMediaLibraryPermissionsAsync();
      if (!permission.granted) {
        throw new Error('Photo library permission is required.');
      }
      const picked = await ImagePicker.launchImageLibraryAsync({
        mediaTypes: ['images'],
        quality: 0.8,
      });
      if (picked.canceled || !picked.assets?.[0]) return null;
      const file = picked.assets[0];
      const name = file.fileName || 'profile.jpg';
      const mimeType = file.mimeType || 'image/jpeg';
      const profile = await usersApi.uploadProfilePicture({ uri: file.uri, name, mimeType, file: file.file });
      return profile.profileImageUrl ?? file.uri;
    },
    onSuccess: (uri) => {
      if (!uri) return;
      setDraft((current) => ({ ...current, photoUri: uri }));
      showToast({ title: 'Photo updated', variant: 'success' });
    },
    onError: (error) => {
      showToast({
        title: 'Photo failed',
        message: getErrorMessage(error, 'Could not upload photo'),
        variant: 'error',
      });
    },
  });

  const logout = useMutation({ mutationFn: () => authApi.logout() });

  const patch = <K extends keyof Draft>(key: K, value: Draft[K]) => {
    setDraft((current) => ({ ...current, [key]: value }));
  };

  const toggleSpecialization = (id: string) => {
    setDraft((current) => ({
      ...current,
      specializationIds: current.specializationIds.includes(id)
        ? current.specializationIds.filter((value) => value !== id)
        : [...current.specializationIds, id],
    }));
  };

  const toggleDay = (flag: number) => {
    setDraft((current) => ({
      ...current,
      availabilityFlags: current.availabilityFlags.includes(flag)
        ? current.availabilityFlags.filter((value) => value !== flag)
        : [...current.availabilityFlags, flag],
    }));
  };

  const validateStep = (target: number): string | null => {
    if (target === 2) {
      if (!draft.stateBarCouncil.trim()) return 'Enter your State Bar Council.';
      if (!draft.barCouncilNumber.trim()) return 'Enter your enrollment / registration number.';
      if (draft.specializationIds.length === 0) return 'Select at least one practice area.';
      if (draft.yearsOfExperience === '' || Number.isNaN(Number(draft.yearsOfExperience))) {
        return 'Enter years of experience.';
      }
      if (!draft.languages.trim()) return 'Enter languages.';
      if (!draft.education.trim()) return 'Enter education.';
    }
    if (target === 3) {
      const missing = DOCUMENT_SLOTS.filter((slot) => slot.required && !hasDocument(slot.type));
      if (missing.length > 0) return `Upload: ${missing.map((slot) => slot.label).join(', ')}.`;
    }
    if (target === 4) {
      if (draft.consultationFee === '' || Number(draft.consultationFee) < 0) return 'Enter a consultation fee.';
      if (!draft.cityName.trim() || !draft.stateName.trim() || !draft.countryName.trim()) {
        return 'Enter city, state, and country.';
      }
      if (!draft.bio.trim()) return 'Add a short about section.';
      if (draft.availabilityFlags.length === 0) return 'Select at least one available day.';
    }
    return null;
  };

  const goNext = async () => {
    const message = validateStep(step);
    if (message) {
      showToast({ title: 'Complete this step', message, variant: 'warning' });
      return;
    }
    if (step === 2 || step === 4) {
      try {
        await save.mutateAsync();
      } catch (error) {
        showToast({
          title: 'Could not save',
          message: getErrorMessage(error, 'Try again'),
          variant: 'error',
        });
        return;
      }
    }
    if (step < 5) {
      setStep((value) => value + 1);
      return;
    }
    try {
      await save.mutateAsync();
      await submit.mutateAsync();
    } catch {
      // Toast handled by mutations / save catch above.
    }
  };

  const footerTitle = step === 5 ? 'Submit application' : 'Continue';
  const busy = save.isPending || submit.isPending || uploadDoc.isPending || uploadPhoto.isPending;

  return (
    <Screen
      edges={['top', 'left', 'right']}
      scroll
      keyboard
      footer={
        <View style={{ gap: theme.spacing.sm }}>
          <Button title={footerTitle} variant="accent" loading={busy} disabled={busy} onPress={() => void goNext()} />
          {step > 2 ? (
            <Button title="Back" variant="ghost" disabled={busy} onPress={() => setStep((value) => value - 1)} />
          ) : (
            <Button title="Sign out" variant="ghost" loading={logout.isPending} onPress={() => logout.mutate()} />
          )}
        </View>
      }
    >
      <ScreenHeader
        title={step === 2 ? 'Professional information' : step === 3 ? 'Documents' : step === 4 ? 'Consultation profile' : 'Review & submit'}
        subtitle={mine.data?.reviewReason ? `Requested changes: ${mine.data.reviewReason}` : 'Complete verification to go live'}
      />
      <OnboardingProgress current={step} />

      {step === 2 ? (
        <View style={{ gap: theme.spacing.md }}>
          <TextField
            label="State Bar Council"
            value={draft.stateBarCouncil}
            onChangeText={(value) => patch('stateBarCouncil', value)}
            placeholder="Karnataka State Bar Council"
          />
          <TextField
            label="Enrollment / registration number"
            value={draft.barCouncilNumber}
            onChangeText={(value) => patch('barCouncilNumber', value)}
          />
          <View style={{ gap: theme.spacing.xs }}>
            <Text variant="label" color="secondary">
              Practice areas
            </Text>
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs }}>
              {practiceOptions.map((area) => (
                <Chip
                  key={area.id}
                  label={area.name}
                  selected={draft.specializationIds.includes(area.id)}
                  onPress={() => toggleSpecialization(area.id)}
                />
              ))}
            </View>
          </View>
          <TextField
            label="Years of experience"
            value={draft.yearsOfExperience}
            onChangeText={(value) => patch('yearsOfExperience', value.replace(/[^\d]/g, ''))}
            keyboardType="number-pad"
          />
          <TextField
            label="Languages"
            value={draft.languages}
            onChangeText={(value) => patch('languages', value)}
            placeholder="English, Hindi, Kannada"
          />
          <TextField
            label="Education"
            value={draft.education}
            onChangeText={(value) => patch('education', value)}
            placeholder="NLSIU Bangalore"
          />
        </View>
      ) : null}

      {step === 3 ? (
        <View style={{ gap: theme.spacing.md }}>
          <Text variant="caption" color="secondary">
            Documents are stored privately and reviewed by admin. Certificate of Practice is optional unless your verification policy requires it.
          </Text>
          {DOCUMENT_SLOTS.map((slot) => {
            const uploaded = hasDocument(slot.type);
            return (
              <Pressable
                key={slot.type}
                onPress={() => uploadDoc.mutate(slot.type)}
                style={{
                  borderWidth: 1,
                  borderColor: uploaded ? theme.colors.accent : theme.colors.border,
                  borderRadius: theme.radius.lg,
                  padding: theme.spacing.md,
                  gap: 4,
                  backgroundColor: theme.colors.surface,
                }}
              >
                <Text variant="bodyMedium">
                  {slot.label}
                  {slot.required ? '' : ' (optional)'}
                </Text>
                <Text variant="caption" color="secondary">
                  {uploaded ? 'Uploaded' : slot.hint}
                </Text>
              </Pressable>
            );
          })}
        </View>
      ) : null}

      {step === 4 ? (
        <View style={{ gap: theme.spacing.md }}>
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: theme.spacing.md }}>
            <Avatar name={profileName || 'Lawyer'} uri={draft.photoUri} size="lg" />
            <Button
              title={draft.photoUri ? 'Change photo' : 'Add profile photo'}
              variant="outline"
              fullWidth={false}
              loading={uploadPhoto.isPending}
              onPress={() => uploadPhoto.mutate()}
            />
          </View>
          <TextField
            label="Consultation fee (INR)"
            value={draft.consultationFee}
            onChangeText={(value) => patch('consultationFee', value.replace(/[^\d.]/g, ''))}
            keyboardType="decimal-pad"
          />
          <TextField label="City" value={draft.cityName} onChangeText={(value) => patch('cityName', value)} />
          <TextField label="State" value={draft.stateName} onChangeText={(value) => patch('stateName', value)} />
          <TextField label="Country" value={draft.countryName} onChangeText={(value) => patch('countryName', value)} />
          <TextField
            label="About"
            value={draft.bio}
            onChangeText={(value) => patch('bio', value)}
            multiline
            numberOfLines={4}
            textAlignVertical="top"
          />
          <View style={{ gap: theme.spacing.xs }}>
            <Text variant="label" color="secondary">
              Availability (10:00–18:00)
            </Text>
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs }}>
              {WEEKDAYS.map((day) => (
                <Chip
                  key={day.flag}
                  label={day.label}
                  selected={draft.availabilityFlags.includes(day.flag)}
                  onPress={() => toggleDay(day.flag)}
                />
              ))}
            </View>
          </View>
        </View>
      ) : null}

      {step === 5 ? (
        <View style={{ gap: theme.spacing.md }}>
          <Text variant="body" color="secondary">
            Your application will be submitted for verification. You will not appear as a verified lawyer, accept SOS, or go online until an admin approves your account.
          </Text>
          <Text variant="bodyMedium">{profileName || 'Lawyer'}</Text>
          <Text variant="caption" color="secondary">
            {draft.stateBarCouncil} · {draft.barCouncilNumber}
          </Text>
          <Text variant="caption" color="secondary">
            {draft.cityName}, {draft.stateName}, {draft.countryName}
          </Text>
          <Text variant="caption" color="secondary">
            Fee ₹{draft.consultationFee || '0'} · {draft.languages}
          </Text>
          <Text variant="caption" color="secondary">
            Documents: {DOCUMENT_SLOTS.filter((slot) => hasDocument(slot.type)).map((slot) => slot.label).join(', ') || 'None'}
          </Text>
        </View>
      ) : null}
    </Screen>
  );
}
