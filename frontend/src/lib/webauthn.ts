import { browserSupportsWebAuthn } from '@simplewebauthn/browser'

/**
 * Check if WebAuthn is supported by the browser
 */
export function isWebAuthnSupported(): boolean {
  return browserSupportsWebAuthn()
}

/**
 * Get a suggested name for the passkey based on browser and OS
 */
export function getSuggestedPasskeyName(): string {
  const userAgent = navigator.userAgent
  
  // Detect browser
  let browser = 'Browser'
  if (userAgent.includes('Chrome') && !userAgent.includes('Edg')) {
    browser = 'Chrome'
  } else if (userAgent.includes('Firefox')) {
    browser = 'Firefox'
  } else if (userAgent.includes('Safari') && !userAgent.includes('Chrome')) {
    browser = 'Safari'
  } else if (userAgent.includes('Edg')) {
    browser = 'Edge'
  }
  
  // Detect OS
  let os = ''
  if (userAgent.includes('Windows')) {
    os = 'Windows'
  } else if (userAgent.includes('Mac OS X') || userAgent.includes('Macintosh')) {
    os = 'macOS'
  } else if (userAgent.includes('Linux')) {
    os = 'Linux'
  } else if (userAgent.includes('Android')) {
    os = 'Android'
  } else if (userAgent.includes('iPhone') || userAgent.includes('iPad')) {
    os = 'iOS'
  }
  
  if (os) {
    return `${browser} on ${os}`
  }
  
  return browser
}

/**
 * WebAuthn error codes mapped to i18n keys
 */
export type WebAuthnErrorType = 
  | 'cancelled'
  | 'timeout'
  | 'not_allowed'
  | 'invalid_state'
  | 'unknown'

/**
 * Parse WebAuthn error and return error type
 */
export function parseWebAuthnError(error: unknown): WebAuthnErrorType {
  if (error instanceof Error) {
    const message = error.message.toLowerCase()
    const name = error.name
    
    // User cancelled the operation
    if (name === 'NotAllowedError' || message.includes('cancelled') || message.includes('canceled')) {
      return 'cancelled'
    }
    
    // Operation timed out
    if (name === 'TimeoutError' || message.includes('timeout')) {
      return 'timeout'
    }
    
    // Not allowed (e.g., no authenticator available)
    if (message.includes('not allowed')) {
      return 'not_allowed'
    }
    
    // Invalid state (e.g., credential already registered)
    if (name === 'InvalidStateError') {
      return 'invalid_state'
    }
  }
  
  return 'unknown'
}
