import type { PublicKeyCredentialCreationOptionsJSON, PublicKeyCredentialRequestOptionsJSON, RegistrationResponseJSON, AuthenticationResponseJSON } from '@simplewebauthn/browser'

// Passkey DTO from backend
export interface PasskeyDto {
  id: string
  name: string
  createdAt: string
  lastUsedAt: string | null
}

// Response wrapper for get passkeys
export interface GetPasskeysResponse {
  passkeys: PasskeyDto[]
}

// Request/Response types
export interface PasskeyRegisterOptionsResponse {
  options: PublicKeyCredentialCreationOptionsJSON
  challengeId: string
}

export interface PasskeyRegisterRequest {
  challengeId: string
  attestationResponse: RegistrationResponseJSON
  name?: string
}

export interface PasskeyLoginOptionsResponse {
  options: PublicKeyCredentialRequestOptionsJSON
  challengeId: string
}

export interface PasskeyLoginRequest {
  challengeId: string
  assertionResponse: AuthenticationResponseJSON
  deviceId?: string | null
  deviceFingerprint?: string | null
  rememberMe?: boolean
}

export interface RenamePasskeyRequest {
  name: string
}
