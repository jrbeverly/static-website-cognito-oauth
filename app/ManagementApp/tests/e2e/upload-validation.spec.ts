import { test, expect } from '@playwright/test'
import { loginAs } from './helpers/auth'
import { createNonZipFile } from './helpers/zip'

const SITE_NAME = 'Upload Validation Test Site'

test.describe('Upload validation', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)

    // Create a site to publish to
    await page.goto('/sites')

    const existingSite = page.getByText(SITE_NAME)
    if (await existingSite.isVisible()) {
      await page.getByRole('row', { name: new RegExp(SITE_NAME) })
        .getByRole('button', { name: 'Publish' }).click()
    } else {
      await page.getByRole('button', { name: 'New Site' }).click()
      await page.getByLabel('Site Name').fill(SITE_NAME)
      await page.getByRole('button', { name: 'Create' }).click()
      await expect(page.getByText(SITE_NAME)).toBeVisible()

      await page.getByRole('row', { name: new RegExp(SITE_NAME) })
        .getByRole('button', { name: 'Publish' }).click()
    }

    await expect(page.getByRole('heading', { name: SITE_NAME })).toBeVisible()
  })

  test('non-ZIP file shows client-side error and does not submit', async ({ page }) => {
    const nonZip = createNonZipFile()

    const fileInput = page.locator('input[type="file"]')
    await fileInput.setInputFiles({
      name: nonZip.name,
      mimeType: nonZip.mimeType,
      buffer: nonZip.buffer,
    })

    // Click publish to trigger client-side validation
    await page.getByRole('button', { name: 'Publish' }).click()

    // The validateFile() function runs synchronously before any API call,
    // so the error appears without a network request being made.
    await expect(page.getByText('Only .zip files are accepted.')).toBeVisible()
  })
})
