export { useLogin } from './use-login'
export { useRegister } from './use-register'
export { useLogout } from './use-logout'
export { useCurrentUser } from './use-current-user'
export { useAcceptInvite } from './use-accept-invite'
export { useValidateInvite } from './use-validate-invite'
export { useUpdatePreferences } from './use-update-preferences'

// MFA hooks
export { useMfaSetup } from './use-mfa-setup'
export { useMfaConfirm } from './use-mfa-confirm'
export { useMfaVerify } from './use-mfa-verify'
export { useMfaDisable } from './use-mfa-disable'
export { useRegenerateBackupCodes } from './use-regenerate-backup-codes'

// Device hooks
export { useDevices, DEVICES_QUERY_KEY } from './use-devices'
export { useRevokeDevice } from './use-revoke-device'
export { useRenameDevice } from './use-rename-device'
export { useApproveDeviceFromSession } from './use-approve-device-from-session'

// Password reset hooks
export { useForgotPassword } from './use-forgot-password'
export { useResetPassword } from './use-reset-password'

// Device approval hooks
export { useApproveDeviceByCode } from './use-approve-device-by-code'
export { useApproveDeviceByLink } from './use-approve-device-by-link'
export { useDenyDevice } from './use-deny-device'

// Passkey hooks
export { usePasskeys, PASSKEYS_QUERY_KEY } from './use-passkeys'
export { usePasskeyRegisterOptions } from './use-passkey-register-options'
export { usePasskeyRegister } from './use-passkey-register'
export { usePasskeyLoginOptions } from './use-passkey-login-options'
export { usePasskeyLogin, type UsePasskeyLoginOptions } from './use-passkey-login'
export { useRenamePasskey } from './use-rename-passkey'
export { useDeletePasskey } from './use-delete-passkey'
export { useWebAuthnSupport } from './use-webauthn-support'
