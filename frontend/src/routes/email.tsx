import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { PageHeader } from '@/components/shared/layout'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { usePermissions } from '@/contexts/auth-context'
import { ProviderList } from '@/features/email/components/provider-list'
import { EmailConfigurationTab } from '@/features/email/components/email-configuration-tab'
import { EmailLogsTab } from '@/features/email/components/email-logs-tab'
import { EmailDlqTab } from '@/features/email/components/email-dlq-tab'
import { EmailAnnouncementsTab } from '@/features/email/components/email-announcements-tab'

type TabValue = 'providers' | 'configuration' | 'logs' | 'dlq' | 'announcements'

interface TabConfig {
  value: TabValue
  label: string
  permission: string
}

const TABS: TabConfig[] = [
  { value: 'providers', label: 'tabs.providers', permission: 'email:providers:read' },
  { value: 'configuration', label: 'tabs.configuration', permission: 'email:config:read' },
  { value: 'logs', label: 'tabs.logs', permission: 'email:logs:read' },
  { value: 'dlq', label: 'tabs.dlq', permission: 'email:dlq:manage' },
  { value: 'announcements', label: 'tabs.announcements', permission: 'email:announcements:read' },
]

export function EmailPage() {
  const { t } = useTranslation('email')
  const { hasPermission } = usePermissions()

  // Filter tabs based on permissions
  const visibleTabs = TABS.filter(tab => hasPermission(tab.permission))

  // Set default tab to first visible tab
  const [activeTab, setActiveTab] = useState<TabValue>(visibleTabs[0]?.value || 'providers')

  // If no tabs are visible, show forbidden message
  if (visibleTabs.length === 0) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-muted-foreground">{t('common:errors.forbidden')}</p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <PageHeader title={t('title')} description={t('subtitle')} />

      <Tabs
        value={activeTab}
        onValueChange={value => setActiveTab(value as TabValue)}
        className="space-y-4"
      >
        <TabsList>
          {visibleTabs.map(tab => (
            <TabsTrigger key={tab.value} value={tab.value}>
              {t(tab.label)}
            </TabsTrigger>
          ))}
        </TabsList>

        {hasPermission('email:providers:read') && (
          <TabsContent value="providers" className="space-y-4">
            <ProviderList />
          </TabsContent>
        )}

        {hasPermission('email:config:read') && (
          <TabsContent value="configuration" className="space-y-4">
            <EmailConfigurationTab />
          </TabsContent>
        )}

        {hasPermission('email:logs:read') && (
          <TabsContent value="logs" className="space-y-4">
            <EmailLogsTab />
          </TabsContent>
        )}

        {hasPermission('email:dlq:manage') && (
          <TabsContent value="dlq" className="space-y-4">
            <EmailDlqTab />
          </TabsContent>
        )}

        {hasPermission('email:announcements:read') && (
          <TabsContent value="announcements" className="space-y-4">
            <EmailAnnouncementsTab />
          </TabsContent>
        )}
      </Tabs>
    </div>
  )
}
