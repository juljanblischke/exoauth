import { useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Link } from '@tanstack/react-router'
import { Loader2 } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { PasswordInput } from '@/components/shared/form'

import { useLogin } from '../hooks/use-login'
import { createLoginSchema, type LoginFormData } from '../types'
import { getErrorMessage } from '@/lib/error-utils'

export function LoginForm() {
  const { t } = useTranslation()
  const { mutate: login, isPending, error } = useLogin()

  const loginSchema = useMemo(() => createLoginSchema(t), [t])

  const form = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
    },
  })

  const onSubmit = (data: LoginFormData) => {
    login(data)
  }

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
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
        <Label htmlFor="password">{t('auth:login.password')}</Label>
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

      <Button type="submit" className="w-full" disabled={isPending}>
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
        <Link to="/register" className="text-primary hover:underline">
          {t('auth:login.register')}
        </Link>
      </p>
    </form>
  )
}
