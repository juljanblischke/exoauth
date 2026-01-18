import { useTranslation } from 'react-i18next'
import { Link } from '@tanstack/react-router'
import { Mail } from 'lucide-react'

import { Button } from '@/components/ui/button'

interface MagicLinkSentProps {
  email: string
}

export function MagicLinkSent({ email }: MagicLinkSentProps) {
  const { t } = useTranslation()

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <div className="w-full max-w-md space-y-6">
        <div className="rounded-lg border bg-card p-6 shadow-sm">
          <div className="flex flex-col items-center gap-4 text-center">
            <div className="rounded-full bg-blue-100 p-3 dark:bg-blue-900/20">
              <Mail className="h-8 w-8 text-blue-600 dark:text-blue-400" />
            </div>
            <h1 className="text-xl font-semibold">
              {t('auth:magicLink.sent')}
            </h1>
            <p className="text-sm text-muted-foreground">
              {t('auth:magicLink.sentMessage', { email })}
            </p>
            <Button asChild className="w-full mt-4">
              <Link to="/login">{t('auth:forgotPassword.backToLogin')}</Link>
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
