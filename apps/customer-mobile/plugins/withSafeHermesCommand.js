const { withAppBuildGradle } = require('expo/config-plugins');

/**
 * SDK 54 app/build.gradle evaluates hermesCommand via node require.resolve + getParentFile().
 * If that resolve is empty (hermes-compiler or a blank .execute result), Gradle NPEs:
 *   Cannot invoke method getAbsolutePath() on null object (line 14).
 * RN's Gradle plugin already locates hermesc; drop the custom assignment.
 */
function withSafeHermesCommand(config) {
  return withAppBuildGradle(config, (modConfig) => {
    let contents = modConfig.modResults.contents;
    const multiline = /[ \t]*hermesCommand\s*=\s*new File\([\s\S]*?getParentFile\(\)\.getAbsolutePath\(\)\s*\+\s*["'][^"']+["']\s*\r?\n/;
    if (multiline.test(contents)) {
      contents = contents.replace(multiline, '');
    } else {
      contents = contents.replace(/^[ \t]*hermesCommand\s*=.*\r?\n/m, '');
    }
    modConfig.modResults.contents = contents;
    return modConfig;
  });
}

module.exports = withSafeHermesCommand;
