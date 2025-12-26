import { useTranslation } from 'react-i18next'
import { PageHeader } from '@/components/shared/layout'
import { AuditLogsTable } from '@/features/audit-logs'

export function AuditLogsPage() {
  const { t } = useTranslation()

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('auditLogs:title')}
        description={t('auditLogs:subtitle')}
      />

      <AuditLogsTable />
    </div>
  )
}
