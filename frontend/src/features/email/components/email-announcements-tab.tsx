import { useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { AlertCircle, Plus, RefreshCw } from 'lucide-react'

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
import { TypeConfirmDialog } from '@/components/shared/feedback'
import { usePermissions } from '@/contexts/auth-context'
import {
  useEmailAnnouncements,
  useCreateEmailAnnouncement,
  useUpdateEmailAnnouncement,
  useDeleteEmailAnnouncement,
  useSendEmailAnnouncement,
} from '../hooks'
import { AnnouncementsTable } from './announcements-table'
import { AnnouncementFormModal } from './announcement-form-modal'
import { AnnouncementDetailsSheet } from './announcement-details-sheet'
import { EmailAnnouncementTarget } from '../types'
import type {
  EmailAnnouncementDto,
  EmailAnnouncementDetailDto,
  CreateAnnouncementRequest,
  UpdateAnnouncementRequest,
} from '../types'

export function EmailAnnouncementsTab() {
  const { t } = useTranslation()
  const { hasPermission } = usePermissions()
  const canManage = hasPermission('email:announcements:manage')

  // State
  const [formOpen, setFormOpen] = useState(false)
  const [editingAnnouncement, setEditingAnnouncement] =
    useState<EmailAnnouncementDetailDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<EmailAnnouncementDto | EmailAnnouncementDetailDto | null>(null)
  const [sendTarget, setSendTarget] = useState<EmailAnnouncementDto | EmailAnnouncementDetailDto | null>(null)
  const [detailsTarget, setDetailsTarget] =
    useState<EmailAnnouncementDetailDto | null>(null)
  const [detailsSheetOpen, setDetailsSheetOpen] = useState(false)

  // Queries & Mutations
  const {
    data,
    isLoading,
    error,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    refetch,
    isRefetching,
  } = useEmailAnnouncements({})

  const createAnnouncement = useCreateEmailAnnouncement()
  const updateAnnouncement = useUpdateEmailAnnouncement()
  const deleteAnnouncement = useDeleteEmailAnnouncement()
  const sendAnnouncement = useSendEmailAnnouncement()

  const announcements = data?.pages.flatMap((page) => page.announcements) ?? []

  const handleLoadMore = useCallback(() => {
    if (hasNextPage && !isFetchingNextPage) {
      fetchNextPage()
    }
  }, [fetchNextPage, hasNextPage, isFetchingNextPage])

  const handleRefresh = useCallback(async () => {
    await refetch()
    toast.success(t('email:announcements.refreshed'))
  }, [refetch, t])

  // Handlers
  const handleCreate = useCallback(() => {
    setEditingAnnouncement(null)
    setFormOpen(true)
  }, [])

  const handleEdit = useCallback(async (announcement: EmailAnnouncementDto) => {
    // We need to fetch the full details for editing
    try {
      const details = await import('../api/email-api').then((m) =>
        m.emailApi.getAnnouncement(announcement.id)
      )
      setEditingAnnouncement(details)
      setFormOpen(true)
    } catch {
      // Error handled via toast
    }
  }, [])

  const handleFormSubmit = useCallback(
    async (data: CreateAnnouncementRequest | UpdateAnnouncementRequest) => {
      if (editingAnnouncement) {
        await updateAnnouncement.mutateAsync({
          id: editingAnnouncement.id,
          request: data as UpdateAnnouncementRequest,
        })
      } else {
        await createAnnouncement.mutateAsync(data as CreateAnnouncementRequest)
      }
      setFormOpen(false)
      setEditingAnnouncement(null)
    },
    [editingAnnouncement, createAnnouncement, updateAnnouncement]
  )

  const handleSend = useCallback((announcement: EmailAnnouncementDto | EmailAnnouncementDetailDto) => {
    setSendTarget(announcement)
  }, [])

  const handleConfirmSend = useCallback(async () => {
    if (sendTarget) {
      await sendAnnouncement.mutateAsync(sendTarget.id)
      setSendTarget(null)
    }
  }, [sendTarget, sendAnnouncement])

  const handleViewDetails = useCallback(
    async (announcement: EmailAnnouncementDto) => {
      try {
        const details = await import('../api/email-api').then((m) =>
          m.emailApi.getAnnouncement(announcement.id)
        )
        setDetailsTarget(details)
        setDetailsSheetOpen(true)
      } catch {
        // Error handled via toast
      }
    },
    []
  )

  const handleDelete = useCallback((announcement: EmailAnnouncementDto | EmailAnnouncementDetailDto) => {
    setDeleteTarget(announcement)
  }, [])

  // Handler for edit from the details sheet
  const handleEditFromSheet = useCallback((announcement: EmailAnnouncementDetailDto) => {
    setDetailsSheetOpen(false)
    setEditingAnnouncement(announcement)
    setFormOpen(true)
  }, [])

  // Handler for send from the details sheet
  const handleSendFromSheet = useCallback((announcement: EmailAnnouncementDetailDto) => {
    setSendTarget(announcement)
  }, [])

  // Handler for delete from the details sheet
  const handleDeleteFromSheet = useCallback((announcement: EmailAnnouncementDetailDto) => {
    setDeleteTarget(announcement)
  }, [])

  const handleConfirmDelete = useCallback(async () => {
    if (deleteTarget) {
      await deleteAnnouncement.mutateAsync(deleteTarget.id)
      setDeleteTarget(null)
    }
  }, [deleteTarget, deleteAnnouncement])

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertCircle className="h-4 w-4" />
        <AlertTitle>{t('common:error')}</AlertTitle>
        <AlertDescription>{t('email:errors.loadAnnouncements')}</AlertDescription>
      </Alert>
    )
  }

  return (
    <div className="space-y-4">
      {/* Header with Create and Refresh buttons */}
      <div className="flex justify-end gap-2">
        <Button
          variant="outline"
          size="icon"
          onClick={handleRefresh}
          disabled={isRefetching}
        >
          <RefreshCw className={`h-4 w-4 ${isRefetching ? 'animate-spin' : ''}`} />
          <span className="sr-only">{t('common:actions.refresh')}</span>
        </Button>
        {canManage && (
          <Button onClick={handleCreate}>
            <Plus className="h-4 w-4 mr-2" />
            {t('email:announcements.create')}
          </Button>
        )}
      </div>

      {/* Table */}
      <AnnouncementsTable
        announcements={announcements}
        isLoading={isLoading}
        hasMore={hasNextPage ?? false}
        onLoadMore={handleLoadMore}
        isFetchingMore={isFetchingNextPage}
        onEdit={handleEdit}
        onSend={handleSend}
        onViewDetails={handleViewDetails}
        onDelete={handleDelete}
        canManage={canManage}
      />

      {/* Create/Edit Modal */}
      <AnnouncementFormModal
        open={formOpen}
        onOpenChange={setFormOpen}
        announcement={editingAnnouncement}
        onSubmit={handleFormSubmit}
        isLoading={createAnnouncement.isPending || updateAnnouncement.isPending}
      />

      {/* Details Sheet */}
      <AnnouncementDetailsSheet
        announcement={detailsTarget}
        open={detailsSheetOpen}
        onOpenChange={(open) => {
          setDetailsSheetOpen(open)
          if (!open) setDetailsTarget(null)
        }}
        onEdit={handleEditFromSheet}
        onSend={handleSendFromSheet}
        onDelete={handleDeleteFromSheet}
        canManage={canManage}
      />

      {/* Send Confirmation Dialog - TypeConfirmDialog for all users, regular AlertDialog for others */}
      {sendTarget?.targetType === EmailAnnouncementTarget.AllUsers ? (
        <TypeConfirmDialog
          open={!!sendTarget}
          onOpenChange={(open) => !open && setSendTarget(null)}
          title={t('email:announcements.send.title')}
          description={t('email:announcements.send.allUsersDescription')}
          confirmText={t('email:announcements.send.confirmText')}
          confirmLabel={t('email:announcements.send.button')}
          loadingLabel={t('email:announcements.send.sending')}
          onConfirm={handleConfirmSend}
          isLoading={sendAnnouncement.isPending}
        />
      ) : (
        <AlertDialog
          open={!!sendTarget}
          onOpenChange={(open) => !open && setSendTarget(null)}
        >
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>{t('email:announcements.send.title')}</AlertDialogTitle>
              <AlertDialogDescription>
                {t('email:announcements.send.description')}
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>{t('common:actions.cancel')}</AlertDialogCancel>
              <AlertDialogAction 
                onClick={handleConfirmSend}
                disabled={sendAnnouncement.isPending}
              >
                {t('email:announcements.send.button')}
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      )}

      {/* Delete Confirmation Dialog */}
      <AlertDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('email:announcements.delete.title')}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('email:announcements.delete.description')}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('common:actions.cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleConfirmDelete}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {t('common:actions.delete')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
