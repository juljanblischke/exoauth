import { useEffect } from 'react'
import { GoogleReCaptchaProvider } from 'react-google-recaptcha-v3'
import { useTranslation } from 'react-i18next'
import { AlertCircle } from 'lucide-react'
import { Skeleton } from '@/components/ui/skeleton'
import { useCaptchaConfig } from '../hooks/use-captcha-config'
import { TurnstileCaptcha } from './turnstile-captcha'
import { RecaptchaCaptcha } from './recaptcha-captcha'
import { HCaptchaCaptcha } from './hcaptcha-captcha'
import type { CaptchaWidgetProps } from '../types/captcha'

export function CaptchaWidget({
  onVerify,
  onError,
  onExpire,
  onLoad,
  action,
  className,
}: CaptchaWidgetProps) {
  const { t } = useTranslation()
  const { data: config, isLoading, error } = useCaptchaConfig()

  // Normalize provider to lowercase for comparison
  const provider = config?.provider?.toLowerCase()
  
  // Check if CAPTCHA is disabled, unknown provider, or missing siteKey
  const knownProviders = ['turnstile', 'recaptcha', 'hcaptcha']
  const isDisabled = !isLoading && !error && (
    !config?.enabled || 
    !config?.siteKey ||  // No siteKey = treat as disabled
    provider === 'disabled' ||
    (provider && !knownProviders.includes(provider))
  )

  // Auto-verify when disabled (must be in useEffect to avoid setState during render)
  useEffect(() => {
    if (isDisabled) {
      onVerify('')
    }
  }, [isDisabled, onVerify])

  // Loading state
  if (isLoading) {
    return (
      <div className={className}>
        <Skeleton className="h-[65px] w-full max-w-[300px]" />
        <p className="text-xs text-muted-foreground mt-1">
          {t('auth:captcha.loading')}
        </p>
      </div>
    )
  }

  // Error state - graceful degradation
  if (error) {
    return (
      <div className={className}>
        <div className="flex items-center gap-2 text-xs text-destructive">
          <AlertCircle className="h-3 w-3" />
          <span>{t('auth:captcha.error')}</span>
        </div>
      </div>
    )
  }

  // Disabled state - no CAPTCHA needed (auto-verify handled by useEffect above)
  if (isDisabled || !config) {
    return null
  }

  // Render provider-specific widget
  switch (provider) {
    case 'turnstile':
      return (
        <TurnstileCaptcha
          onVerify={onVerify}
          onError={onError}
          onExpire={onExpire}
          onLoad={onLoad}
          className={className}
        />
      )

    case 'recaptcha':
      return (
        <GoogleReCaptchaProvider reCaptchaKey={config.siteKey}>
          <RecaptchaCaptcha
            onVerify={onVerify}
            onError={onError}
            action={action}
            className={className}
          />
        </GoogleReCaptchaProvider>
      )

    case 'hcaptcha':
      return (
        <HCaptchaCaptcha
          onVerify={onVerify}
          onError={onError}
          onExpire={onExpire}
          onLoad={onLoad}
          className={className}
        />
      )

    default:
      // Unknown provider - graceful degradation (treated as disabled)
      return null
  }
}
