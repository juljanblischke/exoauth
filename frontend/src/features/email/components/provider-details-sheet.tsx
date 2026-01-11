import { useTranslation } from 'react-i18next'
import {
  Mail,
  Clock,
  Activity,
  Settings,
  AlertTriangle,
  CheckCircle,
  XCircle,
  TrendingUp,
} from 'lucide-react'

import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import { RelativeTime } from '@/components/shared/relative-time'
import { EmailProviderTypeBadge } from './email-provider-type-badge'
import { EmailProviderStatusBadge } from './email-provider-status-badge'
import type { EmailProviderDto } from '../types'

interface ProviderDetailsSheetProps {
  provider: EmailProviderDto | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function ProviderDetailsSheet({
  provider,
  open,
  onOpenChange,
}: ProviderDetailsSheetProps) {
  const { t } = useTranslation()

  if (!provider) return null

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-lg flex flex-col p-0 overflow-hidden">
        <SheetHeader className="sr-only">
          <SheetTitle>{provider.name}</SheetTitle>
          <SheetDescription>{t('email:providers.details.description')}</SheetDescription>
        </SheetHeader>

        {/* Header */}
        <div className="p-6 pb-4 border-b space-y-4">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
              <Mail className="h-5 w-5 text-primary" />
            </div>
            <div>
              <h2 className="font-semibold text-lg">{provider.name}</h2>
              <div className="flex items-center gap-2 mt-1">
                <EmailProviderTypeBadge type={provider.type} />
                <EmailProviderStatusBadge
                  isEnabled={provider.isEnabled}
                  isCircuitBreakerOpen={provider.isCircuitBreakerOpen}
                />
              </div>
            </div>
          </div>
        </div>

        {/* Content */}
        <ScrollArea className="flex-1 min-h-0">
          <div className="p-6 space-y-6">
            {/* Statistics Section */}
            <div className="space-y-3">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <TrendingUp className="h-4 w-4" />
                {t('email:providers.details.statistics')}
              </div>
              <div className="grid grid-cols-2 gap-4 pl-6">
                <div className="space-y-1">
                  <div className="flex items-center gap-2 text-sm text-muted-foreground">
                    <CheckCircle className="h-4 w-4 text-green-500" />
                    {t('email:providers.stats.sent')}
                  </div>
                  <p className="text-2xl font-semibold">{provider.totalSent}</p>
                </div>
                <div className="space-y-1">
                  <div className="flex items-center gap-2 text-sm text-muted-foreground">
                    <XCircle className="h-4 w-4 text-destructive" />
                    {t('email:providers.stats.failed')}
                  </div>
                  <p className="text-2xl font-semibold">{provider.totalFailed}</p>
                </div>
              </div>
              <div className="pl-6">
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">{t('email:providers.details.successRate')}</span>
                  <span className="font-medium">{(provider.successRate * 100).toFixed(1)}%</span>
                </div>
              </div>
            </div>

            <Separator />

            {/* Configuration Section */}
            <div className="space-y-3">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <Settings className="h-4 w-4" />
                {t('email:providers.details.configuration')}
              </div>
              <div className="pl-6 space-y-2">
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">{t('email:providers.details.priority')}</span>
                  <span className="font-medium">{provider.priority + 1}</span>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">{t('email:providers.details.enabled')}</span>
                  <span className="font-medium">
                    {provider.isEnabled ? t('common:status.enabled') : t('common:status.disabled')}
                  </span>
                </div>
              </div>
            </div>

            <Separator />

            {/* Health Section */}
            <div className="space-y-3">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <Activity className="h-4 w-4" />
                {t('email:providers.details.health')}
              </div>
              <div className="pl-6 space-y-2">
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">{t('email:providers.details.failureCount')}</span>
                  <span className="font-medium">{provider.failureCount}</span>
                </div>
                {provider.isCircuitBreakerOpen && (
                  <div className="flex items-center gap-2 p-3 bg-destructive/10 rounded-md text-sm">
                    <AlertTriangle className="h-4 w-4 text-destructive" />
                    <span className="text-destructive">
                      {t('email:providers.circuitBreakerOpen')}
                    </span>
                  </div>
                )}
                {provider.circuitBreakerOpenUntil && (
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">{t('email:providers.details.openUntil')}</span>
                    <RelativeTime date={provider.circuitBreakerOpenUntil} />
                  </div>
                )}
              </div>
            </div>

            <Separator />

            {/* Timestamps Section */}
            <div className="space-y-3">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <Clock className="h-4 w-4" />
                {t('email:providers.details.timestamps')}
              </div>
              <div className="pl-6 space-y-2">
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">{t('email:providers.details.createdAt')}</span>
                  <RelativeTime date={provider.createdAt} />
                </div>
                {provider.updatedAt && (
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">{t('email:providers.details.updatedAt')}</span>
                    <RelativeTime date={provider.updatedAt} />
                  </div>
                )}
                {provider.lastSuccessAt && (
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">{t('email:providers.details.lastSuccess')}</span>
                    <RelativeTime date={provider.lastSuccessAt} />
                  </div>
                )}
                {provider.lastFailureAt && (
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">{t('email:providers.details.lastFailure')}</span>
                    <RelativeTime date={provider.lastFailureAt} />
                  </div>
                )}
              </div>
            </div>
          </div>
        </ScrollArea>
      </SheetContent>
    </Sheet>
  )
}
