import { useState, useMemo, useCallback } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Loader2 } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

import { useRequestMagicLink } from '../hooks/use-request-magic-link'
import { createMagicLinkSchema, type MagicLinkFormData } from '../types'
import { getErrorMessage } from '@/lib/error-utils'
import { CaptchaWidget } from './captcha-widget'
import { useCaptchaConfig } from '../hooks'

interface MagicLinkFormProps {
  onSuccess?: (email: string) => void
  defaultEmail?: string
}

export function MagicLinkForm({ onSuccess, defaultEmail = '' }: MagicLinkFormProps) {
  const { t } = useTranslation()

  // CAPTCHA state
  const [captchaToken, setCaptchaToken] = useState<string | null>(null)
  const { data: captchaConfig } = useCaptchaConfig()
  const captchaRequired = captchaConfig?.enabled && captchaConfig?.provider !== 'Disabled'

  const handleCaptchaVerify = useCallback((token: string) => {
    setCaptchaToken(token)
  }, [])

  const handleCaptchaExpire = useCallback(() => {
    setCaptchaToken(null)
  }, [])

  const { mutate: requestMagicLink, isPending, error } = useRequestMagicLink()

  const magicLinkSchema = useMemo(() => createMagicLinkSchema(t), [t])

  const form = useForm<MagicLinkFormData>({
    resolver: zodResolver(magicLinkSchema),
    defaultValues: {
      email: defaultEmail,
    },
  })

  const onSubmit = (data: MagicLinkFormData) => {
    requestMagicLink(
      {
        email: data.email,
        captchaToken: captchaToken || undefined,
      },
      {
        onSuccess: () => {
          if (onSuccess) {
            onSuccess(data.email)
          }
        },
      }
    )
  }

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      {error && (
        <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
          {getErrorMessage(error, t)}
        </div>
      )}

      <div className="space-y-2">
        <Label htmlFor="email">{t('auth:magicLink.email')}</Label>
        <Input
          id="email"
          type="email"
          autoComplete="email"
          placeholder="name@example.com"
          {...form.register('email')}
          disabled={isPending}
          autoFocus
        />
        {form.formState.errors.email && (
          <p className="text-sm text-destructive">
            {form.formState.errors.email.message}
          </p>
        )}
      </div>

      {/* CAPTCHA Widget - always visible for magic link */}
      <CaptchaWidget
        onVerify={handleCaptchaVerify}
        onExpire={handleCaptchaExpire}
        action="magic_link"
        className="flex justify-center"
      />

      <Button
        type="submit"
        className="w-full"
        disabled={isPending || (captchaRequired && !captchaToken)}
      >
        {isPending ? (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            {t('auth:magicLink.sending')}
          </>
        ) : (
          t('auth:magicLink.button')
        )}
      </Button>
    </form>
  )
}
