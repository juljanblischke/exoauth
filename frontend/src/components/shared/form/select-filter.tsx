import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Check, ChevronDown, X } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'

export interface SelectFilterOption {
  label: string
  value: string
}

interface SingleSelectFilterProps {
  label: string
  options: SelectFilterOption[]
  value?: string
  onChange?: (value: string | undefined) => void
  placeholder?: string
  searchPlaceholder?: string
  className?: string
  multiple?: false
}

interface MultiSelectFilterProps {
  label: string
  options: SelectFilterOption[]
  values?: string[]
  onValuesChange?: (values: string[]) => void
  placeholder?: string
  searchPlaceholder?: string
  className?: string
  multiple: true
}

type SelectFilterProps = SingleSelectFilterProps | MultiSelectFilterProps

export function SelectFilter(props: SelectFilterProps) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const { label, options, placeholder, searchPlaceholder, className, multiple } = props

  // Multi-select mode
  if (multiple) {
    const { values = [], onValuesChange } = props as MultiSelectFilterProps

    const selectedOptions = options.filter((opt) => values.includes(opt.value))

    const handleClear = (e: React.MouseEvent) => {
      e.preventDefault()
      e.stopPropagation()
      onValuesChange?.([])
    }

    const handleSelect = (optionValue: string) => {
      const newValues = values.includes(optionValue)
        ? values.filter((v) => v !== optionValue)
        : [...values, optionValue]
      onValuesChange?.(newValues)
    }

    return (
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            size="sm"
            className={cn(
              'h-8 border-dashed justify-between gap-1',
              selectedOptions.length === 0 && 'text-muted-foreground',
              className
            )}
          >
            <span className="flex items-center gap-1.5">
              {selectedOptions.length > 0 ? (
                <>
                  <span className="font-medium">{label}:</span>
                  <span>
                    {selectedOptions.length === 1
                      ? selectedOptions[0].label
                      : t('common:filters.selected', { count: selectedOptions.length })}
                  </span>
                </>
              ) : (
                placeholder || label
              )}
            </span>
            {selectedOptions.length > 0 ? (
              <span
                role="button"
                className="ml-1 rounded-sm p-0.5 hover:bg-muted"
                onClick={handleClear}
                onKeyDown={(e) => e.key === 'Enter' && handleClear(e as unknown as React.MouseEvent)}
                tabIndex={0}
              >
                <X className="h-3 w-3" />
              </span>
            ) : (
              <ChevronDown className="ml-1 h-3 w-3 shrink-0 opacity-50" />
            )}
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-[250px] max-w-[calc(100vw-2rem)] p-0" align="start">
          <Command>
            <CommandInput placeholder={searchPlaceholder || t('common:actions.search')} />
            <CommandList>
              <CommandEmpty>{t('common:search.noResults')}</CommandEmpty>
              <CommandGroup>
                {options.map((option) => (
                  <CommandItem
                    key={option.value}
                    value={`${option.value}::${option.label}`}
                    onSelect={() => handleSelect(option.value)}
                  >
                    <Check
                      className={cn(
                        'mr-2 h-4 w-4',
                        values.includes(option.value) ? 'opacity-100' : 'opacity-0'
                      )}
                    />
                    {option.label}
                  </CommandItem>
                ))}
              </CommandGroup>
            </CommandList>
          </Command>
        </PopoverContent>
      </Popover>
    )
  }

  // Single-select mode
  const { value, onChange } = props as SingleSelectFilterProps
  const selectedOption = options.find((opt) => opt.value === value)

  const handleClear = (e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
    onChange?.(undefined)
    setOpen(false)
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          size="sm"
          className={cn(
            'h-8 border-dashed justify-between gap-1',
            !selectedOption && 'text-muted-foreground',
            className
          )}
        >
          <span className="flex items-center gap-1.5">
            {selectedOption ? (
              <>
                <span className="font-medium">{label}:</span>
                <span>{selectedOption.label}</span>
              </>
            ) : (
              placeholder || label
            )}
          </span>
          {selectedOption ? (
            <span
              role="button"
              className="ml-1 rounded-sm p-0.5 hover:bg-muted"
              onClick={handleClear}
              onKeyDown={(e) => e.key === 'Enter' && handleClear(e as unknown as React.MouseEvent)}
              tabIndex={0}
            >
              <X className="h-3 w-3" />
            </span>
          ) : (
            <ChevronDown className="ml-1 h-3 w-3 shrink-0 opacity-50" />
          )}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[250px] max-w-[calc(100vw-2rem)] p-0" align="start">
        <Command>
          <CommandInput placeholder={searchPlaceholder || t('common:actions.search')} />
          <CommandList>
            <CommandEmpty>{t('common:search.noResults')}</CommandEmpty>
            <CommandGroup>
              {options.map((option) => (
                <CommandItem
                  key={option.value}
                  value={`${option.value}::${option.label}`}
                  onSelect={() => {
                    onChange?.(option.value === value ? undefined : option.value)
                    setOpen(false)
                  }}
                >
                  <Check
                    className={cn(
                      'mr-2 h-4 w-4',
                      value === option.value ? 'opacity-100' : 'opacity-0'
                    )}
                  />
                  {option.label}
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  )
}
