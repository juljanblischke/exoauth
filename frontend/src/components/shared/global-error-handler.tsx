import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'

export function GlobalErrorHandler() {
  const { t } = useTranslation()

  useEffect(() => {
    const handleRateLimited = (event: CustomEvent<{ retryAfter?: number }>) => {
      const retryAfter = event.detail?.retryAfter
      const message = retryAfter
        ? t('errors:rateLimited.withRetry', { seconds: retryAfter })
        : t('errors:rateLimited.message')
      
      toast.error(t('errors:rateLimited.title'), {
        description: message,
      })
    }

    const handleIpBlacklisted = () => {
      toast.error(t('errors:ipBlacklisted.title'), {
        description: t('errors:ipBlacklisted.message'),
      })
    }

    window.addEventListener('api:rate-limited', handleRateLimited as EventListener)
    window.addEventListener('api:ip-blacklisted', handleIpBlacklisted)

    return () => {
      window.removeEventListener('api:rate-limited', handleRateLimited as EventListener)
      window.removeEventListener('api:ip-blacklisted', handleIpBlacklisted)
    }
  }, [t])

  return null
}
