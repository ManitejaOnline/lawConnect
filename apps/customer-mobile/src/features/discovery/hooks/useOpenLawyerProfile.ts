import React from 'react';

import { useNavigation } from '@react-navigation/native';

/** Navigate into Search tab lawyer profile from Home stack screens. */
export function useOpenLawyerProfile() {
  const navigation = useNavigation();

  return (lawyerId: string) => {
    // Cross-tab navigation into discovery stack.
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (navigation as any).navigate('SearchTab', {
      screen: 'LawyerProfile',
      params: { lawyerId },
    });
  };
}
