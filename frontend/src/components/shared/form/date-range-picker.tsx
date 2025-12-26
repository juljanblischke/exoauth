import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { format } from 'date-fns'
import { de, enUS } from 'date-fns/locale'
import { Calendar as CalendarIcon, X } from 'lucide-react'
import { type DateRange } from 'react-day-picker'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Calendar } from '@/components/ui/calendar'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'

function useIsMobile() {
  const [isMobile, setIsMobile] = useState(false)

  useEffect(() => {
    const checkMobile = () => setIsMobile(window.innerWidth < 640)
    checkMobile()
    window.addEventListener('resize', checkMobile)
    return () => window.removeEventListener('resize', checkMobile)
  }, [])

  return isMobile
}

interface DateRangePickerProps {
  value?: DateRange
  onChange?: (range: DateRange | undefined) => void
  placeholder?: string
  className?: string
}

export function DateRangePicker({
  value,
  onChange,
  placeholder,
  className,
}: DateRangePickerProps) {
  const { t, i18n } = useTranslation()
  const [open, setOpen] = useState(false)
  const [tempRange, setTempRange] = useState<DateRange | undefined>(value)
  const isMobile = useIsMobile()

  const locale = i18n.language === 'de' ? de : enUS

  const formatDateRange = (range: DateRange | undefined) => {
    if (!range?.from) return null
    if (!range.to) {
      return format(range.from, 'PP', { locale })
    }
    return `${format(range.from, 'PP', { locale })} - ${format(range.to, 'PP', { locale })}`
  }

  const displayText = formatDateRange(value)

  const handleClear = (e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
    onChange?.(undefined)
    setTempRange(undefined)
  }

  const handleOpenChange = (isOpen: boolean) => {
    setOpen(isOpen)
    if (isOpen) {
      setTempRange(value)
    }
  }

  const handleApply = () => {
    onChange?.(tempRange)
    setOpen(false)
  }

  const handleReset = () => {
    setTempRange(undefined)
    // Don't close - just clear the selection so user can pick new dates
  }

  return (
    <Popover open={open} onOpenChange={handleOpenChange}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          size="sm"
          className={cn(
            'h-8 border-dashed justify-start text-left font-normal',
            !displayText && 'text-muted-foreground',
            className
          )}
        >
          <CalendarIcon className="mr-2 h-4 w-4" />
          {displayText || placeholder || t('common:filters.dateRange')}
          {displayText && (
            <span
              role="button"
              className="ml-2 rounded-sm p-0.5 hover:bg-muted"
              onClick={handleClear}
              onKeyDown={(e) => e.key === 'Enter' && handleClear(e as unknown as React.MouseEvent)}
              tabIndex={0}
            >
              <X className="h-3 w-3" />
            </span>
          )}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-0 max-w-[calc(100vw-2rem)]" align="start">
        <Calendar
          mode="range"
          defaultMonth={tempRange?.from}
          selected={tempRange}
          onSelect={setTempRange}
          numberOfMonths={isMobile ? 1 : 2}
          locale={locale}
        />
        <div className="flex items-center justify-end gap-2 border-t p-3">
          <Button variant="ghost" size="sm" onClick={handleReset}>
            {t('common:actions.reset')}
          </Button>
          <Button size="sm" onClick={handleApply}>
            {t('common:actions.confirm')}
          </Button>
        </div>
      </PopoverContent>
    </Popover>
  )
}
