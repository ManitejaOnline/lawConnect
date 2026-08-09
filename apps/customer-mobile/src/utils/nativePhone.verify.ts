import {
  normalizeDialablePhone,
  PHONE_APP_UNAVAILABLE,
  PHONE_UNAVAILABLE,
  toTelUri,
} from './nativePhoneLogic';

function assert(condition: unknown, message: string): void {
  if (!condition) throw new Error(message);
}

assert(normalizeDialablePhone(null) === null, 'null phone');
assert(normalizeDialablePhone('') === null, 'empty phone');
assert(normalizeDialablePhone('123') === null, 'too short');
assert(normalizeDialablePhone('+919876543201') === '+919876543201', 'e164');
assert(normalizeDialablePhone('+91 98765 43201') === '+919876543201', 'spaces');
assert(normalizeDialablePhone('+91-(98765)-43201') === '+919876543201', 'hyphen paren');
assert(normalizeDialablePhone('919876543201') === '+919876543201', 'country code without plus');
assert(normalizeDialablePhone('9876543210') === '9876543210', 'local 10 digits');
assert(normalizeDialablePhone('09876543210') === '+09876543210', '11 digits get plus');
assert(toTelUri('+919876543201') === 'tel:+919876543201', 'tel uri');
assert(PHONE_UNAVAILABLE === 'Phone number unavailable', 'copy');
assert(PHONE_APP_UNAVAILABLE === 'Unable to open the phone app.', 'dialer copy');
