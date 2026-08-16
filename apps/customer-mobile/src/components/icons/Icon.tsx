import React from 'react';
import { Feather, MaterialCommunityIcons } from '@expo/vector-icons';

import { useTheme } from '@/theme/ThemeProvider';

export type IconName =
  | 'home'
  | 'search'
  | 'calendar'
  | 'chat'
  | 'user'
  | 'close'
  | 'chevron-left'
  | 'chevron-right'
  | 'check'
  | 'star'
  | 'star-half'
  | 'alert'
  | 'siren'
  | 'wifi-off'
  | 'plus'
  | 'filter'
  | 'bell'
  | 'settings'
  | 'phone'
  | 'shield'
  | 'arrow-right'
  | 'video'
  | 'mic'
  | 'mic-off'
  | 'speaker'
  | 'send'
  | 'image'
  | 'file'
  | 'checks'
  | 'clock'
  | 'refresh'
  | 'phone-end'
  | 'share'
  | 'heart'
  | 'heart-fill'
  | 'map-pin'
  | 'logout'
  | 'credit-card'
  | 'help'
  | 'briefcase'
  | 'paperclip'
  | 'more'
  | 'edit'
  | 'document'
  | 'users'
  | 'globe'
  | 'scale'
  | 'leaf'
  | 'building'
  | 'gavel'
  | 'flag';

export type IconProps = {
  name: IconName;
  size?: number;
  color?: string;
  strokeWidth?: number;
};

const FEATHER: Partial<Record<IconName, keyof typeof Feather.glyphMap>> = {
  home: 'home',
  search: 'search',
  calendar: 'calendar',
  chat: 'message-circle',
  user: 'user',
  close: 'x',
  'chevron-left': 'chevron-left',
  'chevron-right': 'chevron-right',
  check: 'check',
  star: 'star',
  alert: 'alert-triangle',
  'wifi-off': 'wifi-off',
  plus: 'plus',
  filter: 'sliders',
  bell: 'bell',
  settings: 'settings',
  phone: 'phone',
  shield: 'shield',
  'arrow-right': 'arrow-right',
  video: 'video',
  mic: 'mic',
  'mic-off': 'mic-off',
  speaker: 'volume-2',
  send: 'send',
  image: 'image',
  file: 'file',
  clock: 'clock',
  refresh: 'refresh-cw',
  share: 'share-2',
  heart: 'heart',
  'map-pin': 'map-pin',
  logout: 'log-out',
  'credit-card': 'credit-card',
  help: 'help-circle',
  briefcase: 'briefcase',
  paperclip: 'paperclip',
  more: 'more-horizontal',
  edit: 'edit-2',
  document: 'file-text',
  users: 'users',
  globe: 'globe',
  building: 'home',
  flag: 'flag',
};

const MCI: Partial<Record<IconName, keyof typeof MaterialCommunityIcons.glyphMap>> = {
  siren: 'alarm-light-outline',
  'star-half': 'star-half-full',
  checks: 'check-all',
  'phone-end': 'phone-hangup',
  'heart-fill': 'heart',
  scale: 'scale-balance',
  leaf: 'leaf',
  gavel: 'gavel',
};

export function Icon({ name, size = 20, color }: IconProps) {
  const theme = useTheme();
  const resolved = color ?? theme.colors.textPrimary;

  const mci = MCI[name];
  if (mci) {
    return <MaterialCommunityIcons name={mci} size={size} color={resolved} />;
  }

  const feather = FEATHER[name] ?? 'circle';
  return <Feather name={feather} size={size} color={resolved} />;
}
