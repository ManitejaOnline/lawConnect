const { getDefaultConfig } = require('expo/metro-config');

/** @type {import('expo/metro-config').MetroConfig} */
const config = getDefaultConfig(__dirname);

// Classic App.tsx entry — not Expo Router (avoid a src/app folder).
config.resolver.sourceExts = config.resolver.sourceExts ?? [];

module.exports = config;
