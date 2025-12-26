// User DTO from backend
export interface User {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  isActive: boolean
  emailVerified: boolean
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
  user: User
  accessToken: string
  refreshToken: string
}

export interface TokenResponse {
  accessToken: string
  refreshToken: string
}

export interface LogoutResponse {
  success: boolean
}

// Auth Requests
export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  email: string
  password: string
  firstName: string
  lastName: string
  organizationName?: string
}

export interface AcceptInviteRequest {
  token: string
  password: string
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  token: string
  password: string
}

export interface MfaVerifyRequest {
  code: string
  backupCode?: boolean
}

export interface SessionInfo {
  expiresAt: string
  issuedAt: string
  remainingTime: number
}
