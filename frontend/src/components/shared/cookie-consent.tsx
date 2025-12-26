import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { X } from 'lucide-react'
import { cn } from '@/lib/utils'

const COOKIE_CONSENT_KEY = 'exoauth-cookie-consent'

type ConsentStatus = 'pending' | 'accepted' | 'rejected'

interface CookieConsentProps {
  className?: string
}

export function CookieConsent({ className }: CookieConsentProps) {
  const { t } = useTranslation('common')
  const [status, setStatus] = useState<ConsentStatus>('pending')
  const [isVisible, setIsVisible] = useState(false)

  useEffect(() => {
    const stored = localStorage.getItem(COOKIE_CONSENT_KEY)
    if (stored === 'accepted' || stored === 'rejected') {
      setStatus(stored as ConsentStatus)
    } else {
      setIsVisible(true)
    }
  }, [])

  const handleAccept = () => {
    localStorage.setItem(COOKIE_CONSENT_KEY, 'accepted')
    setStatus('accepted')
    setIsVisible(false)
  }

  const handleReject = () => {
    localStorage.setItem(COOKIE_CONSENT_KEY, 'rejected')
    setStatus('rejected')
    setIsVisible(false)
  }

  const handleDismiss = () => {
    setIsVisible(false)
  }

  if (!isVisible || status !== 'pending') {
    return null
  }

  return (
    <div
      className={cn(
        'fixed bottom-0 left-0 right-0 z-50 border-t bg-background p-4 shadow-lg md:bottom-4 md:left-4 md:right-auto md:max-w-md md:rounded-lg md:border',
        className
      )}
    >
      <div className="flex items-start gap-4">
        <div className="flex-1">
          <h3 className="text-sm font-semibold">{t('cookies.title')}</h3>
          <p className="mt-1 text-sm text-muted-foreground">
            {t('cookies.description')}
          </p>
          <div className="mt-4 flex flex-wrap gap-2">
            <Button size="sm" onClick={handleAccept}>
              {t('cookies.accept')}
            </Button>
            <Button size="sm" variant="outline" onClick={handleReject}>
              {t('cookies.reject')}
            </Button>
            <Button size="sm" variant="ghost" asChild>
              <a href="/privacy" target="_blank" rel="noopener noreferrer">
                {t('cookies.learnMore')}
              </a>
            </Button>
          </div>
        </div>
        <Button
          size="icon"
          variant="ghost"
          className="h-6 w-6 shrink-0"
          onClick={handleDismiss}
        >
          <X className="h-4 w-4" />
          <span className="sr-only">{t('actions.close')}</span>
        </Button>
      </div>
    </div>
  )
}
