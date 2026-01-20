// Components
export {
  LoginForm,
  RegisterForm,
  AcceptInviteForm,
  PasswordRequirements,
  MagicLinkForm,
  MagicLinkSent,
} from './components'

// Hooks
export {
  useLogin,
  useRegister,
  useLogout,
  useCurrentUser,
  useAcceptInvite,
  useValidateInvite,
} from './hooks'

// Types
export type {
  User,
  AuthState,
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  AcceptInviteRequest,
  LoginFormData,
  RegisterFormData,
  AcceptInviteFormData,
  InviteValidationDto,
  InviterDto,
  InvitePermissionDto,
} from './types'

export {
  createLoginSchema,
  createRegisterSchema,
  createAcceptInviteSchema,
} from './types'

// API
export { authApi } from './api/auth-api'
