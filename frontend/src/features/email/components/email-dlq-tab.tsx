import { useState, useCallback, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertCircle, RotateCcw } from 'lucide-react'

import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { usePermissions } from '@/contexts/auth-context'
import {
  useDeadLetterQueue,
  useProcessDlqMessage,
  useDeleteDlqMessage,
} from '../hooks'
import { EmailDlqTable } from './email-dlq-table'
import type { EmailLogDto } from '../types'

export function EmailDlqTab() {
  const { t } = useTranslation()
  const { hasPermission } = usePermissions()
  const canManage = hasPermission('email:dlq:manage')

  const [deleteTarget, setDeleteTarget] = useState<EmailLogDto | null>(null)
  const [retryAllDialogOpen, setRetryAllDialogOpen] = useState(false)

  const {
    data,
    isLoading,
    error,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useDeadLetterQueue({})

  const retryEmail = useProcessDlqMessage()
  const deleteEmail = useDeleteDlqMessage()

  const emails = useMemo(
    () => data?.pages.flatMap((page) => page.emails) ?? [],
    [data?.pages]
  )
  const totalCount = emails.length

  const handleLoadMore = useCallback(() => {
    if (hasNextPage && !isFetchingNextPage) {
      fetchNextPage()
    }
  }, [fetchNextPage, hasNextPage, isFetchingNextPage])

  const handleRetry = useCallback(
    async (email: EmailLogDto) => {
      await retryEmail.mutateAsync(email.id)
    },
    [retryEmail]
  )

  const handleDelete = useCallback((email: EmailLogDto) => {
    setDeleteTarget(email)
  }, [])

  const handleConfirmDelete = useCallback(async () => {
    if (deleteTarget) {
      await deleteEmail.mutateAsync(deleteTarget.id)
      setDeleteTarget(null)
    }
  }, [deleteTarget, deleteEmail])

  const handleRetryAll = useCallback(async () => {
    // Retry all emails one by one
    for (const email of emails) {
      await retryEmail.mutateAsync(email.id)
    }
    setRetryAllDialogOpen(false)
  }, [emails, retryEmail])

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertCircle className="h-4 w-4" />
        <AlertTitle>{t('common:error')}</AlertTitle>
        <AlertDescription>{t('email:errors.loadDlq')}</AlertDescription>
      </Alert>
    )
  }

  return (
    <div className="space-y-4">
      {/* Header with Retry All button */}
      {canManage && emails.length > 0 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            {t('email:dlq.count', { count: totalCount })}
          </p>
          <Button
            variant="outline"
            onClick={() => setRetryAllDialogOpen(true)}
            disabled={retryEmail.isPending}
          >
            <RotateCcw className="h-4 w-4 mr-2" />
            {t('email:dlq.retryAll')}
          </Button>
        </div>
      )}

      {/* Table */}
      <EmailDlqTable
        emails={emails}
        isLoading={isLoading}
        hasMore={hasNextPage ?? false}
        onLoadMore={handleLoadMore}
        isFetchingMore={isFetchingNextPage}
        onRetry={handleRetry}
        onDelete={handleDelete}
        canManage={canManage}
      />

      {/* Delete Confirmation Dialog */}
      <AlertDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('email:dlq.delete.title')}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('email:dlq.delete.description')}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('common:actions.cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleConfirmDelete}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {t('email:dlq.actions.delete')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Retry All Confirmation Dialog */}
      <AlertDialog open={retryAllDialogOpen} onOpenChange={setRetryAllDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {t('email:dlq.retryAllConfirm.title')}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {t('email:dlq.retryAllConfirm.description', { count: totalCount })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('common:actions.cancel')}</AlertDialogCancel>
            <AlertDialogAction onClick={handleRetryAll}>
              {t('email:dlq.retryAll')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
