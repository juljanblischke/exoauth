import { useMemo } from 'react'
import { formatDistanceToNow, format, parseISO, type Locale } from 'date-fns'
import { de, enUS } from 'date-fns/locale'
import { useTranslation } from 'react-i18next'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { cn } from '@/lib/utils'

interface RelativeTimeProps {
  date: string | Date
  className?: string
  showTooltip?: boolean
}

const locales: Record<string, Locale> = {
  en: enUS,
  de: de,
}

export function RelativeTime({
  date,
  className,
  showTooltip = true,
}: RelativeTimeProps) {
  const { i18n } = useTranslation()

  const { relative, absolute } = useMemo(() => {
    const dateObj = typeof date === 'string' ? parseISO(date) : date
    const locale = locales[i18n.language] || enUS

    return {
      relative: formatDistanceToNow(dateObj, { addSuffix: true, locale }),
      absolute: format(dateObj, 'PPpp', { locale }),
    }
  }, [date, i18n.language])

  if (!showTooltip) {
    return <span className={cn('text-muted-foreground', className)}>{relative}</span>
  }

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <span className={cn('cursor-default text-muted-foreground', className)}>
          {relative}
        </span>
      </TooltipTrigger>
      <TooltipContent>
        <p>{absolute}</p>
      </TooltipContent>
    </Tooltip>
  )
}
