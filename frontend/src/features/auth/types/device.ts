// Unified Device DTOs from backend (Task 017)

// Backend may return status as string or number (enum)
export type DeviceStatus = 'PendingApproval' | 'Trusted' | 'Revoked' | 0 | 1 | 2

// Map numeric status to string
export const deviceStatusMap: Record<number, 'PendingApproval' | 'Trusted' | 'Revoked'> = {
  0: 'PendingApproval',
  1: 'Trusted',
  2: 'Revoked',
}

export function normalizeDeviceStatus(status: DeviceStatus): 'PendingApproval' | 'Trusted' | 'Revoked' {
  if (typeof status === 'number') {
    return deviceStatusMap[status] ?? 'PendingApproval'
  }
  return status
}

export interface DeviceDto {
  id: string
  deviceId: string
  displayName: string | null
  name: string | null

  // Device Info
  userAgent: string | null
  browser: string | null
  browserVersion: string | null
  operatingSystem: string | null
  osVersion: string | null
  deviceType: string | null // Desktop, Mobile, Tablet

  // Location
  ipAddress: string | null
  country: string | null
  countryCode: string | null
  city: string | null
  locationDisplay: string | null

  // Status & Risk
  status: DeviceStatus
  riskScore: number | null
  trustedAt: string | null
  revokedAt: string | null
  lastUsedAt: string

  // Flags
  isCurrent: boolean

  // Timestamps
  createdAt: string
}

// Request types
export interface RenameDeviceRequest {
  name: string
}

// Response types
export interface RevokeDeviceResponse {
  success: boolean
}

export interface RevokeAllDevicesResponse {
  revokedCount: number
}
