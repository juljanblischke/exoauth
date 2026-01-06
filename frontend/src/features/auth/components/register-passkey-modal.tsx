import { useState, useMemo, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Loader2, Fingerprint, AlertCircle } from 'lucide-react'
import { z } from 'zod'
import { startRegistration } from '@simplewebauthn/browser'

import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { getSuggestedPasskeyName, parseWebAuthnError } from '@/lib/webauthn'
import { usePasskeyRegisterOptions, usePasskeyRegister } from '../hooks'

function createRegisterSchema(t: (key: string) => string) {
  return z.object({
    name: z
      .string()
      .min(1, t('validation:required'))
      .max(100, t('validation:maxLength')),
  })
}

type RegisterFormData = z.infer<ReturnType<typeof createRegisterSchema>>

interface RegisterPasskeyModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess?: () => void
}

export function RegisterPasskeyModal({
  open,
  onOpenChange,
  onSuccess,
}: RegisterPasskeyModalProps) {
  const { t } = useTranslation()
  const registerSchema = useMemo(() => createRegisterSchema(t), [t])
  const [error, setError] = useState<string | null>(null)
  const [isRegistering, setIsRegistering] = useState(false)

  const getOptions = usePasskeyRegisterOptions()
  const register = usePasskeyRegister()

  const form = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      name: getSuggestedPasskeyName(),
    },
  })

  // Reset form and error when modal opens
  useEffect(() => {
    if (open) {
      form.reset({ name: getSuggestedPasskeyName() })
      setError(null)
    }
  }, [open, form])

  const handleSubmit = form.handleSubmit(async (data) => {
    setError(null)
    setIsRegistering(true)

    try {
      // Get registration options from server
      const optionsResponse = await getOptions.mutateAsync()
      
      // Start WebAuthn registration
      const credential = await startRegistration({
        optionsJSON: optionsResponse.options,
      })

      // Complete registration on server
      await register.mutateAsync({
        challengeId: optionsResponse.challengeId,
        attestationResponse: credential,
        name: data.name.trim(),
      })

      onOpenChange(false)
      onSuccess?.()
    } catch (err) {
      const errorType = parseWebAuthnError(err)
      
      if (errorType === 'cancelled') {
        setError(t('auth:passkeys.register.cancelled'))
      } else if (errorType === 'timeout') {
        setError(t('auth:passkeys.register.error'))
      } else {
        setError(t('auth:passkeys.register.error'))
      }
    } finally {
      setIsRegistering(false)
    }
  })

  const isLoading = isRegistering || getOptions.isPending || register.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Fingerprint className="h-5 w-5" />
              {t('auth:passkeys.register.title')}
            </DialogTitle>
            <DialogDescription>
              {t('auth:passkeys.register.description')}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            {error && (
              <Alert variant="destructive">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <div className="space-y-2">
              <Label htmlFor="passkey-name">
                {t('auth:passkeys.register.nameLabel')}
              </Label>
              <Input
                id="passkey-name"
                {...form.register('name')}
                placeholder={t('auth:passkeys.register.namePlaceholder')}
                disabled={isLoading}
              />
              {form.formState.errors.name && (
                <p className="text-sm text-destructive">
                  {form.formState.errors.name.message}
                </p>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isLoading}
            >
              {t('common:actions.cancel')}
            </Button>
            <Button
              type="submit"
              disabled={!form.formState.isValid || isLoading}
            >
              {isLoading && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
              {t('auth:passkeys.register.submit')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
