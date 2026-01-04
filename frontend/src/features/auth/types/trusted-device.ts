// Trusted Device DTOs from backend

export interface TrustedDeviceDto {
  id: string
  deviceId: string
  name: string
  browser: string | null
  browserVersion: string | null
  operatingSystem: string | null
  osVersion: string | null
  deviceType: string | null // "Desktop" | "Mobile" | "Tablet"
  lastIpAddress: string | null
  lastCountry: string | null
  lastCity: string | null
  locationDisplay: string | null // "Berlin, Germany"
  isCurrent: boolean
  trustedAt: string // ISO date
  lastUsedAt: string | null // ISO date
}

// Request types
export interface RenameDeviceRequest {
  name: string
}

// Response types
export interface RemoveDeviceResponse {
  success: boolean
}

export interface RemoveAllDevicesResponse {
  removedCount: number
}
