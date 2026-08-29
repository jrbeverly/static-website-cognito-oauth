import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { generateCodeVerifier, generateCodeChallenge } from '@/utils/pkce'

function base64UrlToBase64(base64url: string): string {
  let base64 = base64url.replace(/-/g, '+').replace(/_/g, '/')
  while (base64.length % 4 !== 0) base64 += '='
  return base64
}

interface IdTokenPayload {
  sub: string
  email: string
  exp: number
  [key: string]: unknown
}

function parseIdTokenPayload(token: string): IdTokenPayload {
  const [, payloadBase64] = token.split('.')
  const json = atob(base64UrlToBase64(payloadBase64))
  return JSON.parse(json) as IdTokenPayload
}

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(null)
  const sub = ref<string | null>(null)
  const email = ref<string | null>(null)
  const tokenExpiresAt = ref<number | null>(null)

  const isAuthenticated = computed(() =>
    accessToken.value !== null && Date.now() < (tokenExpiresAt.value ?? 0),
  )

  function getCognitoDomain(): string {
    return import.meta.env.VITE_COGNITO_DOMAIN || ''
  }

  function getClientId(): string {
    return import.meta.env.VITE_COGNITO_CLIENT_ID || ''
  }

  function getRedirectUri(): string {
    return import.meta.env.VITE_COGNITO_REDIRECT_URI || ''
  }

  async function login(): Promise<void> {
    const codeVerifier = generateCodeVerifier()
    const codeChallenge = await generateCodeChallenge(codeVerifier)
    const state = generateCodeVerifier()

    sessionStorage.setItem('cognito_code_verifier', codeVerifier)
    sessionStorage.setItem('cognito_oauth_state', state)

    const domain = getCognitoDomain()
    const clientId = getClientId()
    const redirectUri = getRedirectUri()
    const scope = 'openid email profile'

    const params = new URLSearchParams({
      response_type: 'code',
      client_id: clientId,
      redirect_uri: redirectUri,
      scope,
      code_challenge: codeChallenge,
      code_challenge_method: 'S256',
      state,
    })

    window.location.href = `https://${domain}/oauth2/authorize?${params.toString()}`
  }

  async function handleCallback(code: string, state: string): Promise<void> {
    const codeVerifier = sessionStorage.getItem('cognito_code_verifier')
    const storedState = sessionStorage.getItem('cognito_oauth_state')
    sessionStorage.removeItem('cognito_code_verifier')
    sessionStorage.removeItem('cognito_oauth_state')

    if (!codeVerifier) {
      throw new Error('No code verifier found in session storage. Please initiate login again.')
    }

    if (!storedState || storedState !== state) {
      throw new Error('OAuth state mismatch. Possible CSRF attack.')
    }

    const domain = getCognitoDomain()
    const clientId = getClientId()
    const redirectUri = getRedirectUri()

    const body = new URLSearchParams({
      grant_type: 'authorization_code',
      code,
      code_verifier: codeVerifier,
      redirect_uri: redirectUri,
      client_id: clientId,
    })

    const response = await fetch(`https://${domain}/oauth2/token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: body.toString(),
    })

    if (!response.ok) {
      const errorBody = await response.text()
      throw new Error(`Token exchange failed: ${response.status} ${errorBody}`)
    }

    const data = await response.json()
    const idToken = data.id_token as string

    accessToken.value = data.access_token

    const payload = parseIdTokenPayload(idToken)
    sub.value = payload.sub
    email.value = payload.email
    tokenExpiresAt.value = payload.exp * 1000
  }

  async function logout(): Promise<void> {
    accessToken.value = null
    sub.value = null
    email.value = null
    tokenExpiresAt.value = null

    const domain = getCognitoDomain()
    const clientId = getClientId()
    const redirectUri = getRedirectUri()
    const logoutUri = redirectUri.replace(/\/callback$/, '/')

    window.location.href = `https://${domain}/logout?client_id=${clientId}&logout_uri=${encodeURIComponent(logoutUri)}`
  }

  return {
    accessToken,
    sub,
    email,
    tokenExpiresAt,
    isAuthenticated,
    login,
    handleCallback,
    logout,
  }
})
