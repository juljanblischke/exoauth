import { useMemo, useState, useCallback } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from '@tanstack/react-router'
import { useQueryClient } from '@tanstack/react-query'
import { Loader2 } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { PasswordInput } from '@/components/shared/form'

import { useLogin } from '../hooks/use-login'
import {
  createLoginSchema,
  type LoginFormData,
  type MfaConfirmResponse,
  type DeviceApprovalRequiredResponse,
} from '../types'
import type { AuthResponse } from '@/types/auth'
import { getErrorMessage } from '@/lib/error-utils'
import { getDeviceInfo } from '@/lib/device'
import { MfaVerifyModal } from './mfa-verify-modal'
import { MfaSetupModal } from './mfa-setup-modal'
import { MfaConfirmModal } from './mfa-confirm-modal'
import { ForgotPasswordModal } from './forgot-password-modal'
import { DeviceApprovalModal } from './device-approval-modal'
import { PasskeyLoginButton } from './passkey-login-button'
import { CaptchaWidget } from './captcha-widget'

const AUTH_SESSION_KEY = 'exoauth_has_session'
const AUTH_QUERY_KEY = ['auth', 'me'] as const

export function LoginForm() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  // MFA state
  const [mfaVerifyOpen, setMfaVerifyOpen] = useState(false)
  const [mfaSetupOpen, setMfaSetupOpen] = useState(false)
  const [mfaConfirmOpen, setMfaConfirmOpen] = useState(false)
  const [mfaToken, setMfaToken] = useState<string | null>(null)
  const [setupToken, setSetupToken] = useState<string | null>(null)
  const [backupCodes, setBackupCodes] = useState<string[]>([])
  const [pendingAuthResponse, setPendingAuthResponse] = useState<MfaConfirmResponse | null>(null)
  const [rememberMeForMfa, setRememberMeForMfa] = useState(false)

  // Forgot password state
  const [forgotPasswordOpen, setForgotPasswordOpen] = useState(false)

  // Device approval state
  const [deviceApprovalOpen, setDeviceApprovalOpen] = useState(false)
  const [deviceApprovalToken, setDeviceApprovalToken] = useState<string | null>(null)
  const [deviceRiskFactors, setDeviceRiskFactors] = useState<string[]>([])

  // CAPTCHA state (conditional - only shown after CAPTCHA_REQUIRED error)
  const [showCaptcha, setShowCaptcha] = useState(false)
  const [captchaToken, setCaptchaToken] = useState<string | null>(null)
  const [captchaKey, setCaptchaKey] = useState(0)

  const handleCaptchaVerify = useCallback((token: string) => {
    setCaptchaToken(token)
  }, [])

  const handleCaptchaExpire = useCallback(() => {
    setCaptchaToken(null)
  }, [])

  const resetCaptcha = useCallback(() => {
    setCaptchaToken(null)
    setCaptchaKey((k) => k + 1) // Force widget to remount and refresh
  }, [])

  const { mutate: login, isPending, error } = useLogin({
    onMfaRequired: (response: AuthResponse) => {
      setMfaToken(response.mfaToken)
      setRememberMeForMfa(form.getValues('rememberMe') ?? false)
      setMfaVerifyOpen(true)
    },
    onMfaSetupRequired: (response: AuthResponse) => {
      setSetupToken(response.setupToken)
      setMfaSetupOpen(true)
    },
    onDeviceApprovalRequired: (response: DeviceApprovalRequiredResponse) => {
      setDeviceApprovalToken(response.approvalToken)
      setDeviceRiskFactors(response.riskFactors)
      setDeviceApprovalOpen(true)
    },
    onCaptchaRequired: () => {
      setShowCaptcha(true)
      setCaptchaToken(null)
    },
    onCaptchaExpired: resetCaptcha,
  })

  const loginSchema = useMemo(() => createLoginSchema(t), [t])

  const form = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
      rememberMe: false,
    },
  })

  const onSubmit = (data: LoginFormData) => {
    const deviceInfo = getDeviceInfo()
    login({
      ...data,
      rememberMe: data.rememberMe ?? false,
      ...deviceInfo,
      captchaToken: captchaToken || undefined,
    })
  }

  const handleMfaSetupSuccess = (response: MfaConfirmResponse) => {
    setBackupCodes(response.backupCodes)
    setPendingAuthResponse(response)
    setMfaSetupOpen(false)
    setMfaConfirmOpen(true)
  }

  const handleMfaConfirmContinue = () => {
    // If we have auth data from setupToken flow, complete the login
    if (pendingAuthResponse?.accessToken && pendingAuthResponse.user) {
      // Set user in cache BEFORE navigating (triggers isAuthenticated)
      queryClient.setQueryData(AUTH_QUERY_KEY, pendingAuthResponse.user)
      localStorage.setItem(AUTH_SESSION_KEY, 'true')
      navigate({ to: '/system/dashboard' })
    }
    setMfaConfirmOpen(false)
  }

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      <PasskeyLoginButton
        onMfaRequired={(response: AuthResponse) => {
          setMfaToken(response.mfaToken)
          setRememberMeForMfa(false)
          setMfaVerifyOpen(true)
        }}
        onMfaSetupRequired={(response: AuthResponse) => {
          setSetupToken(response.setupToken)
          setMfaSetupOpen(true)
        }}
        onDeviceApprovalRequired={(response: DeviceApprovalRequiredResponse) => {
          setDeviceApprovalToken(response.approvalToken)
          setDeviceRiskFactors(response.riskFactors)
          setDeviceApprovalOpen(true)
        }}
      />

      <div className="relative">
        <div className="absolute inset-0 flex items-center">
          <span className="w-full border-t" />
        </div>
        <div className="relative flex justify-center text-xs uppercase">
          <span className="bg-background px-2 text-muted-foreground">
            {t('auth:login.orContinueWith')}
          </span>
        </div>
      </div>

      {error && (
        <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
          {getErrorMessage(error, t)}
        </div>
      )}

      <div className="space-y-2">
        <Label htmlFor="email">{t('auth:login.email')}</Label>
        <Input
          id="email"
          type="email"
          autoComplete="email"
          placeholder="name@example.com"
          {...form.register('email')}
        />
        {form.formState.errors.email && (
          <p className="text-sm text-destructive">
            {form.formState.errors.email.message}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <Label htmlFor="password">{t('auth:login.password')}</Label>
          <button
            type="button"
            onClick={() => setForgotPasswordOpen(true)}
            className="text-sm text-primary hover:underline"
          >
            {t('auth:login.forgotPassword')}
          </button>
        </div>
        <PasswordInput
          id="password"
          autoComplete="current-password"
          {...form.register('password')}
        />
        {form.formState.errors.password && (
          <p className="text-sm text-destructive">
            {form.formState.errors.password.message}
          </p>
        )}
      </div>

      <div className="flex items-center space-x-2">
        <Checkbox
          id="rememberMe"
          checked={form.watch('rememberMe')}
          onCheckedChange={(checked) =>
            form.setValue('rememberMe', checked === true)
          }
        />
        <Label
          htmlFor="rememberMe"
          className="text-sm font-normal cursor-pointer"
        >
          {t('auth:login.rememberMe')}
        </Label>
      </div>

      {/* CAPTCHA Widget - shown conditionally after CAPTCHA_REQUIRED error */}
      {showCaptcha && (
        <CaptchaWidget
          key={captchaKey}
          onVerify={handleCaptchaVerify}
          onExpire={handleCaptchaExpire}
          action="login"
          className="flex justify-center"
        />
      )}

      <Button
        type="submit"
        className="w-full"
        disabled={isPending || (showCaptcha && !captchaToken)}
      >
        {isPending ? (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            {t('auth:login.signingIn')}
          </>
        ) : (
          t('auth:login.signIn')
        )}
      </Button>

      <p className="text-center text-sm text-muted-foreground">
        {t('auth:login.noAccount')}{' '}
        <Link to="/system/register" className="text-primary hover:underline">
          {t('auth:login.register')}
        </Link>
      </p>

      {/* Forgot Password Modal */}
      <ForgotPasswordModal
        open={forgotPasswordOpen}
        onOpenChange={setForgotPasswordOpen}
        defaultEmail={form.getValues('email')}
      />

      {/* MFA Verify Modal - shown when user has MFA enabled */}
      <MfaVerifyModal
        open={mfaVerifyOpen}
        onOpenChange={setMfaVerifyOpen}
        mfaToken={mfaToken || ''}
        rememberMe={rememberMeForMfa}
        onDeviceApprovalRequired={(response) => {
          setMfaVerifyOpen(false)
          setDeviceApprovalToken(response.approvalToken)
          setDeviceRiskFactors(response.riskFactors)
          setDeviceApprovalOpen(true)
        }}
      />

      {/* MFA Setup Modal - shown when MFA is required but not set up */}
      <MfaSetupModal
        open={mfaSetupOpen}
        onOpenChange={setMfaSetupOpen}
        onSuccess={handleMfaSetupSuccess}
        setupToken={setupToken || undefined}
        required
      />

      {/* MFA Confirm Modal - shows backup codes after setup */}
      <MfaConfirmModal
        open={mfaConfirmOpen}
        onOpenChange={setMfaConfirmOpen}
        backupCodes={backupCodes}
        onContinue={handleMfaConfirmContinue}
      />

      {/* Device Approval Modal - shown when risk-based auth requires device verification */}
      <DeviceApprovalModal
        open={deviceApprovalOpen}
        onOpenChange={setDeviceApprovalOpen}
        approvalToken={deviceApprovalToken || ''}
        riskFactors={deviceRiskFactors}
        onSuccess={() => {
          // After device approval, user needs to login again
          setDeviceApprovalToken(null)
          setDeviceRiskFactors([])
        }}
      />
    </form>
  )
}
