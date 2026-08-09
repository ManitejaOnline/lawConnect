import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

/**
 * Lightweight async key-value storage for Zustand persist.
 * Uses SecureStore on native; memory+localStorage fallback on web.
 */
const memory = new Map<string, string>();

export const mmkvLikeStorage = {
  getItem: async (name: string): Promise<string | null> => {
    if (Platform.OS === 'web') {
      try {
        return globalThis.localStorage?.getItem(name) ?? memory.get(name) ?? null;
      } catch {
        return memory.get(name) ?? null;
      }
    }
    return SecureStore.getItemAsync(name);
  },
  setItem: async (name: string, value: string): Promise<void> => {
    if (Platform.OS === 'web') {
      try {
        globalThis.localStorage?.setItem(name, value);
      } catch {
        memory.set(name, value);
      }
      return;
    }
    await SecureStore.setItemAsync(name, value);
  },
  removeItem: async (name: string): Promise<void> => {
    if (Platform.OS === 'web') {
      try {
        globalThis.localStorage?.removeItem(name);
      } catch {
        memory.delete(name);
      }
      return;
    }
    await SecureStore.deleteItemAsync(name);
  },
};
