import * as Network from 'expo-network';

/** True only when the device reports no network interface. API failures are not offline. */
export async function isDeviceOffline(): Promise<boolean> {
  try {
    const state = await Network.getNetworkStateAsync();
    return state.isConnected === false;
  } catch {
    return false;
  }
}
