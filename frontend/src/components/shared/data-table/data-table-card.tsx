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
  secondaryField?: keyof TData | ((row: TData) => string)
  tertiaryFields?: Array<{
    key: keyof TData
    label?: string
    render?: (value: unknown, row: TData) => React.ReactNode
  }>
  avatar?: { name?: string; email?: string; imageUrl?: string }
  /** Custom icon element to display instead of avatar (e.g., for non-user entities) */
  icon?: React.ReactNode
  actions?: RowAction<TData>[]
  isSelected?: boolean
  onSelect?: () => void
  onClick?: () => void
}

export function DataTableCard<TData>({
  data,
  primaryField,
  secondaryField,
  tertiaryFields,
  avatar,
  icon,
  actions = [],
  isSelected = false,
  onSelect,
  onClick,
}: DataTableCardProps<TData>) {
  const primaryValue = String(data[primaryField] ?? '')
  const secondaryValue = secondaryField
    ? typeof secondaryField === 'function'
      ? secondaryField(data)
      : String(data[secondaryField] ?? '')
    : undefined

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
        isSelected && 'border-primary bg-primary/5',
        onClick && 'cursor-pointer hover:bg-muted/50 active:bg-muted'
      )}
      onClick={onClick}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      onKeyDown={onClick ? (e) => e.key === 'Enter' && onClick() : undefined}
    >
      <div className="flex items-start gap-3">
        {onSelect && (
          <Checkbox
            checked={isSelected}
            onCheckedChange={onSelect}
            className="mt-1"
          />
        )}

        {/* Custom icon displayed prominently */}
        {icon && <div className="shrink-0">{icon}</div>}

        <div className="flex-1 min-w-0">
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0">
              <p className="font-medium truncate">{primaryValue}</p>
              {(secondaryValue || avatar) && (
                <div className="flex items-center gap-1.5 mt-0.5">
                  {avatar && (
                    <UserAvatar
                      name={avatar.name}
                      email={avatar.email}
                      imageUrl={avatar.imageUrl}
                      size="sm"
                    />
                  )}
                  {secondaryValue && (
                    <p className="text-sm text-muted-foreground truncate">
                      {secondaryValue}
                    </p>
                  )}
                </div>
              )}
            </div>

            {visibleActions.length > 0 && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 shrink-0"
                    onClick={(e) => e.stopPropagation()}
                  >
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
                          onClick={(e) => {
                            e.stopPropagation()
                            action.onClick(data)
                          }}
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
                const rendered = field.render ? field.render(value, data) : String(value ?? '')

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
