import { useTranslation } from 'react-i18next'
import { Link, useSearch } from '@tanstack/react-router'
import { Loader2, ShieldCheck, ShieldX } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { useApproveDeviceByLink } from '@/features/auth/hooks'
import { getErrorMessage } from '@/lib/error-utils'

export function ApproveDevicePage() {
  const { t } = useTranslation()
  const search = useSearch({ strict: false }) as { token?: string }
  const token = search.token

  const { data, isLoading, isError, error } = useApproveDeviceByLink(token || '')

  // No token provided
  if (!token) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <div className="w-full max-w-md space-y-6">
          <div className="rounded-lg border bg-card p-6 shadow-sm">
            <div className="flex flex-col items-center gap-4 text-center">
              <div className="rounded-full bg-destructive/10 p-3">
                <ShieldX className="h-8 w-8 text-destructive" />
              </div>
              <h1 className="text-xl font-semibold">
                {t('auth:deviceApproval.linkApproval.title')}
              </h1>
              <p className="text-sm text-muted-foreground">
                {t('auth:deviceApproval.linkApproval.error')}
              </p>
              <Button asChild className="mt-4 w-full">
                <Link to="/system/login">
                  {t('auth:deviceApproval.linkApproval.backToLogin')}
                </Link>
              </Button>
            </div>
          </div>
        </div>
      </div>
    )
  }

  // Loading state
  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <div className="w-full max-w-md space-y-6">
          <div className="rounded-lg border bg-card p-6 shadow-sm">
            <div className="flex flex-col items-center gap-4 text-center">
              <Loader2 className="h-8 w-8 animate-spin text-primary" />
              <h1 className="text-xl font-semibold">
                {t('auth:deviceApproval.linkApproval.title')}
              </h1>
              <p className="text-sm text-muted-foreground">
                {t('auth:deviceApproval.linkApproval.loading')}
              </p>
            </div>
          </div>
        </div>
      </div>
    )
  }

  // Error state
  if (isError) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <div className="w-full max-w-md space-y-6">
          <div className="rounded-lg border bg-card p-6 shadow-sm">
            <div className="flex flex-col items-center gap-4 text-center">
              <div className="rounded-full bg-destructive/10 p-3">
                <ShieldX className="h-8 w-8 text-destructive" />
              </div>
              <h1 className="text-xl font-semibold">
                {t('auth:deviceApproval.linkApproval.title')}
              </h1>
              <p className="text-sm text-muted-foreground">
                {getErrorMessage(error, t) || t('auth:deviceApproval.linkApproval.error')}
              </p>
              <Button asChild className="mt-4 w-full">
                <Link to="/system/login">
                  {t('auth:deviceApproval.linkApproval.backToLogin')}
                </Link>
              </Button>
            </div>
          </div>
        </div>
      </div>
    )
  }

  // Success state
  if (data?.success) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <div className="w-full max-w-md space-y-6">
          <div className="rounded-lg border bg-card p-6 shadow-sm">
            <div className="flex flex-col items-center gap-4 text-center">
              <div className="rounded-full bg-green-100 p-3 dark:bg-green-900/20">
                <ShieldCheck className="h-8 w-8 text-green-600 dark:text-green-400" />
              </div>
              <h1 className="text-xl font-semibold">
                {t('auth:deviceApproval.linkApproval.title')}
              </h1>
              <p className="text-sm text-muted-foreground">
                {t('auth:deviceApproval.linkApproval.success')}
              </p>
              <Button asChild className="mt-4 w-full">
                <Link to="/system/login">
                  {t('auth:deviceApproval.linkApproval.backToLogin')}
                </Link>
              </Button>
            </div>
          </div>
        </div>
      </div>
    )
  }

  // Fallback - should not reach here
  return null
}
