import { useState, useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Link, useSearch } from '@tanstack/react-router'
import { Loader2, CheckCircle2, AlertTriangle } from 'lucide-react'
import { z } from 'zod'
import type { TFunction } from 'i18next'

import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { PasswordInput } from '@/components/shared/form'
import { PasswordRequirements } from '@/features/auth/components/password-requirements'
import { useResetPassword } from '@/features/auth/hooks'
import { getErrorMessage } from '@/lib/error-utils'

// Password schema: min 12 chars, upper, lower, digit, special
const createResetPasswordSchema = (t: TFunction) =>
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

interface ResetPasswordFormData {
  newPassword: string
  confirmPassword: string
}

export function ResetPasswordPage() {
  const { t } = useTranslation()
  const search = useSearch({ strict: false }) as { token?: string }
  const token = search.token || ''

  const [isSuccess, setIsSuccess] = useState(false)
  const resetPassword = useResetPassword()

  const schema = useMemo(() => createResetPasswordSchema(t), [t])

  const form = useForm<ResetPasswordFormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      newPassword: '',
      confirmPassword: '',
    },
  })

  const password = form.watch('newPassword')

  const onSubmit = (data: ResetPasswordFormData) => {
    resetPassword.mutate(
      { token, newPassword: data.newPassword },
      {
        onSuccess: () => {
          setIsSuccess(true)
        },
      }
    )
  }

  const isPending = resetPassword.isPending

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
                {t('errors:codes.AUTH_INVITE_INVALID')}
              </p>
              <Button asChild className="w-full mt-4">
                <Link to="/system/login">{t('auth:forgotPassword.backToLogin')}</Link>
              </Button>
            </div>
          </div>
        </div>
      </div>
    )
  }

  // Success state
  if (isSuccess) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <div className="w-full max-w-md space-y-6">
          <div className="rounded-lg border bg-card p-6 shadow-sm">
            <div className="flex flex-col items-center gap-4 text-center">
              <div className="rounded-full bg-green-100 p-3 dark:bg-green-900/20">
                <CheckCircle2 className="h-8 w-8 text-green-600 dark:text-green-400" />
              </div>
              <h1 className="text-xl font-semibold">
                {t('auth:resetPassword.success')}
              </h1>
              <p className="text-sm text-muted-foreground">
                {t('auth:resetPassword.successMessage')}
              </p>
              <Button asChild className="w-full mt-4">
                <Link to="/system/login">{t('auth:login.signIn')}</Link>
              </Button>
            </div>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <div className="w-full max-w-md space-y-6">
        <div className="space-y-2 text-center">
          <h1 className="text-3xl font-bold tracking-tight">
            {t('auth:resetPassword.title')}
          </h1>
          <p className="text-muted-foreground">
            {t('auth:resetPassword.subtitle')}
          </p>
        </div>

        <div className="rounded-lg border bg-card p-6 shadow-sm">
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            {resetPassword.isError && (
              <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                {getErrorMessage(resetPassword.error, t)}
              </div>
            )}

            <div className="space-y-2">
              <Label htmlFor="newPassword">
                {t('auth:resetPassword.newPassword')}
              </Label>
              <PasswordInput
                id="newPassword"
                autoComplete="new-password"
                {...form.register('newPassword')}
                disabled={isPending}
              />
              {form.formState.errors.newPassword && (
                <p className="text-sm text-destructive">
                  {form.formState.errors.newPassword.message}
                </p>
              )}
            </div>

            <PasswordRequirements password={password} />

            <div className="space-y-2">
              <Label htmlFor="confirmPassword">
                {t('auth:resetPassword.confirmPassword')}
              </Label>
              <PasswordInput
                id="confirmPassword"
                autoComplete="new-password"
                {...form.register('confirmPassword')}
                disabled={isPending}
              />
              {form.formState.errors.confirmPassword && (
                <p className="text-sm text-destructive">
                  {form.formState.errors.confirmPassword.message}
                </p>
              )}
            </div>

            <Button type="submit" className="w-full" disabled={isPending}>
              {isPending ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  {t('auth:resetPassword.resetting')}
                </>
              ) : (
                t('auth:resetPassword.reset')
              )}
            </Button>

            <p className="text-center text-sm text-muted-foreground">
              <Link to="/system/login" className="text-primary hover:underline">
                {t('auth:forgotPassword.backToLogin')}
              </Link>
            </p>
          </form>
        </div>
      </div>
    </div>
  )
}
