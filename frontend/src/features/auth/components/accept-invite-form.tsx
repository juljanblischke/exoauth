import { useState, useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { useNavigate } from '@tanstack/react-router'
import { useQueryClient } from '@tanstack/react-query'
import { Loader2 } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { PasswordInput } from '@/components/shared/form'

import { useAcceptInvite } from '../hooks/use-accept-invite'
import { createAcceptInviteSchema, type AcceptInviteFormData, type MfaConfirmResponse } from '../types'
import type { AuthResponse } from '@/types/auth'
import { PasswordRequirements } from './password-requirements'
import { getErrorMessage } from '@/lib/error-utils'
import { getDeviceInfo } from '@/lib/device'
import { MfaSetupModal } from './mfa-setup-modal'
import { MfaConfirmModal } from './mfa-confirm-modal'

const AUTH_SESSION_KEY = 'exoauth_has_session'

interface AcceptInviteFormProps {
  token: string
}

const AUTH_QUERY_KEY = ['auth', 'me'] as const

export function AcceptInviteForm({ token }: AcceptInviteFormProps) {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [password, setPassword] = useState('')

  // MFA state (for invited users with system permissions)
  const [mfaSetupOpen, setMfaSetupOpen] = useState(false)
  const [mfaConfirmOpen, setMfaConfirmOpen] = useState(false)
  const [setupToken, setSetupToken] = useState<string | null>(null)
  const [backupCodes, setBackupCodes] = useState<string[]>([])
  const [pendingAuthResponse, setPendingAuthResponse] = useState<MfaConfirmResponse | null>(null)

  const { mutate: acceptInvite, isPending, error } = useAcceptInvite({
    onMfaSetupRequired: (response: AuthResponse) => {
      setSetupToken(response.setupToken)
      setMfaSetupOpen(true)
    },
  })

  const acceptInviteSchema = useMemo(() => createAcceptInviteSchema(t), [t])

  const form = useForm<AcceptInviteFormData>({
    resolver: zodResolver(acceptInviteSchema),
    defaultValues: {
      token,
      password: '',
      confirmPassword: '',
    },
  })

  const onSubmit = (data: AcceptInviteFormData) => {
    const deviceInfo = getDeviceInfo()
    acceptInvite({
      token: data.token,
      password: data.password,
      language: i18n.language,
      ...deviceInfo,
    })
  }

  const handleMfaSetupSuccess = (response: MfaConfirmResponse) => {
    setBackupCodes(response.backupCodes)
    setPendingAuthResponse(response)
    setMfaSetupOpen(false)
    setMfaConfirmOpen(true)
  }

  const handleMfaConfirmContinue = () => {
    // If we have auth data from setupToken flow, complete the accept invite
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
      {error && (
        <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
          {getErrorMessage(error, t)}
        </div>
      )}

      <div className="space-y-2">
        <Label htmlFor="password">{t('auth:register.password')}</Label>
        <PasswordInput
          id="password"
          autoComplete="new-password"
          {...form.register('password', {
            onChange: (e) => setPassword(e.target.value),
          })}
        />
        {form.formState.errors.password && (
          <p className="text-sm text-destructive">
            {form.formState.errors.password.message}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="confirmPassword">{t('auth:register.confirmPassword')}</Label>
        <PasswordInput
          id="confirmPassword"
          autoComplete="new-password"
          {...form.register('confirmPassword')}
        />
        {form.formState.errors.confirmPassword && (
          <p className="text-sm text-destructive">
            {form.formState.errors.confirmPassword.message}
          </p>
        )}
      </div>

      <PasswordRequirements password={password} />

      <Button type="submit" className="w-full" disabled={isPending}>
        {isPending ? (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            {t('auth:invite.accepting')}
          </>
        ) : (
          t('auth:invite.accept')
        )}
      </Button>

      {/* MFA Setup Modal - shown when MFA is required for invited user with system permissions */}
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
    </form>
  )
}
