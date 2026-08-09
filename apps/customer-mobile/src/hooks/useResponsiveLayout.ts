import { useWindowDimensions } from 'react-native';

import { layout } from '@/theme';

export function useResponsiveLayout() {
  const { width, height } = useWindowDimensions();
  const isTablet = Math.min(width, height) >= 600;
  const contentMaxWidth = isTablet ? layout.maxContentWidth : width;
  const gutter = isTablet ? layout.gutterTablet : layout.gutter;

  return {
    width,
    height,
    isTablet,
    isPhone: !isTablet,
    contentMaxWidth,
    gutter,
  };
}
