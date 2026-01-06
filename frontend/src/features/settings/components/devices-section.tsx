import { useTranslation } from 'react-i18next'
import { DevicesList } from '@/features/auth/components'

export function DevicesSection() {
  const { t } = useTranslation()

  return (
    <div className="space-y-4">
      <div>
        <h3 className="text-lg font-medium">{t('auth:devices.title')}</h3>
        <p className="text-sm text-muted-foreground mt-1">
          {t('auth:devices.description')}
        </p>
      </div>

      <DevicesList />
    </div>
  )
}
