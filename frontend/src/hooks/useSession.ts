import { useState } from 'react'
import type { AuthResult } from '../api'

export function useSession() {
  const [session, setSession] = useState<AuthResult | null>(() => {
    const stored = localStorage.getItem('voltflow.session')
    return stored ? (JSON.parse(stored) as AuthResult) : null
  })

  return { session, setSession }
}
