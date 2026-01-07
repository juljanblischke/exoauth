import { useEffect, useCallback } from 'react'
import { useGoogleReCaptcha } from 'react-google-recaptcha-v3'
import { useTranslation } from 'react-i18next'
import { Loader2, CheckCircle2 } from 'lucide-react'
import type { CaptchaWidgetProps } from '../types/captcha'

interface RecaptchaCaptchaProps extends CaptchaWidgetProps {
  executeOnMount?: boolean
}

export function RecaptchaCaptcha({
  onVerify,
  onError,
  action = 'submit',
  className,
  executeOnMount = true,
}: RecaptchaCaptchaProps) {
  const { t } = useTranslation()
  const { executeRecaptcha } = useGoogleReCaptcha()

  const handleExecute = useCallback(async () => {
    if (!executeRecaptcha) {
      onError?.(t('auth:captcha.error'))
      return
    }

    try {
      const token = await executeRecaptcha(action)
      onVerify(token)
    } catch {
      onError?.(t('auth:captcha.error'))
    }
  }, [executeRecaptcha, action, onVerify, onError, t])

  useEffect(() => {
    if (executeOnMount && executeRecaptcha) {
      handleExecute()
    }
  }, [executeOnMount, executeRecaptcha, handleExecute])

  // reCAPTCHA v3 is invisible - show a subtle indicator
  return (
    <div className={className}>
      <div className="flex items-center gap-2 text-xs text-muted-foreground">
        {executeRecaptcha ? (
          <>
            <CheckCircle2 className="h-3 w-3 text-green-500" />
            <span>{t('auth:captcha.protected')}</span>
          </>
        ) : (
          <>
            <Loader2 className="h-3 w-3 animate-spin" />
            <span>{t('auth:captcha.loading')}</span>
          </>
        )}
      </div>
    </div>
  )
}
