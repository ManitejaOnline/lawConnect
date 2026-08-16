module.exports = ({ config }) => {
  const standaloneProfiles = new Set(['production', 'preview']);
  const profile = process.env.EAS_BUILD_PROFILE ?? '';
  const isStandaloneBuild = standaloneProfiles.has(profile);

  if (!isStandaloneBuild) {
    return config;
  }

  return {
    ...config,
    plugins: [...(config.plugins || []), './plugins/withStandaloneRelease'],
  };
};
