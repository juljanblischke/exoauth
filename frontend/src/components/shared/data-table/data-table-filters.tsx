import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Filter, Check, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
} from '@/components/ui/command'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { cn } from '@/lib/utils'
import type { TableFilter, ActiveFilter, FilterOption } from '@/types/table'

interface DataTableFiltersProps {
  filters: TableFilter[]
  activeFilters: ActiveFilter[]
  onFilterChange?: (filters: ActiveFilter[]) => void
}

export function DataTableFilters({
  filters,
  activeFilters,
  onFilterChange,
}: DataTableFiltersProps) {
  const { t } = useTranslation('common')
  const [open, setOpen] = useState(false)

  const getActiveFilterValue = (filterId: string) => {
    const active = activeFilters.find((f) => f.id === filterId)
    return active?.value
  }

  const handleSelect = (filter: TableFilter, option: FilterOption) => {
    const currentValue = getActiveFilterValue(filter.id)

    if (filter.type === 'multi-select') {
      const currentArray = (currentValue as string[]) || []
      const newValue = currentArray.includes(option.value)
        ? currentArray.filter((v) => v !== option.value)
        : [...currentArray, option.value]

      if (newValue.length === 0) {
        onFilterChange?.(activeFilters.filter((f) => f.id !== filter.id))
      } else {
        const newFilters = activeFilters.filter((f) => f.id !== filter.id)
        onFilterChange?.([...newFilters, { id: filter.id, value: newValue }])
      }
    } else {
      if (currentValue === option.value) {
        onFilterChange?.(activeFilters.filter((f) => f.id !== filter.id))
      } else {
        const newFilters = activeFilters.filter((f) => f.id !== filter.id)
        onFilterChange?.([...newFilters, { id: filter.id, value: option.value }])
      }
    }
  }

  const isOptionSelected = (filter: TableFilter, option: FilterOption) => {
    const value = getActiveFilterValue(filter.id)
    if (filter.type === 'multi-select') {
      return ((value as string[]) || []).includes(option.value)
    }
    return value === option.value
  }

  const activeCount = activeFilters.length

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button variant="outline" size="sm" className="h-8 border-dashed">
          <Filter className="mr-2 h-4 w-4" />
          {t('actions.filter')}
          {activeCount > 0 && (
            <>
              <Separator orientation="vertical" className="mx-2 h-4" />
              <Badge
                variant="secondary"
                className="rounded-sm px-1 font-normal"
              >
                {activeCount}
              </Badge>
            </>
          )}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[280px] p-0" align="start">
        <Command>
          <CommandInput placeholder={t('actions.search')} />
          <CommandList>
            <CommandEmpty>{t('search.noResults')}</CommandEmpty>
            {filters.map((filter, index) => (
              <div key={filter.id}>
                {index > 0 && <CommandSeparator />}
                <CommandGroup heading={filter.label}>
                  {filter.options?.map((option) => {
                    const isSelected = isOptionSelected(filter, option)
                    return (
                      <CommandItem
                        key={option.value}
                        onSelect={() => handleSelect(filter, option)}
                      >
                        <div
                          className={cn(
                            'mr-2 flex h-4 w-4 items-center justify-center rounded-sm border border-primary',
                            isSelected
                              ? 'bg-primary text-primary-foreground'
                              : 'opacity-50 [&_svg]:invisible'
                          )}
                        >
                          <Check className="h-3 w-3" />
                        </div>
                        {option.icon && (
                          <span className="mr-2">{option.icon}</span>
                        )}
                        <span>{option.label}</span>
                      </CommandItem>
                    )
                  })}
                </CommandGroup>
              </div>
            ))}
          </CommandList>
          {activeCount > 0 && (
            <>
              <CommandSeparator />
              <CommandGroup>
                <CommandItem
                  onSelect={() => {
                    onFilterChange?.([])
                    setOpen(false)
                  }}
                  className="justify-center text-center"
                >
                  <X className="mr-2 h-4 w-4" />
                  {t('actions.reset')}
                </CommandItem>
              </CommandGroup>
            </>
          )}
        </Command>
      </PopoverContent>
    </Popover>
  )
}
