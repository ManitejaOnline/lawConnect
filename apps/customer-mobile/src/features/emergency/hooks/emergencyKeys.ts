export const emergencyKeys = {
  all: ['emergency'] as const,
  customerActive: () => ['emergency', 'customer-active'] as const,
};
