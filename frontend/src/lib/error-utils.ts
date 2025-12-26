import type { TFunction } from 'i18next'
import type { ApiError } from '@/types/api'

/**
 * Extract error message from various error types and translate if possible
 */
export function getErrorMessage(
  error: unknown,
  t: TFunction
): string {
  // Handle ApiError from axios interceptor
  if (isApiError(error)) {
    const translationKey = `errors:codes.${error.code}`
    const translated = t(translationKey)
    // If translation exists (not same as key), use it
    if (translated !== translationKey) {
      return translated
    }
    // Fall back to error message
    return error.message
  }

  // Handle Error objects
  if (error instanceof Error) {
    return error.message
  }

  // Handle string errors
  if (typeof error === 'string') {
    return error
  }

  // Default fallback
  return t('errors:general.message')
}

/**
 * Type guard for ApiError
 */
export function isApiError(error: unknown): error is ApiError {
  return (
    typeof error === 'object' &&
    error !== null &&
    'code' in error &&
    'message' in error &&
    typeof (error as ApiError).code === 'string' &&
    typeof (error as ApiError).message === 'string'
  )
}

/**
 * Get field-specific error from API errors array
 */
export function getFieldError(
  errors: ApiError[] | undefined,
  field: string,
  t: TFunction
): string | undefined {
  const fieldError = errors?.find((e) => e.field === field)
  if (!fieldError) return undefined

  const translationKey = `errors:codes.${fieldError.code}`
  const translated = t(translationKey)
  return translated !== translationKey ? translated : fieldError.message
}
