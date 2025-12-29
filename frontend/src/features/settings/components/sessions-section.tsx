import { useTranslation } from 'react-i18next'

import { SessionsList } from '@/features/auth/components'

export function SessionsSection() {
  const { t } = useTranslation()

  return (
    <div className="space-y-4">
      <div>
        <h3 className="text-lg font-medium">{t('sessions:title')}</h3>
        <p className="text-sm text-muted-foreground mt-1">
          {t('sessions:description')}
        </p>
      </div>

      <SessionsList />
    </div>
  )
}
