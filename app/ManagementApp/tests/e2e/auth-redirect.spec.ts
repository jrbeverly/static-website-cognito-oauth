import { test, expect } from '@playwright/test'

/**
 * Verify that unauthenticated users are redirected to the Cognito
 * hosted UI when navigating to a protected route (/sites).
 *
 * The router's beforeEach guard calls auth.login() which redirects
 * to the Cognito authorize endpoint. We intercept this request to
 * prevent actual navigation to an external service.
 */
test('unauthenticated navigation to /sites redirects to Cognito', async ({ page }) => {
  let redirected = false

  // Intercept navigation to the Cognito authorize endpoint
  // Playwright's page.route intercepts both fetch/XHR and document navigations
  await page.route('**/oauth2/authorize**', (route) => {
    redirected = true
    route.abort()
  })

  await page.goto('/sites')

  // Give the SPA time to execute the redirect
  await page.waitForTimeout(1000)

  expect(redirected).toBe(true)
})
