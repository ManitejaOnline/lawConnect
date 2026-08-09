import type { EmergencyUiCategory } from '@/types/emergency';

/**
 * Customer SOS categories mapped onto seeded backend EmergencyCategories.
 * Seed IDs from LawConnect.Shared.Constants.SeedIds.EmergencyCategories.
 */
export const EMERGENCY_UI_CATEGORIES: EmergencyUiCategory[] = [
  {
    key: 'police-arrest',
    title: 'Arrest / Police',
    subtitle: 'Custody, FIR, and immediate criminal counsel',
    backendCategoryId: 'bbbbbbbb-0001-0001-0001-000000000001',
    icon: 'shield',
  },
  {
    key: 'cyber-crime',
    title: 'Cyber Crime',
    subtitle: 'Fraud, hacking, and digital threats',
    backendCategoryId: 'bbbbbbbb-0001-0001-0001-000000000004',
    icon: 'alert',
  },
  {
    key: 'domestic-violence',
    title: 'Domestic Violence',
    subtitle: 'Urgent protection and family crisis support',
    backendCategoryId: 'bbbbbbbb-0001-0001-0001-000000000003',
    icon: 'user',
  },
  {
    key: 'accident',
    title: 'Accident',
    subtitle: 'Accident liability and emergency guidance',
    backendCategoryId: 'bbbbbbbb-0001-0001-0001-000000000002',
    icon: 'alert',
  },
  {
    key: 'family',
    title: 'Family',
    subtitle: 'Custody, maintenance, and crisis support',
    backendCategoryId: 'bbbbbbbb-0001-0001-0001-000000000003',
    icon: 'chat',
  },
  {
    key: 'property',
    title: 'Property',
    subtitle: 'Possession, trespass, and urgent civil help',
    backendCategoryId: 'bbbbbbbb-0001-0001-0001-000000000002',
    icon: 'home',
  },
  {
    key: 'other',
    title: 'Other',
    subtitle: 'Describe your emergency — we will route help',
    backendCategoryId: 'bbbbbbbb-0001-0001-0001-000000000001',
    icon: 'phone',
  },
];

export const EMERGENCY_TIPS = [
  'Stay somewhere safe and keep your phone charged.',
  'Share only essential facts — your lawyer will guide next steps.',
  'Do not sign documents until you have spoken with counsel.',
  'If you are in immediate danger, contact local emergency services first.',
];
