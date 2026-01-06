import { useTranslation } from 'react-i18next'
import { AlertTriangle } from 'lucide-react'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'

export function WebAuthnNotSupported() {
  const { t } = useTranslation()

  return (
    <Alert variant="destructive">
      <AlertTriangle className="h-4 w-4" />
      <AlertTitle>{t('auth:passkeys.notSupported.title')}</AlertTitle>
      <AlertDescription>
        {t('auth:passkeys.notSupported.description')}
      </AlertDescription>
    </Alert>
  )
}
