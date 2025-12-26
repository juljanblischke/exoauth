import { useState } from 'react'
import { type Table } from '@tanstack/react-table'
import { useTranslation } from 'react-i18next'
import { Search, X } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { DataTableFilters } from './data-table-filters'
import { DataTableColumnToggle } from './data-table-column-toggle'
import type { TableFilter, ActiveFilter } from '@/types/table'
import { useDebounce } from '@/hooks/use-debounce'

interface DataTableToolbarProps<TData> {
  searchValue?: string
  onSearch?: (value: string) => void
  searchPlaceholder?: string
  filters?: TableFilter[]
  activeFilters?: ActiveFilter[]
  onFilterChange?: (filters: ActiveFilter[]) => void
  table: Table<TData>
  actions?: React.ReactNode
}

export function DataTableToolbar<TData>({
  searchValue = '',
  onSearch,
  searchPlaceholder,
  filters,
  activeFilters = [],
  onFilterChange,
  table,
  actions,
}: DataTableToolbarProps<TData>) {
  const { t } = useTranslation('common')
  const [localSearch, setLocalSearch] = useState(searchValue)

  const debouncedSearch = useDebounce((value: string) => {
    onSearch?.(value)
  }, 300)

  const handleSearchChange = (value: string) => {
    setLocalSearch(value)
    debouncedSearch(value)
  }

  const clearSearch = () => {
    setLocalSearch('')
    onSearch?.('')
  }

  const hasActiveFilters = activeFilters.length > 0

  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex flex-1 items-center gap-2">
        {onSearch && (
          <div className="relative w-full sm:max-w-sm">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder={searchPlaceholder || t('actions.search')}
              value={localSearch}
              onChange={(e) => handleSearchChange(e.target.value)}
              className="pl-9 pr-9"
            />
            {localSearch && (
              <Button
                variant="ghost"
                size="icon"
                className="absolute right-1 top-1/2 h-6 w-6 -translate-y-1/2"
                onClick={clearSearch}
              >
                <X className="h-3 w-3" />
                <span className="sr-only">{t('actions.clear')}</span>
              </Button>
            )}
          </div>
        )}

        {filters && filters.length > 0 && (
          <DataTableFilters
            filters={filters}
            activeFilters={activeFilters}
            onFilterChange={onFilterChange}
          />
        )}

        {hasActiveFilters && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => onFilterChange?.([])}
            className="h-8 px-2 lg:px-3"
          >
            {t('actions.reset')}
            <X className="ml-2 h-4 w-4" />
          </Button>
        )}
      </div>

      <div className="flex items-center gap-2">
        <DataTableColumnToggle table={table} />
        {actions}
      </div>
    </div>
  )
}
