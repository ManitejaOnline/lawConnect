const { getDefaultConfig } = require('expo/metro-config');

/** @type {import('expo/metro-config').MetroConfig} */
const config = getDefaultConfig(__dirname);

// Classic App.tsx entry — not Expo Router (avoid a src/app folder).
config.resolver.sourceExts = config.resolver.sourceExts ?? [];

// Web dev bundles are plain <script> tags (not ES modules), so any package whose
// ESM build contains `import.meta` (e.g. zustand's devtools middleware chunk,
// pulled in transitively even though this app never calls devtools()) throws a
// SyntaxError at parse time and blanks the whole page. Prefer the "react-native"
// (CJS) condition over "import" for the web platform to avoid ESM-only builds.
config.resolver.unstable_conditionsByPlatform = {
  ...config.resolver.unstable_conditionsByPlatform,
  web: ['react-native', 'browser', 'require'],
};

module.exports = config;
