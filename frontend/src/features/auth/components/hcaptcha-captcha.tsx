import { useRef } from 'react'
import HCaptcha from '@hcaptcha/react-hcaptcha'
import { useTranslation } from 'react-i18next'
import { Skeleton } from '@/components/ui/skeleton'
import { useCaptchaConfig } from '../hooks/use-captcha-config'
import type { CaptchaWidgetProps } from '../types/captcha'

export function HCaptchaCaptcha({
  onVerify,
  onError,
  onExpire,
  onLoad,
  className,
}: Omit<CaptchaWidgetProps, 'action'>) {
  const { t } = useTranslation()
  const { data: config, isLoading } = useCaptchaConfig()
  const captchaRef = useRef<HCaptcha>(null)

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

  if (!config?.siteKey || config.provider !== 'HCaptcha') {
    return null
  }

  return (
    <div className={className}>
      <HCaptcha
        ref={captchaRef}
        sitekey={config.siteKey}
        onVerify={onVerify}
        onError={() => onError?.(t('auth:captcha.error'))}
        onExpire={() => onExpire?.()}
        onLoad={() => onLoad?.()}
        theme="light"
      />
    </div>
  )
}
