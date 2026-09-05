function withCleartextTraffic(config, enabled) {
  const plugins = (config.plugins || []).map((plugin) => {
    if (Array.isArray(plugin) && plugin[0] === 'expo-build-properties') {
      const options = plugin[1] || {};
      return [
        'expo-build-properties',
        {
          ...options,
          android: {
            ...(options.android || {}),
            usesCleartextTraffic: enabled,
          },
        },
      ];
    }
    return plugin;
  });
  return { ...config, plugins };
}

module.exports = ({ config }) => {
  const standaloneProfiles = new Set(['production', 'preview']);
  const profile = process.env.EAS_BUILD_PROFILE ?? '';
  const isStandaloneBuild = standaloneProfiles.has(profile);

  let next = {
    ...config,
    android: {
      ...(config.android || {}),
      versionCode: config.android?.versionCode ?? 1,
    },
  };

  if (!isStandaloneBuild) {
    return next;
  }

  next = withCleartextTraffic(next, false);
  next.plugins = [...(next.plugins || []), './plugins/withStandaloneRelease'];
  return next;
};
