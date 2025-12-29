import { useState, useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Loader2 } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { PasswordInput } from '@/components/shared/form'

import { useAcceptInvite } from '../hooks/use-accept-invite'
import { createAcceptInviteSchema, type AcceptInviteFormData } from '../types'
import { PasswordRequirements } from './password-requirements'
import { getErrorMessage } from '@/lib/error-utils'
import { getDeviceInfo } from '@/lib/device'

interface AcceptInviteFormProps {
  token: string
}

export function AcceptInviteForm({ token }: AcceptInviteFormProps) {
  const { t, i18n } = useTranslation()
  const { mutate: acceptInvite, isPending, error } = useAcceptInvite()
  const [password, setPassword] = useState('')

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
    </form>
  )
}
