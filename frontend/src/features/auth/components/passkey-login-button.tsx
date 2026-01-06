import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Fingerprint, Loader2 } from 'lucide-react'
import { toast } from 'sonner'
import { startAuthentication } from '@simplewebauthn/browser'

import { Button } from '@/components/ui/button'
import { getDeviceInfo } from '@/lib/device'
import { parseWebAuthnError } from '@/lib/webauthn'
import { useWebAuthnSupport } from '../hooks/use-webauthn-support'
import { usePasskeyLoginOptions } from '../hooks/use-passkey-login-options'
import { usePasskeyLogin, type UsePasskeyLoginOptions } from '../hooks/use-passkey-login'

interface PasskeyLoginButtonProps {
  onMfaRequired?: UsePasskeyLoginOptions['onMfaRequired']
  onMfaSetupRequired?: UsePasskeyLoginOptions['onMfaSetupRequired']
  onDeviceApprovalRequired?: UsePasskeyLoginOptions['onDeviceApprovalRequired']
}

export function PasskeyLoginButton({
  onMfaRequired,
  onMfaSetupRequired,
  onDeviceApprovalRequired,
}: PasskeyLoginButtonProps) {
  const { t } = useTranslation()
  const { isSupported, isLoading: isCheckingSupport } = useWebAuthnSupport()
  const [isAuthenticating, setIsAuthenticating] = useState(false)

  const getOptions = usePasskeyLoginOptions()
  const login = usePasskeyLogin({
    onMfaRequired,
    onMfaSetupRequired,
    onDeviceApprovalRequired,
  })

  const handleClick = async () => {
    setIsAuthenticating(true)

    try {
      // Get login options from server
      const optionsResponse = await getOptions.mutateAsync()

      // Start WebAuthn authentication
      const credential = await startAuthentication({
        optionsJSON: optionsResponse.options,
      })

      // Get device info for the request
      const deviceInfo = getDeviceInfo()

      // Complete login on server
      await login.mutateAsync({
        challengeId: optionsResponse.challengeId,
        assertionResponse: credential,
        deviceId: deviceInfo.deviceId,
        deviceFingerprint: deviceInfo.deviceFingerprint,
      })
    } catch (err) {
      const errorType = parseWebAuthnError(err)
      
      if (errorType === 'cancelled') {
        // User cancelled - no toast needed
      } else {
        toast.error(t('auth:passkeys.login.error'))
      }
    } finally {
      setIsAuthenticating(false)
    }
  }

  // Don't render if WebAuthn is not supported
  if (isCheckingSupport || !isSupported) {
    return null
  }

  const isLoading = isAuthenticating || getOptions.isPending || login.isPending

  return (
    <Button
      type="button"
      variant="outline"
      className="w-full"
      onClick={handleClick}
      disabled={isLoading}
    >
      {isLoading ? (
        <Loader2 className="h-4 w-4 mr-2 animate-spin" />
      ) : (
        <Fingerprint className="h-4 w-4 mr-2" />
      )}
      {t('auth:passkeys.loginButton')}
    </Button>
  )
}
