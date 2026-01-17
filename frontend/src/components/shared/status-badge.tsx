import { cn } from '@/lib/utils'

type StatusType = 'success' | 'warning' | 'error' | 'info' | 'neutral'

interface StatusBadgeProps {
  status: StatusType
  label: string
  showDot?: boolean
  className?: string
}

const statusStyles: Record<StatusType, { bg: string; text: string; dot: string }> = {
  success: {
    bg: 'bg-emerald-100/50 dark:bg-emerald-950/50',
    text: 'text-emerald-700 dark:text-emerald-400',
    dot: 'bg-emerald-500',
  },
  warning: {
    bg: 'bg-amber-100/50 dark:bg-amber-950/50',
    text: 'text-amber-700 dark:text-amber-400',
    dot: 'bg-amber-500',
  },
  error: {
    bg: 'bg-red-100/50 dark:bg-red-950/50',
    text: 'text-red-700 dark:text-red-400',
    dot: 'bg-red-500',
  },
  info: {
    bg: 'bg-blue-100/50 dark:bg-blue-950/50',
    text: 'text-blue-700 dark:text-blue-400',
    dot: 'bg-blue-500',
  },
  neutral: {
    bg: 'bg-gray-100/50 dark:bg-gray-800/50',
    text: 'text-gray-700 dark:text-gray-300',
    dot: 'bg-gray-500',
  },
}

export function StatusBadge({
  status,
  label,
  showDot = true,
  className,
}: StatusBadgeProps) {
  const styles = statusStyles[status]

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium',
        styles.bg,
        styles.text,
        className
      )}
    >
      {showDot && (
        <span className={cn('h-1.5 w-1.5 rounded-full', styles.dot)} />
      )}
      {label}
    </span>
  )
}
