import type { Page } from '@playwright/test'

/**
 * Injects authentication state directly into the browser's Pinia auth store.
 *
 * This bypasses the Cognito hosted UI OAuth flow, which cannot be automated
 * in headless CI environments. The token must be obtained out-of-band (e.g.
 * via a CI setup script using Cognito's InitiateAuth API) and provided via
 * the TEST_ACCESS_TOKEN environment variable.
 *
 * Strategy: Direct token injection via page.evaluate().
 *
 * The helper navigates to the app, accesses the Pinia store through Vue's
 * app instance, and sets the auth store state (accessToken, sub, email,
 * tokenExpiresAt). A subsequent reload ensures the router guard picks up
 * the authenticated state.
 *
 * Required environment variables:
 *   TEST_ACCESS_TOKEN  — A valid Cognito access token (JWT)
 *   TEST_USER_SUB      — The sub claim from the ID token (defaults to 'test-user')
 *   TEST_USER_EMAIL    — The user's email (defaults to 'test@example.com')
 */
export async function loginAs(page: Page): Promise<void> {
  const accessToken = process.env.TEST_ACCESS_TOKEN
  if (!accessToken) {
    throw new Error(
      'TEST_ACCESS_TOKEN environment variable is required for authenticated E2E tests.',
    )
  }

  const sub = process.env.TEST_USER_SUB ?? 'test-user'
  const email = process.env.TEST_USER_EMAIL ?? 'test@example.com'

  // Navigate to the app so Vue + Pinia initialize
  await page.goto('/')
  await page.waitForLoadState('networkidle')

  // Access Pinia via Vue's app instance and set auth store state
  await page.evaluate(
    ({ token, sub, email }) => {
      // __vue_app__ is a Vue 3 internal used for testing — not in the public types
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const pinia = (document.querySelector('#app') as any).__vue_app__?.config
        ?.globalProperties?.$pinia
      if (!pinia) {
        throw new Error(
          'Could not access Pinia instance. Make sure the app is running.',
        )
      }

      pinia.state.value.auth = {
        accessToken: token,
        sub,
        email,
        tokenExpiresAt: Date.now() + 24 * 60 * 60 * 1000,
      }
    },
    { token: accessToken, sub, email },
  )

  // Reload so the router guard picks up the authenticated state
  await page.goto('/')
  await page.waitForLoadState('networkidle')
}

/**
 * Returns true if the required auth environment variables are set.
 */
export function hasAuthCredentials(): boolean {
  return !!process.env.TEST_ACCESS_TOKEN
}
