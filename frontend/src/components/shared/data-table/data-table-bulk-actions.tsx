import { useTranslation } from 'react-i18next'
import { X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import type { BulkAction } from '@/types/table'

interface DataTableBulkActionsProps<TData> {
  selectedCount: number
  actions: BulkAction<TData>[]
  selectedRows: TData[]
  onClearSelection: () => void
}

export function DataTableBulkActions<TData>({
  selectedCount,
  actions,
  selectedRows,
  onClearSelection,
}: DataTableBulkActionsProps<TData>) {
  const { t } = useTranslation('common')

  if (selectedCount === 0) {
    return null
  }

  return (
    <div className="fixed bottom-4 left-1/2 z-50 -translate-x-1/2">
      <div className="flex items-center gap-2 rounded-lg border bg-background px-4 py-3 shadow-lg">
        <span className="text-sm font-medium">
          {t('table.selected', { count: selectedCount })}
        </span>

        <div className="mx-2 h-4 w-px bg-border" />

        <div className="flex items-center gap-2">
          {actions.map((action) => (
            <Button
              key={action.label}
              variant={action.variant || 'default'}
              size="sm"
              onClick={() => action.onClick(selectedRows)}
              className={cn(
                'gap-2',
                action.variant === 'destructive' &&
                  'bg-destructive text-destructive-foreground hover:bg-destructive/90'
              )}
            >
              {action.icon}
              {action.label}
            </Button>
          ))}
        </div>

        <div className="mx-2 h-4 w-px bg-border" />

        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8"
          onClick={onClearSelection}
        >
          <X className="h-4 w-4" />
          <span className="sr-only">{t('actions.close')}</span>
        </Button>
      </div>
    </div>
  )
}
