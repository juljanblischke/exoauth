import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Shield, ShieldCheck, ShieldOff } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { useAuth } from '@/contexts/auth-context'
import {
  MfaSetupModal,
  MfaConfirmModal,
  MfaDisableModal,
} from '@/features/auth/components'
import type { MfaConfirmResponse } from '@/features/auth/types'

export function MfaSection() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const [showSetupModal, setShowSetupModal] = useState(false)
  const [showDisableModal, setShowDisableModal] = useState(false)
  const [showBackupCodesModal, setShowBackupCodesModal] = useState(false)
  const [backupCodes, setBackupCodes] = useState<string[]>([])

  const isMfaEnabled = user?.mfaEnabled ?? false

  const handleSetupSuccess = (response: MfaConfirmResponse) => {
    setBackupCodes(response.backupCodes)
    setShowSetupModal(false)
    setShowBackupCodesModal(true)
  }

  return (
    <>
      <div className="flex items-start justify-between">
        <div className="flex items-start gap-4">
          <div className="p-2 rounded-lg bg-muted">
            {isMfaEnabled ? (
              <ShieldCheck className="h-5 w-5 text-green-600" />
            ) : (
              <Shield className="h-5 w-5 text-muted-foreground" />
            )}
          </div>
          <div className="space-y-1">
            <div className="flex items-center gap-2">
              <h4 className="font-medium">{t('mfa:title')}</h4>
              <Badge variant={isMfaEnabled ? 'default' : 'secondary'}>
                {isMfaEnabled ? t('mfa:status.enabled') : t('mfa:status.disabled')}
              </Badge>
            </div>
            <p className="text-sm text-muted-foreground">
              {t('mfa:description')}
            </p>
          </div>
        </div>

        <div className="flex gap-2">
          {isMfaEnabled ? (
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowDisableModal(true)}
            >
              <ShieldOff className="h-4 w-4 mr-2" />
              {t('mfa:disable.button')}
            </Button>
          ) : (
            <Button size="sm" onClick={() => setShowSetupModal(true)}>
              <Shield className="h-4 w-4 mr-2" />
              {t('mfa:enable.button')}
            </Button>
          )}
        </div>
      </div>

      {/* MFA Setup Modal */}
      <MfaSetupModal
        open={showSetupModal}
        onOpenChange={setShowSetupModal}
        onSuccess={handleSetupSuccess}
      />

      {/* Backup Codes Modal (shown after setup) */}
      <MfaConfirmModal
        open={showBackupCodesModal}
        onOpenChange={setShowBackupCodesModal}
        backupCodes={backupCodes}
      />

      {/* MFA Disable Modal */}
      <MfaDisableModal
        open={showDisableModal}
        onOpenChange={setShowDisableModal}
      />
    </>
  )
}
