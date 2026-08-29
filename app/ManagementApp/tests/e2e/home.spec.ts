import { test, expect } from '@playwright/test'
import { loginAs } from './helpers/auth'

test('root path redirects to /sites when authenticated', async ({ page }) => {
  await loginAs(page)
  await page.goto('/')
  await expect(page).toHaveURL(/\/sites$/)
})
