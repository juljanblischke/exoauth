import { useMemo, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Check, X } from 'lucide-react'
import { Progress } from '@/components/ui/progress'
import { cn } from '@/lib/utils'

interface PasswordStrengthProps {
  password: string
  onStrengthChange?: (strength: number) => void
  showRequirements?: boolean
}

interface Requirement {
  key: string
  label: string
  test: (password: string) => boolean
}

export function PasswordStrength({
  password,
  onStrengthChange,
  showRequirements = true,
}: PasswordStrengthProps) {
  const { t } = useTranslation('validation')

  const requirements: Requirement[] = useMemo(
    () => [
      {
        key: 'minLength',
        label: t('password.minLength', { min: 8 }),
        test: (p) => p.length >= 8,
      },
      {
        key: 'lowercase',
        label: t('password.lowercase'),
        test: (p) => /[a-z]/.test(p),
      },
      {
        key: 'uppercase',
        label: t('password.uppercase'),
        test: (p) => /[A-Z]/.test(p),
      },
      {
        key: 'number',
        label: t('password.number'),
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

  const { strength, passedCount, results } = useMemo(() => {
    const results = requirements.map((req) => ({
      ...req,
      passed: req.test(password),
    }))
    const passedCount = results.filter((r) => r.passed).length
    const strength = Math.round((passedCount / requirements.length) * 100)
    return { strength, passedCount, results }
  }, [password, requirements])

  useEffect(() => {
    onStrengthChange?.(strength)
  }, [strength, onStrengthChange])

  const getStrengthLabel = () => {
    if (passedCount <= 1) return t('password.strength.weak')
    if (passedCount <= 2) return t('password.strength.fair')
    if (passedCount <= 4) return t('password.strength.good')
    return t('password.strength.strong')
  }

  const getStrengthColor = () => {
    if (passedCount <= 1) return 'bg-red-500'
    if (passedCount <= 2) return 'bg-orange-500'
    if (passedCount <= 4) return 'bg-yellow-500'
    return 'bg-green-500'
  }

  return (
    <div className="space-y-3">
      <div className="space-y-1">
        <div className="flex items-center justify-between text-xs">
          <span className="text-muted-foreground">Password strength</span>
          <span
            className={cn(
              'font-medium',
              passedCount <= 1 && 'text-red-500',
              passedCount === 2 && 'text-orange-500',
              passedCount >= 3 && passedCount <= 4 && 'text-yellow-500',
              passedCount === 5 && 'text-green-500'
            )}
          >
            {getStrengthLabel()}
          </span>
        </div>
        <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted">
          <div
            className={cn('h-full transition-all duration-300', getStrengthColor())}
            style={{ width: `${strength}%` }}
          />
        </div>
      </div>

      {showRequirements && (
        <ul className="space-y-1 text-xs">
          {results.map((req) => (
            <li
              key={req.key}
              className={cn(
                'flex items-center gap-2',
                req.passed ? 'text-green-600 dark:text-green-400' : 'text-muted-foreground'
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
      )}
    </div>
  )
}
