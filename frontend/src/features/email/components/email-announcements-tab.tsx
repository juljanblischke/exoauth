import { useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertCircle, Plus } from 'lucide-react'

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
  useEmailAnnouncements,
  useCreateEmailAnnouncement,
  useUpdateEmailAnnouncement,
  useDeleteEmailAnnouncement,
  useSendEmailAnnouncement,
} from '../hooks'
import { AnnouncementsTable } from './announcements-table'
import { AnnouncementFormModal } from './announcement-form-modal'
import { AnnouncementDetailsSheet } from './announcement-details-sheet'
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
  const [deleteTarget, setDeleteTarget] = useState<EmailAnnouncementDto | null>(null)
  const [sendTarget, setSendTarget] = useState<EmailAnnouncementDto | null>(null)
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

  const handleSend = useCallback((announcement: EmailAnnouncementDto) => {
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

  const handleDelete = useCallback((announcement: EmailAnnouncementDto) => {
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
      {/* Header with Create button */}
      {canManage && (
        <div className="flex justify-end">
          <Button onClick={handleCreate}>
            <Plus className="h-4 w-4 mr-2" />
            {t('email:announcements.create')}
          </Button>
        </div>
      )}

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
      />

      {/* Send Confirmation Dialog */}
      <AlertDialog
        open={!!sendTarget}
        onOpenChange={(open) => !open && setSendTarget(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('email:announcements.send.title')}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('email:announcements.send.description', {
                count: sendTarget?.totalRecipients ?? 0,
              })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('common:actions.cancel')}</AlertDialogCancel>
            <AlertDialogAction onClick={handleConfirmSend}>
              {t('email:announcements.send.button')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

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
