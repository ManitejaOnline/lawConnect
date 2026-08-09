import { isExpoBundlerUrl, isLoopbackApiUrl, readEnvApiUrl } from './apiBaseUrlLogic';

function assert(condition: unknown, message: string): void {
  if (!condition) throw new Error(message);
}

assert(isExpoBundlerUrl('exp://lpbyins-anonymous-8081.exp.direct:80') === true, 'exp scheme');
assert(isExpoBundlerUrl('http://lpbyins-anonymous-8081.exp.direct') === true, 'exp.direct');
assert(isExpoBundlerUrl('http://192.168.1.38:8081') === true, 'metro port');
assert(isExpoBundlerUrl('http://192.168.1.38:5080') === false, 'lan api');
assert(isExpoBundlerUrl('https://api.lawconnect.example') === false, 'prod api');
assert(isLoopbackApiUrl('http://localhost:5080') === true, 'localhost');
assert(isLoopbackApiUrl('http://127.0.0.1:5080') === true, 'loopback');
assert(isLoopbackApiUrl('http://192.168.1.38:5080') === false, 'lan not loopback');
assert(readEnvApiUrl({ EXPO_PUBLIC_API_URL: 'http://192.168.1.100:5080/' }) === 'http://192.168.1.100:5080', 'env trim');
assert(readEnvApiUrl({ EXPO_PUBLIC_API_URL: 'exp://lpbyins-anonymous-8081.exp.direct:80' }) === 'exp://lpbyins-anonymous-8081.exp.direct:80', 'does not rewrite expo');

console.info('apiBaseUrl.verify ok');
