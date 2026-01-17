import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Plus, Info } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { ConfirmDialog } from '@/components/shared/feedback'
import { useWebAuthnSupport } from '../hooks/use-webauthn-support'
import { usePasskeys } from '../hooks/use-passkeys'
import { useRenamePasskey } from '../hooks/use-rename-passkey'
import { useDeletePasskey } from '../hooks/use-delete-passkey'
import { PasskeyCard } from './passkey-card'
import { PasskeyEmptyState } from './passkey-empty-state'
import { RegisterPasskeyModal } from './register-passkey-modal'
import { RenamePasskeyModal } from './rename-passkey-modal'
import { WebAuthnNotSupported } from './webauthn-not-supported'
import type { PasskeyDto } from '../types/passkey'

export function PasskeysSection() {
  const { t } = useTranslation()
  const { isSupported, isLoading: isCheckingSupport } = useWebAuthnSupport()
  const { data: passkeys, isLoading: isLoadingPasskeys } = usePasskeys()
  const renamePasskey = useRenamePasskey()
  const deletePasskey = useDeletePasskey()

  const [showRegisterModal, setShowRegisterModal] = useState(false)
  const [passkeyToRename, setPasskeyToRename] = useState<PasskeyDto | null>(null)
  const [passkeyToDelete, setPasskeyToDelete] = useState<PasskeyDto | null>(null)
  const [deletingId, setDeletingId] = useState<string | null>(null)

  const handleRename = (passkey: PasskeyDto) => {
    setPasskeyToRename(passkey)
  }

  const handleRenameConfirm = async (name: string) => {
    if (!passkeyToRename) return

    await renamePasskey.mutateAsync({
      id: passkeyToRename.id,
      request: { name },
    })
    setPasskeyToRename(null)
  }

  const handleDelete = (passkey: PasskeyDto) => {
    setPasskeyToDelete(passkey)
  }

  const handleDeleteConfirm = async () => {
    if (!passkeyToDelete) return

    setDeletingId(passkeyToDelete.id)
    try {
      await deletePasskey.mutateAsync(passkeyToDelete.id)
    } finally {
      setDeletingId(null)
    }
    setPasskeyToDelete(null)
  }

  // Loading state
  if (isCheckingSupport || isLoadingPasskeys) {
    return (
      <div className="space-y-4">
        <div>
          <Skeleton className="h-6 w-32 mb-2" />
          <Skeleton className="h-4 w-64" />
        </div>
        <Skeleton className="h-24 w-full" />
      </div>
    )
  }

  // WebAuthn not supported
  if (!isSupported) {
    return (
      <div className="space-y-4">
        <div>
          <h3 className="text-lg font-medium">{t('auth:passkeys.title')}</h3>
          <p className="text-sm text-muted-foreground mt-1">
            {t('auth:passkeys.description')}
          </p>
        </div>
        <WebAuthnNotSupported />
      </div>
    )
  }

  const passkeysList = passkeys ?? []
  const hasPasskeys = passkeysList.length > 0

  return (
    <div className="space-y-4">
      <div className="flex items-start justify-between">
        <div>
          <h3 className="text-lg font-medium">{t('auth:passkeys.title')}</h3>
          <p className="text-sm text-muted-foreground mt-1">
            {t('auth:passkeys.description')}
          </p>
        </div>
        {hasPasskeys && (
          <Button onClick={() => setShowRegisterModal(true)} size="sm">
            <Plus className="h-4 w-4 mr-2" />
            {t('auth:passkeys.addButton')}
          </Button>
        )}
      </div>

      {hasPasskeys ? (
        <>
          <div className="space-y-3">
            {passkeysList.map((passkey) => (
              <PasskeyCard
                key={passkey.id}
                passkey={passkey}
                onRename={handleRename}
                onDelete={handleDelete}
                isDeleting={deletingId === passkey.id}
              />
            ))}
          </div>

          <div className="flex items-start gap-2 p-3 rounded-lg bg-muted/50 text-sm text-muted-foreground">
            <Info className="h-4 w-4 mt-0.5 flex-shrink-0" />
            <span>{t('auth:passkeys.multiDeviceHint')}</span>
          </div>
        </>
      ) : (
        <PasskeyEmptyState onAddPasskey={() => setShowRegisterModal(true)} />
      )}

      <RegisterPasskeyModal
        open={showRegisterModal}
        onOpenChange={setShowRegisterModal}
      />

      <RenamePasskeyModal
        passkey={passkeyToRename}
        open={!!passkeyToRename}
        onOpenChange={(open) => !open && setPasskeyToRename(null)}
        onConfirm={handleRenameConfirm}
        isLoading={renamePasskey.isPending}
      />

      <ConfirmDialog
        open={!!passkeyToDelete}
        onOpenChange={(open) => !open && setPasskeyToDelete(null)}
        title={t('auth:passkeys.delete.title')}
        description={t('auth:passkeys.delete.message')}
        confirmLabel={t('common:actions.delete')}
        onConfirm={handleDeleteConfirm}
        variant="destructive"
        isLoading={deletePasskey.isPending}
      />
    </div>
  )
}
