/**
 * Device identification utilities for auth requests.
 * Device ID format: {timestamp-first-half}-{random}-{os}-{browser}-{timestamp-second-half}
 * Example: "1735474-a1b2c3d4-win-chrome-800000"
 * Max: 100 characters (DB limit), typical: 35-45 characters
 */

const DEVICE_ID_STORAGE_KEY = 'exoauth_device_id'

type OSType = 'win' | 'mac' | 'linux' | 'android' | 'ios' | 'unknown'
type BrowserType = 'chrome' | 'firefox' | 'safari' | 'edge' | 'opera' | 'unknown'

/**
 * Detects the operating system from userAgent
 */
function detectOS(): OSType {
  const ua = navigator.userAgent.toLowerCase()

  if (ua.includes('win')) return 'win'
  if (ua.includes('mac')) {
    // Check for iOS devices first
    if (ua.includes('iphone') || ua.includes('ipad') || ua.includes('ipod')) {
      return 'ios'
    }
    return 'mac'
  }
  if (ua.includes('linux')) {
    // Check for Android
    if (ua.includes('android')) return 'android'
    return 'linux'
  }
  if (ua.includes('android')) return 'android'
  if (ua.includes('iphone') || ua.includes('ipad') || ua.includes('ipod')) return 'ios'

  return 'unknown'
}

/**
 * Detects the browser from userAgent
 */
function detectBrowser(): BrowserType {
  const ua = navigator.userAgent.toLowerCase()

  // Order matters - check more specific ones first
  if (ua.includes('edg/') || ua.includes('edge/')) return 'edge'
  if (ua.includes('opr/') || ua.includes('opera')) return 'opera'
  if (ua.includes('chrome')) return 'chrome'
  if (ua.includes('safari') && !ua.includes('chrome')) return 'safari'
  if (ua.includes('firefox')) return 'firefox'

  return 'unknown'
}

/**
 * Generates a new device ID with embedded OS/browser info
 */
function generateDeviceId(): string {
  const ts = Date.now().toString()
  const tsFirst = ts.slice(0, 7)
  const tsSecond = ts.slice(7)

  // Generate random part using crypto if available
  const random =
    typeof crypto !== 'undefined' && crypto.randomUUID
      ? crypto.randomUUID().slice(0, 8)
      : Math.random().toString(36).slice(2, 10)

  const os = detectOS()
  const browser = detectBrowser()

  return `${tsFirst}-${random}-${os}-${browser}-${tsSecond}`
}

/**
 * Gets the existing device ID from localStorage or creates a new one.
 * The ID is persisted until the user clears their browser cache.
 */
export function getOrCreateDeviceId(): string {
  try {
    const stored = localStorage.getItem(DEVICE_ID_STORAGE_KEY)
    if (stored && stored.length <= 100) {
      return stored
    }

    const newId = generateDeviceId()
    localStorage.setItem(DEVICE_ID_STORAGE_KEY, newId)
    return newId
  } catch {
    // localStorage not available (e.g., private browsing in some browsers)
    return generateDeviceId()
  }
}

/**
 * Collects browser/device properties for fingerprinting.
 */
function collectFingerprintData(): string[] {
  const data: string[] = []

  try {
    // Navigator properties
    data.push(navigator.userAgent)
    data.push(navigator.language)
    data.push(navigator.languages?.join(',') || '')
    data.push(navigator.platform)
    data.push(navigator.vendor || '')
    data.push(String(navigator.hardwareConcurrency || 0))
    data.push(String(navigator.maxTouchPoints || 0))
    data.push(String(navigator.cookieEnabled))
    data.push(navigator.doNotTrack || '')
    data.push(String((navigator as { deviceMemory?: number }).deviceMemory || 0))

    // Screen properties
    data.push(String(screen.width))
    data.push(String(screen.height))
    data.push(String(screen.availWidth))
    data.push(String(screen.availHeight))
    data.push(String(screen.colorDepth))
    data.push(String(window.devicePixelRatio || 1))

    // Timezone
    data.push(String(new Date().getTimezoneOffset()))
    data.push(Intl.DateTimeFormat().resolvedOptions().timeZone || '')

    // Canvas fingerprint
    try {
      const canvas = document.createElement('canvas')
      canvas.width = 200
      canvas.height = 50
      const ctx = canvas.getContext('2d')
      if (ctx) {
        ctx.textBaseline = 'top'
        ctx.font = '14px Arial'
        ctx.fillStyle = '#f60'
        ctx.fillRect(125, 1, 62, 20)
        ctx.fillStyle = '#069'
        ctx.fillText('ExoAuth', 2, 15)
        ctx.fillStyle = 'rgba(102, 204, 0, 0.7)'
        ctx.fillText('ExoAuth', 4, 17)
        data.push(canvas.toDataURL().slice(-50))
      }
    } catch {
      data.push('no-canvas')
    }

    // WebGL info
    try {
      const canvas = document.createElement('canvas')
      const gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl')
      if (gl && gl instanceof WebGLRenderingContext) {
        const debugInfo = gl.getExtension('WEBGL_debug_renderer_info')
        if (debugInfo) {
          data.push(gl.getParameter(debugInfo.UNMASKED_VENDOR_WEBGL) || '')
          data.push(gl.getParameter(debugInfo.UNMASKED_RENDERER_WEBGL) || '')
        }
      }
    } catch {
      data.push('no-webgl')
    }

    // Audio context
    try {
      const audioCtx = new (window.AudioContext || (window as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext)()
      data.push(String(audioCtx.sampleRate))
      audioCtx.close()
    } catch {
      data.push('no-audio')
    }

  } catch {
    // Fallback if anything fails
    data.push('error')
  }

  return data
}

/**
 * Generates a SHA-256 hash and returns first 64 hex characters.
 */
async function sha256Hash(str: string): Promise<string> {
  const encoder = new TextEncoder()
  const data = encoder.encode(str)
  const hashBuffer = await crypto.subtle.digest('SHA-256', data)
  const hashArray = Array.from(new Uint8Array(hashBuffer))
  return hashArray.map(b => b.toString(16).padStart(2, '0')).join('')
}

/**
 * Simple synchronous hash fallback (if crypto.subtle unavailable).
 */
function simpleHash64(str: string): string {
  let h1 = 0xdeadbeef
  let h2 = 0x41c6ce57
  for (let i = 0; i < str.length; i++) {
    const char = str.charCodeAt(i)
    h1 = Math.imul(h1 ^ char, 2654435761)
    h2 = Math.imul(h2 ^ char, 1597334677)
  }
  h1 = Math.imul(h1 ^ (h1 >>> 16), 2246822507) ^ Math.imul(h2 ^ (h2 >>> 13), 3266489909)
  h2 = Math.imul(h2 ^ (h2 >>> 16), 2246822507) ^ Math.imul(h1 ^ (h1 >>> 13), 3266489909)

  const part1 = (h1 >>> 0).toString(16).padStart(8, '0')
  const part2 = (h2 >>> 0).toString(16).padStart(8, '0')

  // Repeat to get 64 chars
  return (part1 + part2).repeat(4)
}

// Cache the fingerprint
let cachedFingerprint: string | null = null

/**
 * Creates a fingerprint based on browser properties.
 * Returns a 64-character hex string.
 */
export function getDeviceFingerprint(): string | null {
  if (cachedFingerprint) return cachedFingerprint

  try {
    const components = collectFingerprintData()
    const str = components.join('|')

    // Try async SHA-256, but we need sync result
    // So we use the simple hash and upgrade async in background
    cachedFingerprint = simpleHash64(str)

    // Async upgrade (for next call)
    if (crypto.subtle) {
      sha256Hash(str).then(hash => {
        cachedFingerprint = hash
      }).catch(() => {
        // Keep simple hash
      })
    }

    return cachedFingerprint
  } catch {
    return null
  }
}

/**
 * Returns full device info object for auth requests
 */
export function getDeviceInfo(): {
  deviceId: string | null
  deviceFingerprint: string | null
} {
  return {
    deviceId: getOrCreateDeviceId(),
    deviceFingerprint: getDeviceFingerprint(),
  }
}
