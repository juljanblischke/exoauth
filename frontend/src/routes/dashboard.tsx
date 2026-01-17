import { useTranslation } from 'react-i18next'
import { useAuth } from '@/contexts/auth-context'

export function DashboardPage() {
  const { t } = useTranslation()
  const { user } = useAuth()

  const welcomeMessage = user?.firstName
    ? t('common:dashboard.welcome', { name: user.firstName })
    : t('common:dashboard.welcomeGeneric')

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">{t('common:dashboard.title')}</h1>
        <p className="text-muted-foreground">{welcomeMessage}</p>
      </div>
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <div className="rounded-lg border bg-card p-6">
          <h3 className="text-sm font-medium text-muted-foreground">
            {t('common:dashboard.stats.totalUsers')}
          </h3>
          <p className="text-2xl font-bold">--</p>
        </div>
        <div className="rounded-lg border bg-card p-6">
          <h3 className="text-sm font-medium text-muted-foreground">
            {t('common:dashboard.stats.activeSessions')}
          </h3>
          <p className="text-2xl font-bold">--</p>
        </div>
        <div className="rounded-lg border bg-card p-6">
          <h3 className="text-sm font-medium text-muted-foreground">
            {t('common:dashboard.stats.organizations')}
          </h3>
          <p className="text-2xl font-bold">--</p>
        </div>
        <div className="rounded-lg border bg-card p-6">
          <h3 className="text-sm font-medium text-muted-foreground">
            {t('common:dashboard.stats.projects')}
          </h3>
          <p className="text-2xl font-bold">--</p>
        </div>
      </div>
    </div>
  )
}
