import { useState, useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from '@tanstack/react-router'
import { Loader2 } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { PasswordInput } from '@/components/shared/form'

import { useRegister } from '../hooks/use-register'
import { createRegisterSchema, type RegisterFormData, type MfaConfirmResponse } from '../types'
import type { AuthResponse } from '@/types/auth'
import { PasswordRequirements } from './password-requirements'
import { getErrorMessage } from '@/lib/error-utils'
import { getDeviceInfo } from '@/lib/device'
import { MfaSetupModal } from './mfa-setup-modal'
import { MfaConfirmModal } from './mfa-confirm-modal'

const AUTH_SESSION_KEY = 'exoauth_has_session'

export function RegisterForm() {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const [password, setPassword] = useState('')

  // MFA state (for first user registration)
  const [mfaSetupOpen, setMfaSetupOpen] = useState(false)
  const [mfaConfirmOpen, setMfaConfirmOpen] = useState(false)
  const [setupToken, setSetupToken] = useState<string | null>(null)
  const [backupCodes, setBackupCodes] = useState<string[]>([])
  const [pendingAuthResponse, setPendingAuthResponse] = useState<MfaConfirmResponse | null>(null)

  const { mutate: register, isPending, error } = useRegister({
    onMfaSetupRequired: (response: AuthResponse) => {
      setSetupToken(response.setupToken)
      setMfaSetupOpen(true)
    },
  })

  const registerSchema = useMemo(() => createRegisterSchema(t), [t])

  const form = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      email: '',
      password: '',
      firstName: '',
      lastName: '',
    },
  })

  const onSubmit = (data: RegisterFormData) => {
    const deviceInfo = getDeviceInfo()
    register({
      ...data,
      ...deviceInfo,
      language: i18n.language,
    })
  }

  const handleMfaSetupSuccess = (response: MfaConfirmResponse) => {
    setBackupCodes(response.backupCodes)
    setPendingAuthResponse(response)
    setMfaSetupOpen(false)
    setMfaConfirmOpen(true)
  }

  const handleMfaConfirmContinue = () => {
    // If we have auth data from setupToken flow, complete the registration
    if (pendingAuthResponse?.accessToken) {
      localStorage.setItem(AUTH_SESSION_KEY, 'true')
      navigate({ to: '/dashboard' })
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

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label htmlFor="firstName">{t('auth:register.firstName')}</Label>
          <Input
            id="firstName"
            autoComplete="given-name"
            {...form.register('firstName')}
          />
          {form.formState.errors.firstName && (
            <p className="text-sm text-destructive">
              {form.formState.errors.firstName.message}
            </p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="lastName">{t('auth:register.lastName')}</Label>
          <Input
            id="lastName"
            autoComplete="family-name"
            {...form.register('lastName')}
          />
          {form.formState.errors.lastName && (
            <p className="text-sm text-destructive">
              {form.formState.errors.lastName.message}
            </p>
          )}
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor="email">{t('auth:register.email')}</Label>
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

      <PasswordRequirements password={password} />

      <Button type="submit" className="w-full" disabled={isPending}>
        {isPending ? (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            {t('auth:register.creating')}
          </>
        ) : (
          t('auth:register.createAccount')
        )}
      </Button>

      <p className="text-center text-sm text-muted-foreground">
        {t('auth:register.hasAccount')}{' '}
        <Link to="/login" className="text-primary hover:underline">
          {t('auth:register.signIn')}
        </Link>
      </p>

      {/* MFA Setup Modal - shown when MFA is required for first user */}
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
