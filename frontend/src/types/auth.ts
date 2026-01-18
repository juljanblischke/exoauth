// User DTO from backend
export interface User {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  isActive: boolean
  emailVerified: boolean
  mfaEnabled: boolean
  preferredLanguage: string
  lastLoginAt: string | null
  createdAt: string
  permissions: string[]
}

export type UserStatus = 'active' | 'inactive' | 'pending' | 'suspended'

export interface AuthState {
  user: User | null
  isAuthenticated: boolean
  isLoading: boolean
  tokenExpiresAt: string | null
}

// Auth Responses
export interface AuthResponse {
  user: User | null
  accessToken: string | null
  refreshToken: string | null
  sessionId: string | null
  deviceId: string | null
  isNewDevice: boolean
  isNewLocation: boolean
  mfaRequired: boolean
  mfaToken: string | null
  mfaSetupRequired: boolean
  setupToken: string | null
}

export interface TokenResponse {
  accessToken: string
  refreshToken: string
  sessionId: string | null
}

export interface LogoutResponse {
  success: boolean
}

export interface ForgotPasswordResponse {
  success: boolean
  message: string
}

export interface ResetPasswordResponse {
  success: boolean
  message: string
}

export interface RequestMagicLinkResponse {
  success: boolean
  message: string
}

// Device info for auth requests
export interface DeviceInfo {
  deviceId: string | null
  deviceFingerprint: string | null
}

// Auth Requests
export interface LoginRequest extends DeviceInfo {
  email: string
  password: string
  rememberMe: boolean
  captchaToken?: string
}

export interface RegisterRequest extends DeviceInfo {
  email: string
  password: string
  firstName: string
  lastName: string
  organizationName?: string
  language?: string
  captchaToken?: string
}

export interface AcceptInviteRequest extends DeviceInfo {
  token: string
  password: string
  language: string
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export interface ForgotPasswordRequest {
  email: string
  captchaToken?: string
}

export interface ResetPasswordRequest {
  token?: string
  email?: string
  code?: string
  newPassword: string
}

export interface RequestMagicLinkRequest {
  email: string
  captchaToken?: string
}

export interface MfaVerifyRequest extends DeviceInfo {
  mfaToken: string
  code: string
  rememberMe: boolean
  captchaToken?: string
}

export interface SessionInfo {
  expiresAt: string
  issuedAt: string
  remainingTime: number
}
