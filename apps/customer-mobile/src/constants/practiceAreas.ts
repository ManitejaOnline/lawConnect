import type { PracticeAreaCategory } from '@/types/home';

/**
 * Catalog aligned with LawConnect.Shared.Constants.SeedIds.Specializations.
 * Used for category UI + specializationId search filters (no public catalog endpoint yet).
 */
export const PRACTICE_AREA_CATEGORIES: PracticeAreaCategory[] = [
  { id: 'aaaaaaaa-0001-0001-0001-000000000001', name: 'Criminal Law', shortName: 'Criminal', icon: 'shield' },
  { id: 'aaaaaaaa-0001-0001-0001-000000000002', name: 'Civil Law', shortName: 'Civil', icon: 'scale' },
  { id: 'aaaaaaaa-0001-0001-0001-000000000003', name: 'Family Law', shortName: 'Family', icon: 'users' },
  { id: 'aaaaaaaa-0001-0001-0001-000000000004', name: 'Corporate Law', shortName: 'Corporate', icon: 'briefcase' },
  { id: 'aaaaaaaa-0001-0001-0001-000000000005', name: 'Property Law', shortName: 'Property', icon: 'home' },
  { id: 'aaaaaaaa-0001-0001-0001-000000000006', name: 'Intellectual Property', shortName: 'IP', icon: 'star' },
  { id: 'aaaaaaaa-0001-0001-0001-000000000007', name: 'Tax Law', shortName: 'Tax', icon: 'credit-card' },
  { id: 'aaaaaaaa-0001-0001-0001-000000000008', name: 'Labor & Employment', shortName: 'Labor', icon: 'briefcase' },
  { id: 'aaaaaaaa-0001-0001-0001-000000000009', name: 'Constitutional Law', shortName: 'Constitution', icon: 'scale' },
  { id: 'aaaaaaaa-0001-0001-0001-00000000000a', name: 'Consumer Protection', shortName: 'Consumer', icon: 'help' },
  { id: 'aaaaaaaa-0001-0001-0001-00000000000b', name: 'Cyber Law', shortName: 'Cyber', icon: 'alert' },
  { id: 'aaaaaaaa-0001-0001-0001-00000000000c', name: 'Immigration Law', shortName: 'Immigration', icon: 'globe' },
  { id: 'aaaaaaaa-0001-0001-0001-00000000000d', name: 'Environmental Law', shortName: 'Environment', icon: 'leaf' },
  { id: 'aaaaaaaa-0001-0001-0001-00000000000e', name: 'Banking & Finance', shortName: 'Banking', icon: 'credit-card' },
  { id: 'aaaaaaaa-0001-0001-0001-00000000000f', name: 'Medical Negligence', shortName: 'Medical', icon: 'plus' },
];

export const HOME_PRACTICE_AREA_IDS = [
  'aaaaaaaa-0001-0001-0001-000000000003',
  'aaaaaaaa-0001-0001-0001-000000000001',
  'aaaaaaaa-0001-0001-0001-000000000004',
  'aaaaaaaa-0001-0001-0001-000000000005',
] as const;
