import { useTranslation } from 'react-i18next'
import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import {
  MoreHorizontal,
  Pencil,
  Trash2,
  TestTube2,
  RotateCcw,
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
  onViewDetails?: (provider: EmailProviderDto) => void
  canManage?: boolean
  className?: string
}

export function ProviderCard({
  provider,
  onEdit,
  onDelete,
  onTest,
  onResetCircuitBreaker,
  canManage = false,
  className,
}: ProviderCardProps) {
  const { t } = useTranslation()

  return (
    <Card className={cn('transition-shadow hover:shadow-md', className)}>
      <CardContent className="p-4">
        <div className="flex items-center gap-4">
          {/* Priority number */}
          <div className="flex h-10 w-10 items-center justify-center rounded-full bg-muted text-lg font-semibold">
            {provider.priority + 1}
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

// Sortable wrapper for drag and drop
interface SortableProviderCardProps extends Omit<ProviderCardProps, 'className'> {
  provider: EmailProviderDto
  onViewDetails?: (provider: EmailProviderDto) => void
}

export function SortableProviderCard(props: SortableProviderCardProps) {
  const { provider, canManage, onViewDetails } = props
  const { t } = useTranslation()

  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: provider.id })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  }

  const handleCardClick = (e: React.MouseEvent) => {
    // Don't trigger if clicking on buttons or drag handle
    const target = e.target as HTMLElement
    if (target.closest('button') || target.closest('[data-drag-handle]')) {
      return
    }
    onViewDetails?.(provider)
  }

  return (
    <div ref={setNodeRef} style={style} className={cn(isDragging && 'opacity-50')}>
      <Card
        className={cn(
          'transition-shadow hover:shadow-md cursor-pointer',
          isDragging && 'shadow-lg'
        )}
        onClick={handleCardClick}
      >
        <CardContent className="p-4">
          <div className="flex items-center gap-4">
            {/* Drag handle */}
            {canManage && (
              <button
                {...attributes}
                {...listeners}
                data-drag-handle
                className="cursor-grab active:cursor-grabbing p-1 -m-1 hover:bg-muted rounded touch-none"
                aria-label={t('email:providers.dragHandle')}
              >
                <GripVertical className="h-5 w-5 text-muted-foreground" />
              </button>
            )}

            {/* Priority number */}
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-muted text-lg font-semibold">
              {provider.priority + 1}
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
                  <DropdownMenuItem onClick={() => props.onTest?.(provider)}>
                    <TestTube2 className="h-4 w-4 mr-2" />
                    {t('email:providers.actions.test')}
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => props.onEdit?.(provider)}>
                    <Pencil className="h-4 w-4 mr-2" />
                    {t('common:actions.edit')}
                  </DropdownMenuItem>
                  {provider.isCircuitBreakerOpen && (
                    <DropdownMenuItem onClick={() => props.onResetCircuitBreaker?.(provider)}>
                      <RotateCcw className="h-4 w-4 mr-2" />
                      {t('email:providers.actions.resetCircuitBreaker')}
                    </DropdownMenuItem>
                  )}
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onClick={() => props.onDelete?.(provider)}
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
    </div>
  )
}
