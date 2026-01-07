// Device Approval Types for Risk-Based Authentication

/**
 * Response when login requires device approval
 * Extended from normal login response
 */
export interface DeviceApprovalRequiredResponse {
  deviceApprovalRequired: true
  approvalToken: string
  riskScore: number
  riskLevel: string
  riskFactors: string[]
}

/**
 * Request to approve device by code (from email)
 */
export interface ApproveDeviceByCodeRequest {
  approvalToken: string
  code: string
  captchaToken?: string
}

/**
 * Response after successful device approval by code
 */
export interface ApproveDeviceByCodeResponse {
  success: boolean
  remainingAttempts?: number
  message?: string
}

/**
 * Response when approving device by email link
 */
export interface ApproveDeviceByLinkResponse {
  success: boolean
  message?: string
}

/**
 * Request to deny a device
 */
export interface DenyDeviceRequest {
  approvalToken: string
}

/**
 * Response after denying a device
 */
export interface DenyDeviceResponse {
  success: boolean
  message?: string
}

/**
 * Modal state for device approval flow
 */
export type DeviceApprovalModalState =
  | 'input'       // User entering code
  | 'loading'     // Submitting code
  | 'success'     // Code accepted
  | 'error'       // Wrong code, show remaining attempts
  | 'expired'     // Token expired
  | 'maxAttempts' // Too many tries

/**
 * Props for the DeviceApprovalModal component
 */
export interface DeviceApprovalModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  approvalToken: string
  riskFactors: string[]
  onSuccess: () => void
  onDeny: () => void
}

/**
 * Props for the DeviceApprovalCodeInput component
 */
export interface DeviceApprovalCodeInputProps {
  value: string
  onChange: (code: string) => void
  disabled?: boolean
  error?: boolean
}

/**
 * Type guard to check if login response requires device approval
 */
export function isDeviceApprovalRequired(
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  response: any
): response is DeviceApprovalRequiredResponse {
  return (
    response &&
    response.deviceApprovalRequired === true &&
    typeof response.approvalToken === 'string'
  )
}
