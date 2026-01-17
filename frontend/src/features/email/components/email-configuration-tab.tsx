import { useTranslation } from 'react-i18next'
import { AlertCircle } from 'lucide-react'

import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Skeleton } from '@/components/ui/skeleton'
import { usePermissions } from '@/contexts/auth-context'
import { useEmailConfiguration, useUpdateEmailConfiguration } from '../hooks'
import { EmailConfigurationForm } from './email-configuration-form'

export function EmailConfigurationTab() {
  const { t } = useTranslation()
  const { hasPermission } = usePermissions()
  const canEdit = hasPermission('email:config:manage')

  const { data: configuration, isLoading, error } = useEmailConfiguration()
  const updateConfiguration = useUpdateEmailConfiguration()

  if (isLoading) {
    return (
      <div className="space-y-8">
        <div className="space-y-4">
          <Skeleton className="h-6 w-48" />
          <Skeleton className="h-px w-full" />
          <div className="space-y-4">
            <Skeleton className="h-12 w-full" />
            <Skeleton className="h-12 w-full" />
          </div>
        </div>
        <div className="space-y-4">
          <Skeleton className="h-6 w-48" />
          <Skeleton className="h-px w-full" />
          <div className="grid gap-4 sm:grid-cols-2">
            <Skeleton className="h-20 w-full" />
            <Skeleton className="h-20 w-full" />
            <Skeleton className="h-20 w-full" />
            <Skeleton className="h-20 w-full" />
          </div>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertCircle className="h-4 w-4" />
        <AlertTitle>{t('common:error')}</AlertTitle>
        <AlertDescription>{t('email:errors.loadConfiguration')}</AlertDescription>
      </Alert>
    )
  }

  if (!configuration) {
    return null
  }

  return (
    <div className="max-w-4xl">
      <EmailConfigurationForm
        configuration={configuration}
        onSubmit={updateConfiguration.mutate}
        isLoading={updateConfiguration.isPending}
        canEdit={canEdit}
      />
    </div>
  )
}
