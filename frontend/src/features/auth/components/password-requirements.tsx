import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Check, X } from 'lucide-react'
import { cn } from '@/lib/utils'

interface PasswordRequirementsProps {
  password: string
  className?: string
}

interface Requirement {
  key: string
  label: string
  test: (password: string) => boolean
}

export function PasswordRequirements({
  password,
  className,
}: PasswordRequirementsProps) {
  const { t } = useTranslation('auth')

  const requirements: Requirement[] = useMemo(
    () => [
      {
        key: 'minLength',
        label: t('password.minLength'),
        test: (p) => p.length >= 12,
      },
      {
        key: 'uppercase',
        label: t('password.uppercase'),
        test: (p) => /[A-Z]/.test(p),
      },
      {
        key: 'lowercase',
        label: t('password.lowercase'),
        test: (p) => /[a-z]/.test(p),
      },
      {
        key: 'digit',
        label: t('password.digit'),
        test: (p) => /[0-9]/.test(p),
      },
      {
        key: 'special',
        label: t('password.special'),
        test: (p) => /[^a-zA-Z0-9]/.test(p),
      },
    ],
    [t]
  )

  const results = useMemo(
    () =>
      requirements.map((req) => ({
        ...req,
        passed: req.test(password),
      })),
    [password, requirements]
  )

  return (
    <div className={cn('space-y-2', className)}>
      <p className="text-xs font-medium text-muted-foreground">
        {t('password.requirements')}
      </p>
      <ul className="space-y-1 text-xs">
        {results.map((req) => (
          <li
            key={req.key}
            className={cn(
              'flex items-center gap-2',
              req.passed
                ? 'text-green-600 dark:text-green-400'
                : 'text-muted-foreground'
            )}
          >
            {req.passed ? (
              <Check className="h-3 w-3" />
            ) : (
              <X className="h-3 w-3" />
            )}
            {req.label}
          </li>
        ))}
      </ul>
    </div>
  )
}
