import { useRef, useCallback, type KeyboardEvent, type ClipboardEvent } from 'react'
import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'
import type { DeviceApprovalCodeInputProps } from '../types'

const CODE_LENGTH = 4

/**
 * Two 4-character input fields for device approval code
 * Format: XXXX-XXXX
 * Features:
 * - Auto-focus to second field after 4 chars
 * - Auto-focus back on backspace when empty
 * - Paste handling (split "XXXX-XXXX" or "XXXXXXXX")
 * - Only allows alphanumeric (uppercase)
 */
export function DeviceApprovalCodeInput({
  value,
  onChange,
  disabled = false,
  error = false,
}: DeviceApprovalCodeInputProps) {
  const firstInputRef = useRef<HTMLInputElement>(null)
  const secondInputRef = useRef<HTMLInputElement>(null)

  // Parse value into two parts
  const parts = value.split('-')
  const firstPart = parts[0] || ''
  const secondPart = parts[1] || ''

  // Sanitize input: uppercase alphanumeric only
  const sanitize = (str: string): string => {
    return str.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, CODE_LENGTH)
  }

  // Update the combined value
  const updateValue = useCallback(
    (first: string, second: string) => {
      const sanitizedFirst = sanitize(first)
      const sanitizedSecond = sanitize(second)
      onChange(`${sanitizedFirst}-${sanitizedSecond}`)
    },
    [onChange]
  )

  // Handle first input change
  const handleFirstChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = sanitize(e.target.value)
    updateValue(newValue, secondPart)

    // Auto-focus to second input when first is complete
    if (newValue.length === CODE_LENGTH) {
      secondInputRef.current?.focus()
    }
  }

  // Handle second input change
  const handleSecondChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = sanitize(e.target.value)
    updateValue(firstPart, newValue)
  }

  // Handle backspace on empty second input - focus back to first
  const handleSecondKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Backspace' && secondPart === '') {
      firstInputRef.current?.focus()
    }
  }

  // Handle paste - support both "XXXX-XXXX" and "XXXXXXXX" formats
  const handlePaste = (e: ClipboardEvent<HTMLInputElement>) => {
    e.preventDefault()
    const pastedText = e.clipboardData.getData('text')
    const cleaned = pastedText.toUpperCase().replace(/[^A-Z0-9]/g, '')

    if (cleaned.length >= CODE_LENGTH * 2) {
      // Full code pasted
      updateValue(cleaned.slice(0, CODE_LENGTH), cleaned.slice(CODE_LENGTH, CODE_LENGTH * 2))
      secondInputRef.current?.focus()
    } else if (cleaned.length > CODE_LENGTH) {
      // Partial second part
      updateValue(cleaned.slice(0, CODE_LENGTH), cleaned.slice(CODE_LENGTH))
      secondInputRef.current?.focus()
    } else {
      // Only first part
      updateValue(cleaned, secondPart)
      if (cleaned.length === CODE_LENGTH) {
        secondInputRef.current?.focus()
      }
    }
  }

  const inputClasses = cn(
    'text-center text-2xl font-mono tracking-[0.5em] uppercase h-14',
    error && 'border-destructive focus-visible:ring-destructive'
  )

  return (
    <div className="flex items-center justify-center gap-3">
      <Input
        ref={firstInputRef}
        type="text"
        inputMode="text"
        autoComplete="one-time-code"
        maxLength={CODE_LENGTH}
        value={firstPart}
        onChange={handleFirstChange}
        onPaste={handlePaste}
        disabled={disabled}
        className={inputClasses}
        aria-label="First 4 characters of verification code"
      />
      <span className="text-2xl font-bold text-muted-foreground">-</span>
      <Input
        ref={secondInputRef}
        type="text"
        inputMode="text"
        autoComplete="one-time-code"
        maxLength={CODE_LENGTH}
        value={secondPart}
        onChange={handleSecondChange}
        onKeyDown={handleSecondKeyDown}
        onPaste={handlePaste}
        disabled={disabled}
        className={inputClasses}
        aria-label="Last 4 characters of verification code"
      />
    </div>
  )
}
