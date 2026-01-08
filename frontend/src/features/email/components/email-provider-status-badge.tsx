import { useTranslation } from 'react-i18next'
import { StatusBadge } from '@/components/shared'

interface EmailProviderStatusBadgeProps {
  isEnabled: boolean
  isCircuitBreakerOpen?: boolean
  className?: string
}

export function EmailProviderStatusBadge({
  isEnabled,
  isCircuitBreakerOpen,
  className,
}: EmailProviderStatusBadgeProps) {
  const { t } = useTranslation()

  if (isCircuitBreakerOpen) {
    return (
      <StatusBadge
        status="warning"
        label={t('email:providers.status.circuitOpen')}
        className={className}
      />
    )
  }

  if (isEnabled) {
    return (
      <StatusBadge
        status="success"
        label={t('email:providers.status.active')}
        className={className}
      />
    )
  }

  return (
    <StatusBadge
      status="neutral"
      label={t('email:providers.status.inactive')}
      className={className}
    />
  )
}
