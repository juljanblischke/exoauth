import { MoreHorizontal } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { cn } from '@/lib/utils'
import { UserAvatar } from '../user-avatar'
import type { RowAction } from '@/types/table'

interface DataTableCardProps<TData> {
  data: TData
  primaryField: keyof TData
  secondaryField?: keyof TData
  tertiaryFields?: Array<{
    key: keyof TData
    label?: string
    render?: (value: unknown) => React.ReactNode
  }>
  avatar?: { name?: string; email?: string; imageUrl?: string }
  actions?: RowAction<TData>[]
  isSelected?: boolean
  onSelect?: () => void
}

export function DataTableCard<TData>({
  data,
  primaryField,
  secondaryField,
  tertiaryFields,
  avatar,
  actions = [],
  isSelected = false,
  onSelect,
}: DataTableCardProps<TData>) {
  const primaryValue = String(data[primaryField] ?? '')
  const secondaryValue = secondaryField ? String(data[secondaryField] ?? '') : undefined

  const visibleActions = actions.filter((action) => {
    if (typeof action.hidden === 'function') {
      return !action.hidden(data)
    }
    return !action.hidden
  })

  return (
    <div
      className={cn(
        'rounded-lg border bg-card p-4 shadow-sm transition-colors',
        isSelected && 'border-primary bg-primary/5'
      )}
    >
      <div className="flex items-start gap-3">
        {onSelect && (
          <Checkbox
            checked={isSelected}
            onCheckedChange={onSelect}
            className="mt-1"
          />
        )}

        {avatar && (
          <UserAvatar
            name={avatar.name}
            email={avatar.email}
            imageUrl={avatar.imageUrl}
            size="md"
          />
        )}

        <div className="flex-1 min-w-0">
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0">
              <p className="font-medium truncate">{primaryValue}</p>
              {secondaryValue && (
                <p className="text-sm text-muted-foreground truncate">
                  {secondaryValue}
                </p>
              )}
            </div>

            {visibleActions.length > 0 && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="icon" className="h-8 w-8 shrink-0">
                    <MoreHorizontal className="h-4 w-4" />
                    <span className="sr-only">Open menu</span>
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  {visibleActions.map((action, index) => {
                    const isDisabled =
                      typeof action.disabled === 'function'
                        ? action.disabled(data)
                        : action.disabled

                    return (
                      <div key={action.label}>
                        {action.separator && index > 0 && <DropdownMenuSeparator />}
                        <DropdownMenuItem
                          onClick={() => action.onClick(data)}
                          disabled={isDisabled}
                          className={cn(
                            action.variant === 'destructive' &&
                              'text-destructive focus:text-destructive'
                          )}
                        >
                          {action.icon && <span className="mr-2">{action.icon}</span>}
                          {action.label}
                        </DropdownMenuItem>
                      </div>
                    )
                  })}
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </div>

          {tertiaryFields && tertiaryFields.length > 0 && (
            <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1 text-sm">
              {tertiaryFields.map((field) => {
                const value = data[field.key]
                const rendered = field.render ? field.render(value) : String(value ?? '')

                return (
                  <div key={String(field.key)} className="flex items-center gap-1">
                    {field.label && (
                      <span className="text-muted-foreground">{field.label}:</span>
                    )}
                    <span>{rendered}</span>
                  </div>
                )
              })}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
