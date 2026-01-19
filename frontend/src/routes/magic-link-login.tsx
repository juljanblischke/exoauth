import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useSearch, useNavigate } from '@tanstack/react-router'
import { useQueryClient } from '@tanstack/react-query'
import { Loader2, AlertTriangle } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { useMagicLinkLogin, type UseMagicLinkLoginOptions } from '@/features/auth/hooks'
import { getErrorMessage } from '@/lib/error-utils'
import { getDeviceInfo } from '@/lib/device'
import { MfaVerifyModal } from '@/features/auth/components/mfa-verify-modal'
import { MfaSetupModal } from '@/features/auth/components/mfa-setup-modal'
import { MfaConfirmModal } from '@/features/auth/components/mfa-confirm-modal'
import { DeviceApprovalModal } from '@/features/auth/components/device-approval-modal'
import type { AuthResponse } from '@/types/auth'
import type { MfaConfirmResponse, DeviceApprovalRequiredResponse } from '@/features/auth/types'

const AUTH_SESSION_KEY = 'exoauth_has_session'
const AUTH_QUERY_KEY = ['auth', 'me'] as const

export function MagicLinkLoginPage() {
  const { t } = useTranslation()
  const search = useSearch({ strict: false }) as { token?: string }
  const token = search.token || ''
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [rememberMe, setRememberMe] = useState(false)

  // MFA state
  const [mfaVerifyOpen, setMfaVerifyOpen] = useState(false)
  const [mfaSetupOpen, setMfaSetupOpen] = useState(false)
  const [mfaConfirmOpen, setMfaConfirmOpen] = useState(false)
  const [mfaToken, setMfaToken] = useState<string | null>(null)
  const [setupToken, setSetupToken] = useState<string | null>(null)
  const [backupCodes, setBackupCodes] = useState<string[]>([])
  const [pendingAuthResponse, setPendingAuthResponse] = useState<MfaConfirmResponse | null>(null)

  // Device approval state
  const [deviceApprovalOpen, setDeviceApprovalOpen] = useState(false)
  const [deviceApprovalToken, setDeviceApprovalToken] = useState<string | null>(null)
  const [deviceRiskFactors, setDeviceRiskFactors] = useState<string[]>([])

  const options: UseMagicLinkLoginOptions = {
    onMfaRequired: (response: AuthResponse) => {
      setMfaToken(response.mfaToken)
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
  }

  const magicLinkLogin = useMagicLinkLogin(options)

  // Automatically trigger login when token is available
  useEffect(() => {
    if (token && !magicLinkLogin.isPending && !magicLinkLogin.isSuccess && !magicLinkLogin.isError) {
      const deviceInfo = getDeviceInfo()
      magicLinkLogin.mutate({
        token,
        rememberMe,
        ...deviceInfo,
      })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token])

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
      navigate({ to: '/dashboard' })
    }
    setMfaConfirmOpen(false)
  }

  // No token provided
  if (!token) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <div className="w-full max-w-md space-y-6">
          <div className="rounded-lg border bg-card p-6 shadow-sm">
            <div className="flex flex-col items-center gap-4 text-center">
              <div className="rounded-full bg-destructive/10 p-3">
                <AlertTriangle className="h-8 w-8 text-destructive" />
              </div>
              <h1 className="text-xl font-semibold">
                {t('errors:api.badRequest')}
              </h1>
              <p className="text-sm text-muted-foreground">
                {t('errors:codes.AUTH_MAGIC_LINK_INVALID')}
              </p>
              <Button asChild className="w-full mt-4">
                <Link to="/login">{t('auth:forgotPassword.backToLogin')}</Link>
              </Button>
            </div>
          </div>
        </div>
      </div>
    )
  }

  // Loading state (validating token)
  if (magicLinkLogin.isPending) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <div className="w-full max-w-md space-y-6">
          <div className="rounded-lg border bg-card p-6 shadow-sm">
            <div className="flex flex-col items-center gap-4 text-center">
              <Loader2 className="h-12 w-12 animate-spin text-primary" />
              <h1 className="text-xl font-semibold">
                {t('auth:magicLink.validating')}
              </h1>
              <p className="text-sm text-muted-foreground">
                {t('auth:magicLink.validatingMessage')}
              </p>
            </div>
          </div>
        </div>
      </div>
    )
  }

  // Error state
  if (magicLinkLogin.isError) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <div className="w-full max-w-md space-y-6">
          <div className="space-y-2 text-center">
            <h1 className="text-3xl font-bold tracking-tight">
              {t('auth:magicLink.error')}
            </h1>
          </div>

          <div className="rounded-lg border bg-card p-6 shadow-sm">
            <div className="space-y-4">
              <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                {getErrorMessage(magicLinkLogin.error, t)}
              </div>

              <div className="space-y-2">
                <Label htmlFor="rememberMe" className="text-sm font-normal">
                  {t('auth:magicLink.retryOptions')}
                </Label>
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="rememberMe"
                    checked={rememberMe}
                    onCheckedChange={(checked) => setRememberMe(checked === true)}
                  />
                  <Label
                    htmlFor="rememberMe"
                    className="text-sm font-normal cursor-pointer"
                  >
                    {t('auth:login.rememberMe')}
                  </Label>
                </div>
              </div>

              <Button
                onClick={() => {
                  const deviceInfo = getDeviceInfo()
                  magicLinkLogin.mutate({
                    token,
                    rememberMe,
                    ...deviceInfo,
                  })
                }}
                className="w-full"
                disabled={magicLinkLogin.isPending}
              >
                {magicLinkLogin.isPending ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    {t('auth:magicLink.retrying')}
                  </>
                ) : (
                  t('auth:magicLink.retry')
                )}
              </Button>

              <Button asChild variant="outline" className="w-full">
                <Link to="/login">{t('auth:forgotPassword.backToLogin')}</Link>
              </Button>
            </div>
          </div>
        </div>
      </div>
    )
  }

  // Success state is handled by the hook navigating to /dashboard
  // This return should not be reached, but included for completeness
  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <div className="w-full max-w-md space-y-6">
        <div className="rounded-lg border bg-card p-6 shadow-sm">
          <div className="flex flex-col items-center gap-4 text-center">
            <Loader2 className="h-12 w-12 animate-spin text-primary" />
            <h1 className="text-xl font-semibold">
              {t('auth:magicLink.success')}
            </h1>
            <p className="text-sm text-muted-foreground">
              {t('auth:magicLink.redirecting')}
            </p>
          </div>
        </div>
      </div>

      {/* MFA Verify Modal - shown when user has MFA enabled */}
      <MfaVerifyModal
        open={mfaVerifyOpen}
        onOpenChange={setMfaVerifyOpen}
        mfaToken={mfaToken || ''}
        rememberMe={rememberMe}
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
    </div>
  )
}
