import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from '@/stores/auth'

function createTestJwt(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
  const body = btoa(JSON.stringify(payload))
  const sig = 'fake-signature'
  return `${header}.${body}.${sig}`
}

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.stubEnv('VITE_COGNITO_DOMAIN', 'test.auth.example.com')
    vi.stubEnv('VITE_COGNITO_CLIENT_ID', 'test-client-id')
    vi.stubEnv('VITE_COGNITO_REDIRECT_URI', 'http://localhost:5173/callback')
    sessionStorage.clear()
  })

  afterEach(() => {
    vi.unstubAllEnvs()
  })

  describe('isAuthenticated', () => {
    it('is false when accessToken is null', () => {
      const auth = useAuthStore()
      auth.accessToken = null
      expect(auth.isAuthenticated).toBe(false)
    })

    it('is false when token is expired', () => {
      const auth = useAuthStore()
      auth.accessToken = 'some-token'
      auth.tokenExpiresAt = Date.now() - 1000
      expect(auth.isAuthenticated).toBe(false)
    })

    it('is true when token is present and not expired', () => {
      const auth = useAuthStore()
      auth.accessToken = 'some-token'
      auth.tokenExpiresAt = Date.now() + 3600_000
      expect(auth.isAuthenticated).toBe(true)
    })

    it('is false when token is present but tokenExpiresAt is null', () => {
      const auth = useAuthStore()
      auth.accessToken = 'some-token'
      auth.tokenExpiresAt = null
      expect(auth.isAuthenticated).toBe(false)
    })
  })

  describe('handleCallback', () => {
    it('sets accessToken, sub, email, and tokenExpiresAt after successful exchange', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          access_token: 'test-access-token',
          id_token: createTestJwt({ sub: 'user-123', email: 'test@example.com', exp: 9999999999 }),
        }),
        text: async () => '',
      })
      vi.stubGlobal('fetch', mockFetch)

      sessionStorage.setItem('cognito_code_verifier', 'test-verifier')
      sessionStorage.setItem('cognito_oauth_state', 'test-state')

      const auth = useAuthStore()
      await auth.handleCallback('test-code', 'test-state')

      expect(auth.accessToken).toBe('test-access-token')
      expect(auth.sub).toBe('user-123')
      expect(auth.email).toBe('test@example.com')
      expect(auth.tokenExpiresAt).toBe(9999999999000)
    })

    it('throws when code verifier is missing from session storage', async () => {
      const auth = useAuthStore()
      await expect(auth.handleCallback('code', 'state')).rejects.toThrow(
        'No code verifier found',
      )
    })

    it('throws when state does not match stored state', async () => {
      sessionStorage.setItem('cognito_code_verifier', 'test-verifier')
      sessionStorage.setItem('cognito_oauth_state', 'expected-state')

      const auth = useAuthStore()
      await expect(auth.handleCallback('code', 'wrong-state')).rejects.toThrow(
        'OAuth state mismatch',
      )
    })

    it('throws when token exchange fails', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: false,
        status: 400,
        text: async () => 'invalid_grant',
      })
      vi.stubGlobal('fetch', mockFetch)

      sessionStorage.setItem('cognito_code_verifier', 'test-verifier')
      sessionStorage.setItem('cognito_oauth_state', 'test-state')

      const auth = useAuthStore()
      await expect(auth.handleCallback('code', 'test-state')).rejects.toThrow(
        'Token exchange failed',
      )
    })
  })

  describe('logout', () => {
    it('clears accessToken and related state', async () => {
      const auth = useAuthStore()
      auth.accessToken = 'some-token'
      auth.sub = 'user-123'
      auth.email = 'test@example.com'
      auth.tokenExpiresAt = Date.now() + 3600_000

      // Prevent actual redirect by catching the location assignment
      const locationMock = { href: '' }
      vi.stubGlobal('location', locationMock)

      await auth.logout()

      expect(auth.accessToken).toBeNull()
      expect(auth.sub).toBeNull()
      expect(auth.email).toBeNull()
      expect(auth.tokenExpiresAt).toBeNull()
    })
  })
})
