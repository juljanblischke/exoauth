import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Loader2 } from 'lucide-react'
import { z } from 'zod'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import { Separator } from '@/components/ui/separator'
import type { EmailConfigurationDto, UpdateEmailConfigurationRequest } from '../types'

const configurationSchema = z.object({
  // Retry Settings
  maxRetriesPerProvider: z.coerce.number().min(0).max(10),
  initialRetryDelayMs: z.coerce.number().min(100).max(60000),
  maxRetryDelayMs: z.coerce.number().min(1000).max(3600000),
  backoffMultiplier: z.coerce.number().min(1).max(10),
  // Circuit Breaker Settings
  circuitBreakerFailureThreshold: z.coerce.number().min(1).max(100),
  circuitBreakerWindowMinutes: z.coerce.number().min(1).max(1440),
  circuitBreakerOpenDurationMinutes: z.coerce.number().min(1).max(1440),
  // DLQ Settings
  autoRetryDlq: z.boolean(),
  dlqRetryIntervalHours: z.coerce.number().min(1).max(168),
  // General Settings
  emailsEnabled: z.boolean(),
  testMode: z.boolean(),
})

type FormData = z.infer<typeof configurationSchema>

interface EmailConfigurationFormProps {
  configuration: EmailConfigurationDto
  onSubmit: (data: UpdateEmailConfigurationRequest) => void
  isLoading?: boolean
  canEdit?: boolean
}

export function EmailConfigurationForm({
  configuration,
  onSubmit,
  isLoading,
  canEdit = true,
}: EmailConfigurationFormProps) {
  const { t } = useTranslation('email')

  const form = useForm<FormData>({
    resolver: zodResolver(configurationSchema) as never,
    defaultValues: {
      maxRetriesPerProvider: configuration.maxRetriesPerProvider,
      initialRetryDelayMs: configuration.initialRetryDelayMs,
      maxRetryDelayMs: configuration.maxRetryDelayMs,
      backoffMultiplier: configuration.backoffMultiplier,
      circuitBreakerFailureThreshold: configuration.circuitBreakerFailureThreshold,
      circuitBreakerWindowMinutes: configuration.circuitBreakerWindowMinutes,
      circuitBreakerOpenDurationMinutes: configuration.circuitBreakerOpenDurationMinutes,
      autoRetryDlq: configuration.autoRetryDlq,
      dlqRetryIntervalHours: configuration.dlqRetryIntervalHours,
      emailsEnabled: configuration.emailsEnabled,
      testMode: configuration.testMode,
    },
  })

  // Reset form when configuration changes
  useEffect(() => {
    form.reset({
      maxRetriesPerProvider: configuration.maxRetriesPerProvider,
      initialRetryDelayMs: configuration.initialRetryDelayMs,
      maxRetryDelayMs: configuration.maxRetryDelayMs,
      backoffMultiplier: configuration.backoffMultiplier,
      circuitBreakerFailureThreshold: configuration.circuitBreakerFailureThreshold,
      circuitBreakerWindowMinutes: configuration.circuitBreakerWindowMinutes,
      circuitBreakerOpenDurationMinutes: configuration.circuitBreakerOpenDurationMinutes,
      autoRetryDlq: configuration.autoRetryDlq,
      dlqRetryIntervalHours: configuration.dlqRetryIntervalHours,
      emailsEnabled: configuration.emailsEnabled,
      testMode: configuration.testMode,
    })
  }, [configuration, form])

  const handleSubmit = form.handleSubmit((data) => {
    onSubmit(data as UpdateEmailConfigurationRequest)
  })

  const autoRetryDlq = form.watch('autoRetryDlq')

  return (
    <form onSubmit={handleSubmit} className="space-y-8">
      {/* General Settings */}
      <section className="space-y-4">
        <h3 className="text-lg font-medium">
          {t('email:configuration.sections.general')}
        </h3>
        <Separator />

        <div className="space-y-4">
          {/* Emails Enabled */}
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="emailsEnabled">
                {t('email:configuration.fields.emailsEnabled')}
              </Label>
              <p className="text-sm text-muted-foreground">
                {t('email:configuration.fields.emailsEnabledDescription')}
              </p>
            </div>
            <Switch
              id="emailsEnabled"
              checked={form.watch('emailsEnabled')}
              onCheckedChange={(checked) => form.setValue('emailsEnabled', checked)}
              disabled={!canEdit}
            />
          </div>

          {/* Test Mode */}
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="testMode">
                {t('email:configuration.fields.testMode')}
              </Label>
              <p className="text-sm text-muted-foreground">
                {t('email:configuration.fields.testModeDescription')}
              </p>
            </div>
            <Switch
              id="testMode"
              checked={form.watch('testMode')}
              onCheckedChange={(checked) => form.setValue('testMode', checked)}
              disabled={!canEdit}
            />
          </div>
        </div>
      </section>

      {/* Retry Settings */}
      <section className="space-y-4">
        <h3 className="text-lg font-medium">
          {t('email:configuration.sections.retry')}
        </h3>
        <Separator />

        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="maxRetriesPerProvider">
              {t('email:configuration.fields.maxRetriesPerProvider')}
            </Label>
            <Input
              id="maxRetriesPerProvider"
              type="number"
              {...form.register('maxRetriesPerProvider')}
              disabled={!canEdit}
            />
            <p className="text-xs text-muted-foreground">
              {t('email:configuration.fields.maxRetriesPerProviderDescription')}
            </p>
            {form.formState.errors.maxRetriesPerProvider && (
              <p className="text-sm text-destructive">
                {form.formState.errors.maxRetriesPerProvider.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="backoffMultiplier">
              {t('email:configuration.fields.backoffMultiplier')}
            </Label>
            <Input
              id="backoffMultiplier"
              type="number"
              step="0.1"
              {...form.register('backoffMultiplier')}
              disabled={!canEdit}
            />
            <p className="text-xs text-muted-foreground">
              {t('email:configuration.fields.backoffMultiplierDescription')}
            </p>
            {form.formState.errors.backoffMultiplier && (
              <p className="text-sm text-destructive">
                {form.formState.errors.backoffMultiplier.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="initialRetryDelayMs">
              {t('email:configuration.fields.initialRetryDelayMs')}
            </Label>
            <Input
              id="initialRetryDelayMs"
              type="number"
              {...form.register('initialRetryDelayMs')}
              disabled={!canEdit}
            />
            <p className="text-xs text-muted-foreground">
              {t('email:configuration.fields.initialRetryDelayMsDescription')}
            </p>
            {form.formState.errors.initialRetryDelayMs && (
              <p className="text-sm text-destructive">
                {form.formState.errors.initialRetryDelayMs.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="maxRetryDelayMs">
              {t('email:configuration.fields.maxRetryDelayMs')}
            </Label>
            <Input
              id="maxRetryDelayMs"
              type="number"
              {...form.register('maxRetryDelayMs')}
              disabled={!canEdit}
            />
            <p className="text-xs text-muted-foreground">
              {t('email:configuration.fields.maxRetryDelayMsDescription')}
            </p>
            {form.formState.errors.maxRetryDelayMs && (
              <p className="text-sm text-destructive">
                {form.formState.errors.maxRetryDelayMs.message}
              </p>
            )}
          </div>
        </div>
      </section>

      {/* Circuit Breaker Settings */}
      <section className="space-y-4">
        <h3 className="text-lg font-medium">
          {t('email:configuration.sections.circuitBreaker')}
        </h3>
        <Separator />

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <div className="space-y-2">
            <Label htmlFor="circuitBreakerFailureThreshold">
              {t('email:configuration.fields.circuitBreakerFailureThreshold')}
            </Label>
            <Input
              id="circuitBreakerFailureThreshold"
              type="number"
              {...form.register('circuitBreakerFailureThreshold')}
              disabled={!canEdit}
            />
            <p className="text-xs text-muted-foreground">
              {t('email:configuration.fields.circuitBreakerFailureThresholdDescription')}
            </p>
            {form.formState.errors.circuitBreakerFailureThreshold && (
              <p className="text-sm text-destructive">
                {form.formState.errors.circuitBreakerFailureThreshold.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="circuitBreakerWindowMinutes">
              {t('email:configuration.fields.circuitBreakerWindowMinutes')}
            </Label>
            <Input
              id="circuitBreakerWindowMinutes"
              type="number"
              {...form.register('circuitBreakerWindowMinutes')}
              disabled={!canEdit}
            />
            <p className="text-xs text-muted-foreground">
              {t('email:configuration.fields.circuitBreakerWindowMinutesDescription')}
            </p>
            {form.formState.errors.circuitBreakerWindowMinutes && (
              <p className="text-sm text-destructive">
                {form.formState.errors.circuitBreakerWindowMinutes.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="circuitBreakerOpenDurationMinutes">
              {t('email:configuration.fields.circuitBreakerOpenDurationMinutes')}
            </Label>
            <Input
              id="circuitBreakerOpenDurationMinutes"
              type="number"
              {...form.register('circuitBreakerOpenDurationMinutes')}
              disabled={!canEdit}
            />
            <p className="text-xs text-muted-foreground">
              {t('email:configuration.fields.circuitBreakerOpenDurationMinutesDescription')}
            </p>
            {form.formState.errors.circuitBreakerOpenDurationMinutes && (
              <p className="text-sm text-destructive">
                {form.formState.errors.circuitBreakerOpenDurationMinutes.message}
              </p>
            )}
          </div>
        </div>
      </section>

      {/* DLQ Settings */}
      <section className="space-y-4">
        <h3 className="text-lg font-medium">
          {t('email:configuration.sections.dlq')}
        </h3>
        <Separator />

        <div className="space-y-4">
          {/* Auto Retry DLQ */}
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="autoRetryDlq">
                {t('email:configuration.fields.autoRetryDlq')}
              </Label>
              <p className="text-sm text-muted-foreground">
                {t('email:configuration.fields.autoRetryDlqDescription')}
              </p>
            </div>
            <Switch
              id="autoRetryDlq"
              checked={form.watch('autoRetryDlq')}
              onCheckedChange={(checked) => form.setValue('autoRetryDlq', checked)}
              disabled={!canEdit}
            />
          </div>

          {/* DLQ Retry Interval (only if auto retry is enabled) */}
          {autoRetryDlq && (
            <div className="space-y-2 max-w-xs">
              <Label htmlFor="dlqRetryIntervalHours">
                {t('email:configuration.fields.dlqRetryIntervalHours')}
              </Label>
              <Input
                id="dlqRetryIntervalHours"
                type="number"
                {...form.register('dlqRetryIntervalHours')}
                disabled={!canEdit}
              />
              <p className="text-xs text-muted-foreground">
                {t('email:configuration.fields.dlqRetryIntervalHoursDescription')}
              </p>
              {form.formState.errors.dlqRetryIntervalHours && (
                <p className="text-sm text-destructive">
                  {form.formState.errors.dlqRetryIntervalHours.message}
                </p>
              )}
            </div>
          )}
        </div>
      </section>

      {/* Submit Button */}
      {canEdit && (
        <div className="flex justify-end">
          <Button type="submit" disabled={isLoading || !form.formState.isDirty}>
            {isLoading && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
            {t('common:actions.save')}
          </Button>
        </div>
      )}
    </form>
  )
}
