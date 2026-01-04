import { useTranslation } from 'react-i18next'
import { Shield, Globe } from 'lucide-react'

import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { PageHeader } from '@/components/shared/layout'
import { LanguageSettings, MfaSection, SessionsSection } from '@/features/settings'
import { TrustedDevicesSection } from '@/features/auth/components/trusted-devices-section'

export function SettingsPage() {
  const { t } = useTranslation()

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('settings:title')}
        description={t('settings:description')}
      />

      <Tabs defaultValue="security" className="space-y-6">
        <TabsList>
          <TabsTrigger value="security" className="gap-2">
            <Shield className="h-4 w-4" />
            {t('settings:tabs.security')}
          </TabsTrigger>
          <TabsTrigger value="language" className="gap-2">
            <Globe className="h-4 w-4" />
            {t('settings:tabs.language')}
          </TabsTrigger>
        </TabsList>

        <TabsContent value="security" className="space-y-6">
          {/* MFA Section */}
          <div className="rounded-lg border bg-card p-6">
            <MfaSection />
          </div>

          {/* Sessions Section */}
          <div className="rounded-lg border bg-card p-6">
            <SessionsSection />
          </div>

          {/* Trusted Devices Section */}
          <div className="rounded-lg border bg-card p-6">
            <TrustedDevicesSection />
          </div>
        </TabsContent>

        <TabsContent value="language" className="space-y-6">
          <LanguageSettings />
        </TabsContent>
      </Tabs>
    </div>
  )
}
