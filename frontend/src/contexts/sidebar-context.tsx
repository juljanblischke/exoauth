/* eslint-disable react-refresh/only-export-components */
import {
  createContext,
  useContext,
  useCallback,
  type ReactNode,
} from 'react'
import { useLocalStorage, useIsMobile } from '@/hooks'

interface SidebarContextValue {
  isCollapsed: boolean
  isOpen: boolean // For mobile sheet
  setCollapsed: (collapsed: boolean) => void
  setOpen: (open: boolean) => void
  toggle: () => void
  toggleMobile: () => void
}

const SidebarContext = createContext<SidebarContextValue | null>(null)

const STORAGE_KEY = 'exoauth-sidebar'

interface SidebarProviderProps {
  children: ReactNode
  defaultCollapsed?: boolean
}

export function SidebarProvider({
  children,
  defaultCollapsed = false,
}: SidebarProviderProps) {
  const isMobile = useIsMobile()
  const [isCollapsed, setCollapsedValue] = useLocalStorage<boolean>(
    STORAGE_KEY,
    defaultCollapsed
  )
  const [isOpen, setOpenValue] = useLocalStorage<boolean>(
    `${STORAGE_KEY}-mobile`,
    false
  )

  const setCollapsed = useCallback(
    (collapsed: boolean) => {
      setCollapsedValue(collapsed)
    },
    [setCollapsedValue]
  )

  const setOpen = useCallback(
    (open: boolean) => {
      setOpenValue(open)
    },
    [setOpenValue]
  )

  const toggle = useCallback(() => {
    if (isMobile) {
      setOpenValue((prev) => !prev)
    } else {
      setCollapsedValue((prev) => !prev)
    }
  }, [isMobile, setCollapsedValue, setOpenValue])

  const toggleMobile = useCallback(() => {
    setOpenValue((prev) => !prev)
  }, [setOpenValue])

  return (
    <SidebarContext.Provider
      value={{
        isCollapsed: isMobile ? false : isCollapsed,
        isOpen,
        setCollapsed,
        setOpen,
        toggle,
        toggleMobile,
      }}
    >
      {children}
    </SidebarContext.Provider>
  )
}

export function useSidebar(): SidebarContextValue {
  const context = useContext(SidebarContext)
  if (!context) {
    throw new Error('useSidebar must be used within a SidebarProvider')
  }
  return context
}
