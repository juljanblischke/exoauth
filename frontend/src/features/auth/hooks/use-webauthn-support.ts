import { useSyncExternalStore } from 'react'
import { isWebAuthnSupported } from '@/lib/webauthn'

// Cache the result since it won't change during the session
let cachedSupport: boolean | null = null

function subscribe() {
  // WebAuthn support doesn't change, so no-op subscription
  return () => {}
}

function getSnapshot(): boolean {
  if (cachedSupport === null) {
    cachedSupport = isWebAuthnSupported()
  }
  return cachedSupport
}

function getServerSnapshot(): boolean {
  // On server, assume not supported
  return false
}

export function useWebAuthnSupport() {
  const isSupported = useSyncExternalStore(subscribe, getSnapshot, getServerSnapshot)

  return {
    isSupported,
    isLoading: false,
  }
}
