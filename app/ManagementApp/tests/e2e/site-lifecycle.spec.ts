import { test, expect } from '@playwright/test'
import { loginAs } from './helpers/auth'
import { createTestZip } from './helpers/zip'

const SITE_NAME = 'Playwright Test Site'

test.describe('Site lifecycle', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
  })

  test('create → publish → view → delete', async ({ page }) => {
    // 1. Navigate to dashboard and verify empty state
    await page.goto('/sites')
    await expect(page.getByText('No sites yet')).toBeVisible()

    // 2. Create a new site
    await page.getByRole('button', { name: 'New Site' }).click()

    const dialog = page.getByRole('dialog')
    await expect(dialog.getByText('Create New Site')).toBeVisible()

    await page.getByLabel('Site Name').fill(SITE_NAME)
    await page.getByRole('button', { name: 'Create' }).click()

    // Verify dialog closed and site appears in table
    await expect(dialog).not.toBeVisible()
    await expect(page.getByText(SITE_NAME)).toBeVisible()

    // 3. Publish content — navigate to publish page
    const siteRow = page.getByRole('row', { name: new RegExp(SITE_NAME) })
    await siteRow.getByRole('button', { name: 'Publish' }).click()

    // Verify we're on the publish page
    await expect(page.getByRole('heading', { name: SITE_NAME })).toBeVisible()

    // Upload a test ZIP
    const zip = createTestZip()
    const fileInput = page.locator('input[type="file"]')
    await fileInput.setInputFiles({
      name: 'site.zip',
      mimeType: 'application/zip',
      buffer: zip,
    })

    // Click publish
    await page.getByRole('button', { name: 'Publish' }).click()

    // Verify success message
    await expect(page.getByText('Published!')).toBeVisible({ timeout: 15_000 })

    // 4. Navigate back to dashboard and verify site URL
    await page.goto('/sites')
    await page.waitForLoadState('networkidle')

    // Site row should no longer show "Not published"
    const updatedRow = page.getByRole('row', { name: new RegExp(SITE_NAME) })
    await expect(updatedRow.getByText('Not published')).not.toBeVisible()

    // 5. Delete the site — navigate to detail via the "Delete" action
    await updatedRow.getByRole('button', { name: 'Delete' }).click()

    // Verify we're on the detail page
    await expect(page.getByRole('heading', { name: SITE_NAME })).toBeVisible()

    // Open delete confirmation dialog
    await page.getByRole('button', { name: 'Delete Site' }).click()

    const deleteDialog = page.getByRole('dialog')
    await expect(deleteDialog.getByText('Delete Site?')).toBeVisible()

    // Type site name to confirm
    const confirmInput = deleteDialog.locator('input[type="text"]')
    await confirmInput.fill(SITE_NAME)

    // Click delete
    await deleteDialog.getByRole('button', { name: 'Delete' }).click()

    // Verify we're back on the dashboard with empty state
    await expect(page.getByText('No sites yet')).toBeVisible()
  })
})
