const { withDangerousMod, withSettingsGradle } = require('expo/config-plugins');
const fs = require('fs');
const path = require('path');

const DEV_LAUNCHER_PACKAGES = [
  'expo-dev-client',
  'expo-dev-launcher',
  'expo-dev-menu',
  'expo-dev-menu-interface',
];

function excludeDevLauncherFromPackageJson(projectRoot) {
  const pkgPath = path.join(projectRoot, 'package.json');
  const pkg = JSON.parse(fs.readFileSync(pkgPath, 'utf8'));
  pkg.expo = pkg.expo || {};
  pkg.expo.autolinking = pkg.expo.autolinking || {};
  pkg.expo.autolinking.exclude = [
    ...new Set([...(pkg.expo.autolinking.exclude || []), ...DEV_LAUNCHER_PACKAGES]),
  ];
  fs.writeFileSync(pkgPath, `${JSON.stringify(pkg, null, 2)}\n`);
}

/** Gradle autolinking reads package.json + settings.gradle, not app.config.js. */
function withStandaloneRelease(config) {
  config = withDangerousMod(config, [
    'android',
    async (modConfig) => {
      excludeDevLauncherFromPackageJson(modConfig.modRequest.projectRoot);
      return modConfig;
    },
  ]);

  return withSettingsGradle(config, (modConfig) => {
    if (modConfig.modResults.contents.includes('expoAutolinking.exclude')) {
      return modConfig;
    }

    modConfig.modResults.contents = modConfig.modResults.contents.replace(
      'expoAutolinking.useExpoModules()',
      `expoAutolinking.exclude = ${JSON.stringify(DEV_LAUNCHER_PACKAGES)}\nexpoAutolinking.useExpoModules()`
    );
    return modConfig;
  });
}

module.exports = withStandaloneRelease;
