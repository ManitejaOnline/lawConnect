import type { ApiProblemDetails } from '@/types/api';

export class AppError extends Error {
  readonly status?: number;
  readonly details?: ApiProblemDetails;
  readonly isNetworkError: boolean;
  readonly isServerUnreachable: boolean;

  constructor(
    message: string,
    options?: {
      status?: number;
      details?: ApiProblemDetails;
      isNetworkError?: boolean;
      isServerUnreachable?: boolean;
      cause?: unknown;
    },
  ) {
    super(message, options?.cause ? { cause: options.cause } : undefined);
    this.name = 'AppError';
    this.status = options?.status;
    this.details = options?.details;
    this.isNetworkError = options?.isNetworkError ?? false;
    this.isServerUnreachable = options?.isServerUnreachable ?? false;
  }
}

export function getErrorMessage(error: unknown, fallback = 'Something went wrong'): string {
  if (error instanceof AppError) {
    return error.details?.detail || error.details?.title || error.message || fallback;
  }
  if (error instanceof Error) {
    return error.message || fallback;
  }
  return fallback;
}
