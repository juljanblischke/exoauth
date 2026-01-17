import { useTranslation } from 'react-i18next'
import { PageHeader } from '@/components/shared/layout'
import { IpRestrictionsTable } from '@/features/ip-restrictions'

export function IpRestrictionsPage() {
  const { t } = useTranslation()

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('ipRestrictions:title')}
        description={t('ipRestrictions:subtitle')}
      />

      <IpRestrictionsTable />
    </div>
  )
}
