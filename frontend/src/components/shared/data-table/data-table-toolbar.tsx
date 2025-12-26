import { useState } from 'react'
import { type Table } from '@tanstack/react-table'
import { useTranslation } from 'react-i18next'
import { Search, X, ArrowUpDown } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { DataTableFilters } from './data-table-filters'
import { DataTableColumnToggle } from './data-table-column-toggle'
import type { TableFilter, ActiveFilter } from '@/types/table'
import { useDebouncedCallback } from '@/hooks/use-debounce'

interface DataTableToolbarProps<TData> {
  searchValue?: string
  onSearch?: (value: string) => void
  searchPlaceholder?: string
  filters?: TableFilter[]
  activeFilters?: ActiveFilter[]
  onFilterChange?: (filters: ActiveFilter[]) => void
  table: Table<TData>
  actions?: React.ReactNode
  content?: React.ReactNode
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
  content,
}: DataTableToolbarProps<TData>) {
  const { t } = useTranslation('common')
  const [localSearch, setLocalSearch] = useState(searchValue)

  const debouncedSearch = useDebouncedCallback((value: string) => {
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
  const sortingState = table.getState().sorting
  const hasSorting = sortingState.length > 0

  const clearSorting = () => {
    table.resetSorting()
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      {onSearch && (
        <div className="relative w-full sm:w-auto sm:min-w-[200px] sm:max-w-sm">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder={searchPlaceholder || t('actions.search')}
            value={localSearch}
            onChange={(e) => handleSearchChange(e.target.value)}
            className="h-8 pl-9 pr-9"
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

      {content}

      {hasSorting && (
        <Button
          variant="ghost"
          size="sm"
          onClick={clearSorting}
          className="h-8 px-2 lg:px-3"
        >
          <ArrowUpDown className="mr-1 h-3 w-3" />
          {sortingState.length}
          <X className="ml-1 h-3 w-3" />
        </Button>
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

      <div className="ml-auto flex items-center gap-2">
        <DataTableColumnToggle table={table} />
        {actions}
      </div>
    </div>
  )
}
