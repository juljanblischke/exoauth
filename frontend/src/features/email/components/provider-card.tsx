import { useTranslation } from 'react-i18next'
import {
  MoreHorizontal,
  Pencil,
  Trash2,
  TestTube2,
  RotateCcw,
  ChevronUp,
  ChevronDown,
  GripVertical,
} from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { EmailProviderTypeBadge } from './email-provider-type-badge'
import { EmailProviderStatusBadge } from './email-provider-status-badge'
import type { EmailProviderDto } from '../types'
import { cn } from '@/lib/utils'

interface ProviderCardProps {
  provider: EmailProviderDto
  onEdit?: (provider: EmailProviderDto) => void
  onDelete?: (provider: EmailProviderDto) => void
  onTest?: (provider: EmailProviderDto) => void
  onResetCircuitBreaker?: (provider: EmailProviderDto) => void
  onMoveUp?: (provider: EmailProviderDto) => void
  onMoveDown?: (provider: EmailProviderDto) => void
  isFirst?: boolean
  isLast?: boolean
  canManage?: boolean
  className?: string
}

export function ProviderCard({
  provider,
  onEdit,
  onDelete,
  onTest,
  onResetCircuitBreaker,
  onMoveUp,
  onMoveDown,
  isFirst,
  isLast,
  canManage = false,
  className,
}: ProviderCardProps) {
  const { t } = useTranslation()

  return (
    <Card className={cn('transition-shadow hover:shadow-md', className)}>
      <CardContent className="p-4">
        <div className="flex items-center gap-4">
          {/* Drag handle / priority indicator */}
          {canManage && (
            <div className="flex flex-col gap-1">
              <Button
                variant="ghost"
                size="icon"
                className="h-6 w-6"
                onClick={() => onMoveUp?.(provider)}
                disabled={isFirst}
              >
                <ChevronUp className="h-4 w-4" />
              </Button>
              <GripVertical className="h-4 w-4 text-muted-foreground mx-auto" />
              <Button
                variant="ghost"
                size="icon"
                className="h-6 w-6"
                onClick={() => onMoveDown?.(provider)}
                disabled={isLast}
              >
                <ChevronDown className="h-4 w-4" />
              </Button>
            </div>
          )}

          {/* Priority number */}
          <div className="flex h-10 w-10 items-center justify-center rounded-full bg-muted text-lg font-semibold">
            {provider.priority}
          </div>

          {/* Provider info */}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <h3 className="font-semibold truncate">{provider.name}</h3>
              <EmailProviderTypeBadge type={provider.type} />
              <EmailProviderStatusBadge
                isEnabled={provider.isEnabled}
                isCircuitBreakerOpen={provider.isCircuitBreakerOpen}
              />
            </div>
            
          </div>

          {/* Stats */}
          <div className="hidden md:flex items-center gap-6 text-sm text-muted-foreground">
            <div className="text-center">
              <div className="font-semibold text-foreground">{provider.totalSent}</div>
              <div className="text-xs">{t('email:providers.stats.sent')}</div>
            </div>
            <div className="text-center">
              <div className="font-semibold text-foreground">{provider.failureCount}</div>
              <div className="text-xs">{t('email:providers.stats.failed')}</div>
            </div>
          </div>

          {/* Actions */}
          {canManage && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="icon">
                  <MoreHorizontal className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onClick={() => onTest?.(provider)}>
                  <TestTube2 className="h-4 w-4 mr-2" />
                  {t('email:providers.actions.test')}
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => onEdit?.(provider)}>
                  <Pencil className="h-4 w-4 mr-2" />
                  {t('common:actions.edit')}
                </DropdownMenuItem>
                {provider.isCircuitBreakerOpen && (
                  <DropdownMenuItem onClick={() => onResetCircuitBreaker?.(provider)}>
                    <RotateCcw className="h-4 w-4 mr-2" />
                    {t('email:providers.actions.resetCircuitBreaker')}
                  </DropdownMenuItem>
                )}
                <DropdownMenuSeparator />
                <DropdownMenuItem
                  onClick={() => onDelete?.(provider)}
                  className="text-destructive focus:text-destructive"
                >
                  <Trash2 className="h-4 w-4 mr-2" />
                  {t('common:actions.delete')}
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </div>
      </CardContent>
    </Card>
  )
}
