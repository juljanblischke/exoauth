import { useState, useEffect, useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Loader2, CheckCircle2, Mail, KeyRound, ArrowLeft } from 'lucide-react'
import { z } from 'zod'
import type { TFunction } from 'i18next'

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { PasswordInput } from '@/components/shared/form'
import { PasswordRequirements } from './password-requirements'
import { useForgotPassword, useResetPassword } from '../hooks'
import { getErrorMessage } from '@/lib/error-utils'

type Step = 'email' | 'code' | 'password' | 'success'

// Password schema: min 12 chars, upper, lower, digit, special
const createPasswordSchema = (t: TFunction) =>
  z
    .object({
      newPassword: z
        .string()
        .min(12, t('validation:password.minLength', { min: 12 }))
        .regex(/[a-z]/, t('validation:password.lowercase'))
        .regex(/[A-Z]/, t('validation:password.uppercase'))
        .regex(/[0-9]/, t('validation:password.number'))
        .regex(/[^a-zA-Z0-9]/, t('validation:password.special')),
      confirmPassword: z.string().min(1, t('validation:required')),
    })
    .refine((data) => data.newPassword === data.confirmPassword, {
      message: t('validation:password.mismatch'),
      path: ['confirmPassword'],
    })

interface PasswordFormData {
  newPassword: string
  confirmPassword: string
}

interface ForgotPasswordModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  defaultEmail?: string
}

export function ForgotPasswordModal({
  open,
  onOpenChange,
  defaultEmail = '',
}: ForgotPasswordModalProps) {
  const { t } = useTranslation()
  const [step, setStep] = useState<Step>('email')
  const [email, setEmail] = useState(defaultEmail)
  const [code, setCode] = useState('')

  const forgotPassword = useForgotPassword()
  const resetPassword = useResetPassword()

  const passwordSchema = useMemo(() => createPasswordSchema(t), [t])

  const passwordForm = useForm<PasswordFormData>({
    resolver: zodResolver(passwordSchema),
    defaultValues: {
      newPassword: '',
      confirmPassword: '',
    },
  })

  const watchedPassword = passwordForm.watch('newPassword')

  // Reset state when modal opens/closes
  useEffect(() => {
    if (open) {
      setEmail(defaultEmail)
      setCode('')
      setStep('email')
      forgotPassword.reset()
      resetPassword.reset()
      passwordForm.reset()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, defaultEmail])

  const handleEmailSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!email.trim()) return

    forgotPassword.mutate(
      { email: email.trim() },
      {
        onSuccess: () => {
          setStep('code')
        },
      }
    )
  }

  const handleCodeSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!code.trim()) return
    setStep('password')
  }

  const handlePasswordSubmit = (data: PasswordFormData) => {
    resetPassword.mutate(
      {
        email: email.trim(),
        code: code.trim(),
        newPassword: data.newPassword,
      },
      {
        onSuccess: () => {
          setStep('success')
        },
      }
    )
  }

  const handleBack = () => {
    if (step === 'code') {
      setStep('email')
      setCode('')
    } else if (step === 'password') {
      setStep('code')
      passwordForm.reset()
      resetPassword.reset()
    }
  }

  const isEmailPending = forgotPassword.isPending
  const isResetPending = resetPassword.isPending

  // Step 1: Email entry
  if (step === 'email') {
    return (
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t('auth:forgotPassword.title')}</DialogTitle>
            <DialogDescription>
              {t('auth:forgotPassword.subtitle')}
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleEmailSubmit} className="space-y-4">
            {forgotPassword.isError && (
              <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                {getErrorMessage(forgotPassword.error, t)}
              </div>
            )}

            <div className="space-y-2">
              <Label htmlFor="forgot-email">{t('auth:forgotPassword.email')}</Label>
              <Input
                id="forgot-email"
                type="email"
                placeholder="name@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                autoFocus
                disabled={isEmailPending}
              />
            </div>

            <div className="flex gap-3">
              <Button
                type="button"
                variant="outline"
                className="flex-1"
                onClick={() => onOpenChange(false)}
                disabled={isEmailPending}
              >
                {t('auth:forgotPassword.backToLogin')}
              </Button>
              <Button
                type="submit"
                className="flex-1"
                disabled={!email.trim() || isEmailPending}
              >
                {isEmailPending ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    {t('auth:forgotPassword.sending')}
                  </>
                ) : (
                  t('auth:forgotPassword.sendLink')
                )}
              </Button>
            </div>
          </form>
        </DialogContent>
      </Dialog>
    )
  }

  // Step 2: Code entry
  if (step === 'code') {
    return (
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <KeyRound className="h-5 w-5" />
              {t('auth:forgotPassword.enterCode')}
            </DialogTitle>
            <DialogDescription>
              {t('auth:forgotPassword.codeDescription')}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="flex items-center justify-center py-2">
              <div className="rounded-full bg-primary/10 p-3">
                <Mail className="h-8 w-8 text-primary" />
              </div>
            </div>

            <p className="text-center text-sm text-muted-foreground">
              {email}
            </p>

            <form onSubmit={handleCodeSubmit} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="reset-code">{t('auth:forgotPassword.code')}</Label>
                <Input
                  id="reset-code"
                  type="text"
                  placeholder="XXXX-XXXX"
                  value={code}
                  onChange={(e) => setCode(e.target.value.toUpperCase())}
                  autoFocus
                  className="text-center text-lg tracking-widest font-mono"
                />
              </div>

              <div className="flex gap-3">
                <Button
                  type="button"
                  variant="outline"
                  onClick={handleBack}
                >
                  <ArrowLeft className="h-4 w-4 mr-2" />
                  {t('common:actions.back')}
                </Button>
                <Button
                  type="submit"
                  className="flex-1"
                  disabled={!code.trim()}
                >
                  {t('common:actions.continue')}
                </Button>
              </div>
            </form>
          </div>
        </DialogContent>
      </Dialog>
    )
  }

  // Step 3: New password entry
  if (step === 'password') {
    return (
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t('auth:resetPassword.title')}</DialogTitle>
            <DialogDescription>
              {t('auth:resetPassword.subtitle')}
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={passwordForm.handleSubmit(handlePasswordSubmit)} className="space-y-4">
            {resetPassword.isError && (
              <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                {getErrorMessage(resetPassword.error, t)}
              </div>
            )}

            <div className="space-y-2">
              <Label htmlFor="new-password">
                {t('auth:resetPassword.newPassword')}
              </Label>
              <PasswordInput
                id="new-password"
                autoComplete="new-password"
                {...passwordForm.register('newPassword')}
                disabled={isResetPending}
              />
              {passwordForm.formState.errors.newPassword && (
                <p className="text-sm text-destructive">
                  {passwordForm.formState.errors.newPassword.message}
                </p>
              )}
            </div>

            <PasswordRequirements password={watchedPassword} />

            <div className="space-y-2">
              <Label htmlFor="confirm-password">
                {t('auth:resetPassword.confirmPassword')}
              </Label>
              <PasswordInput
                id="confirm-password"
                autoComplete="new-password"
                {...passwordForm.register('confirmPassword')}
                disabled={isResetPending}
              />
              {passwordForm.formState.errors.confirmPassword && (
                <p className="text-sm text-destructive">
                  {passwordForm.formState.errors.confirmPassword.message}
                </p>
              )}
            </div>

            <div className="flex gap-3">
              <Button
                type="button"
                variant="outline"
                onClick={handleBack}
                disabled={isResetPending}
              >
                <ArrowLeft className="h-4 w-4 mr-2" />
                {t('common:actions.back')}
              </Button>
              <Button
                type="submit"
                className="flex-1"
                disabled={isResetPending}
              >
                {isResetPending ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    {t('auth:resetPassword.resetting')}
                  </>
                ) : (
                  t('auth:resetPassword.reset')
                )}
              </Button>
            </div>
          </form>
        </DialogContent>
      </Dialog>
    )
  }

  // Step 4: Success
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <CheckCircle2 className="h-5 w-5 text-green-500" />
            {t('auth:resetPassword.success')}
          </DialogTitle>
          <DialogDescription>
            {t('auth:resetPassword.successMessage')}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="flex items-center justify-center py-4">
            <div className="rounded-full bg-green-100 p-3 dark:bg-green-900/20">
              <CheckCircle2 className="h-8 w-8 text-green-600 dark:text-green-400" />
            </div>
          </div>

          <Button
            className="w-full"
            onClick={() => onOpenChange(false)}
          >
            {t('auth:login.signIn')}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
