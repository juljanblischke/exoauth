// Device Session DTOs from backend

export interface DeviceSessionDto {
  id: string
  deviceId: string
  displayName: string
  deviceName: string | null
  browser: string | null
  browserVersion: string | null
  operatingSystem: string | null
  osVersion: string | null
  deviceType: string | null
  ipAddress: string | null
  country: string | null
  countryCode: string | null
  city: string | null
  locationDisplay: string | null
  isCurrent: boolean
  lastActivityAt: string
  createdAt: string
}

// Session management requests
export interface UpdateSessionRequest {
  name?: string
}

// Session management responses
export interface RevokeSessionResponse {
  success: boolean
}

export interface RevokeAllSessionsResponse {
  revokedCount: number
}
