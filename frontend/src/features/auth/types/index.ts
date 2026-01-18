// Re-export auth types from global types
export type {
  User,
  UserStatus,
  AuthState,
  AuthResponse,
  TokenResponse,
  LogoutResponse,
  ForgotPasswordResponse,
  ResetPasswordResponse,
  RequestMagicLinkResponse,
  DeviceInfo,
  LoginRequest,
  RegisterRequest,
  AcceptInviteRequest,
  RefreshTokenRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  RequestMagicLinkRequest,
  MfaVerifyRequest,
  SessionInfo,
} from '@/types/auth'

// Validation schemas
import { z } from 'zod'
import type { TFunction } from 'i18next'

// Password schema factory: min 12 chars, upper, lower, digit, special
const createPasswordSchema = (t: TFunction) =>
  z
    .string()
    .min(12, t('validation:password.minLength', { min: 12 }))
    .regex(/[a-z]/, t('validation:password.lowercase'))
    .regex(/[A-Z]/, t('validation:password.uppercase'))
    .regex(/[0-9]/, t('validation:password.number'))
    .regex(/[^a-zA-Z0-9]/, t('validation:password.special'))

export const createLoginSchema = (t: TFunction) =>
  z.object({
    email: z.string().email(t('validation:email')),
    password: z.string().min(1, t('validation:required')),
    rememberMe: z.boolean().default(false),
  })

export const createRegisterSchema = (t: TFunction) =>
  z.object({
    email: z.string().email(t('validation:email')),
    password: createPasswordSchema(t),
    firstName: z.string().min(2, t('validation:minLength', { min: 2 })),
    lastName: z.string().min(2, t('validation:minLength', { min: 2 })),
  })

export const createAcceptInviteSchema = (t: TFunction) =>
  z
    .object({
      token: z.string().min(1, t('validation:required')),
      password: createPasswordSchema(t),
      confirmPassword: z.string().min(1, t('validation:required')),
    })
    .refine((data) => data.password === data.confirmPassword, {
      message: t('validation:password.mismatch'),
      path: ['confirmPassword'],
    })

// Form data types
export interface LoginFormData {
  email: string
  password: string
  rememberMe?: boolean
}

export interface RegisterFormData {
  email: string
  password: string
  firstName: string
  lastName: string
}

export interface AcceptInviteFormData {
  token: string
  password: string
  confirmPassword: string
}

export interface MagicLinkFormData {
  email: string
}

export const createMagicLinkSchema = (t: TFunction) =>
  z.object({
    email: z.string().email(t('validation:email')),
  })

// Invite validation types (public endpoint)
export interface InviterDto {
  fullName: string
}

export interface InvitePermissionDto {
  name: string
  description: string
}

export interface InviteValidationDto {
  valid: boolean
  email: string | null
  firstName: string | null
  lastName: string | null
  expiresAt: string | null
  invitedBy: InviterDto | null
  permissions: InvitePermissionDto[] | null
  errorCode: string | null
  errorMessage: string | null
}

// Re-export device types
export type {
  DeviceDto,
  DeviceStatus,
  RenameDeviceRequest,
  RevokeDeviceResponse,
  RevokeAllDevicesResponse,
} from './device'

// Re-export MFA types
export type {
  MfaSetupResponse,
  MfaConfirmRequest,
  MfaConfirmResponse,
  MfaDisableRequest,
  MfaDisableResponse,
  RegenerateBackupCodesRequest,
  RegenerateBackupCodesResponse,
} from './mfa'

// Re-export Device Approval types
export type {
  DeviceApprovalRequiredResponse,
  ApproveDeviceByCodeRequest,
  ApproveDeviceByCodeResponse,
  ApproveDeviceByLinkResponse,
  DenyDeviceRequest,
  DenyDeviceResponse,
  DeviceApprovalModalState,
  DeviceApprovalModalProps,
  DeviceApprovalCodeInputProps,
} from './device-approval'

export { isDeviceApprovalRequired } from './device-approval'

// Re-export Passkey types
export type {
  PasskeyDto,
  GetPasskeysResponse,
  PasskeyRegisterOptionsResponse,
  PasskeyRegisterRequest,
  PasskeyLoginOptionsResponse,
  PasskeyLoginRequest,
  RenamePasskeyRequest,
} from './passkey'

// Re-export CAPTCHA types
export type {
  CaptchaProvider,
  CaptchaConfig,
  CaptchaWidgetProps,
} from './captcha'

