import type { User, DeviceInfo } from '@/types/auth'

// MFA DTOs from backend

export interface MfaSetupResponse {
  secret: string
  qrCodeUri: string
  manualEntryKey: string
}

export interface MfaConfirmRequest extends Partial<DeviceInfo> {
  code: string
}

export interface MfaConfirmResponse {
  success: boolean
  backupCodes: string[]
  // Optional - only present when using setupToken flow (forced MFA during login/register)
  user?: User
  accessToken?: string | null
  refreshToken?: string | null
  sessionId?: string | null
  deviceId?: string | null
}

export interface MfaDisableRequest {
  code: string
}

export interface MfaDisableResponse {
  success: boolean
}

export interface RegenerateBackupCodesRequest {
  code: string
}

export interface RegenerateBackupCodesResponse {
  backupCodes: string[]
}
