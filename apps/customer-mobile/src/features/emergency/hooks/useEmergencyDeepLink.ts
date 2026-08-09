import { useEffect } from 'react';
import { Linking } from 'react-native';

import { openLawyerEmergency, parseEmergencyDeepLink } from '@/features/emergency/deepLink';

export function useEmergencyDeepLink() {
  useEffect(() => {
    const handleUrl = (url: string | null) => {
      if (!url) return;
      const target = parseEmergencyDeepLink(url);
      if (!target) return;
      openLawyerEmergency(target);
    };

    void Linking.getInitialURL().then(handleUrl);
    const sub = Linking.addEventListener('url', (event) => handleUrl(event.url));
    return () => sub.remove();
  }, []);
}
