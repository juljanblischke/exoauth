import { Turnstile } from '@marsidev/react-turnstile'
import { useTranslation } from 'react-i18next'
import { Skeleton } from '@/components/ui/skeleton'
import { useCaptchaConfig } from '../hooks/use-captcha-config'
import type { CaptchaWidgetProps } from '../types/captcha'

export function TurnstileCaptcha({
  onVerify,
  onError,
  onExpire,
  onLoad,
  className,
}: Omit<CaptchaWidgetProps, 'action'>) {
  const { t } = useTranslation()
  const { data: config, isLoading } = useCaptchaConfig()

  if (isLoading) {
    return (
      <div className={className}>
        <Skeleton className="h-[65px] w-[300px]" />
        <p className="text-xs text-muted-foreground mt-1">
          {t('auth:captcha.loading')}
        </p>
      </div>
    )
  }

  if (!config?.siteKey || config.provider?.toLowerCase() !== 'turnstile') {
    return null
  }

  return (
    <div className={className}>
      <Turnstile
        siteKey={config.siteKey}
        onSuccess={onVerify}
        onError={() => onError?.(t('auth:captcha.error'))}
        onExpire={() => onExpire?.()}
        onWidgetLoad={() => onLoad?.()}
        options={{
          theme: 'auto',
          size: 'normal',
        }}
      />
    </div>
  )
}
