import type { TFunction } from 'i18next'
import type { ApiError } from '@/types/api'

/**
 * Format a date for display in user's locale
 */
function formatLockedUntil(lockedUntil: string, t: TFunction): string {
  try {
    const date = new Date(lockedUntil)
    const now = new Date()
    const diffMs = date.getTime() - now.getTime()
    
    // If less than 1 minute, show seconds
    if (diffMs < 60 * 1000) {
      const seconds = Math.max(0, Math.ceil(diffMs / 1000))
      return t('errors:codes.ACCOUNT_LOCKED_SECONDS', { seconds })
    }
    
    // If less than 1 hour, show minutes
    if (diffMs < 60 * 60 * 1000) {
      const minutes = Math.ceil(diffMs / (60 * 1000))
      return t('errors:codes.ACCOUNT_LOCKED_MINUTES', { minutes })
    }
    
    // Otherwise show the formatted time
    const formattedTime = date.toLocaleTimeString(undefined, {
      hour: '2-digit',
      minute: '2-digit',
    })
    return t('errors:codes.ACCOUNT_LOCKED_UNTIL', { time: formattedTime })
  } catch {
    return t('errors:codes.ACCOUNT_LOCKED')
  }
}

/**
 * Extract error message from various error types and translate if possible
 */
export function getErrorMessage(
  error: unknown,
  t: TFunction
): string {
  // Handle ApiError from axios interceptor
  if (isApiError(error)) {
    // Special handling for ACCOUNT_LOCKED with lockedUntil data
    if (error.code === 'ACCOUNT_LOCKED' && error.data?.lockedUntil) {
      return formatLockedUntil(error.data.lockedUntil as string, t)
    }
    
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
