type LogMeta = Record<string, unknown>;

function write(level: 'info' | 'warn' | 'error', scope: string, message: string, meta?: LogMeta) {
  const isDev = typeof __DEV__ !== 'undefined' ? __DEV__ : process.env.NODE_ENV !== 'production';
  if (!isDev) return;
  const payload = meta ? [message, meta] : [message];
  if (level === 'error') {
    console.error(`[${scope}]`, ...payload);
    return;
  }
  if (level === 'warn') {
    console.warn(`[${scope}]`, ...payload);
    return;
  }
  console.info(`[${scope}]`, ...payload);
}

/** Development diagnostics. Never shown in emergency/call UI. */
export const logger = {
  info(scope: string, message: string, meta?: LogMeta) {
    write('info', scope, message, meta);
  },
  warn(scope: string, message: string, meta?: LogMeta) {
    write('warn', scope, message, meta);
  },
  error(scope: string, message: string, error?: unknown, meta?: LogMeta) {
    write('error', scope, message, {
      ...meta,
      error: error instanceof Error ? { name: error.name, message: error.message } : error,
    });
  },
};
