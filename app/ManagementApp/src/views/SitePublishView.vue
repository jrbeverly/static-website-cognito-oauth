<template>
  <div>
    <v-btn
      variant="text"
      color="primary"
      prepend-icon="mdi-arrow-left"
      class="mb-4"
      :to="{ name: 'sites' }"
    >
      Back to Sites
    </v-btn>

    <v-skeleton-loader
      v-if="isLoading"
      type="heading"
    />

    <v-alert
      v-else-if="error"
      type="error"
      variant="tonal"
    >
      Failed to load site.
    </v-alert>

    <template v-else-if="site">
      <div class="d-flex align-center mb-4">
        <div>
          <h2 class="text-h4">{{ site.siteName }}</h2>
          <p v-if="site.siteUrl" class="text-body-1 text-grey mt-1">
            Published at:
            <a
              :href="site.siteUrl"
              target="_blank"
              rel="noopener noreferrer"
              class="text-decoration-none"
            >
              {{ site.siteUrl }}
              <v-icon size="small">mdi-open-in-new</v-icon>
            </a>
          </p>
        </div>
      </div>

      <v-alert
        v-if="validationError"
        type="error"
        variant="tonal"
        closable
        class="mb-4"
      >
        {{ validationError }}
      </v-alert>

      <v-alert
        v-if="publishMutation.error.value"
        type="error"
        variant="tonal"
        closable
        class="mb-4"
      >
        {{ errorMessage }}
      </v-alert>

      <v-alert
        v-if="publishedUrl"
        type="success"
        variant="tonal"
        class="mb-4"
      >
        Published!
        <a
          :href="publishedUrl"
          target="_blank"
          rel="noopener noreferrer"
          class="ml-2"
        >
          View your site
          <v-icon size="small">mdi-open-in-new</v-icon>
        </a>
      </v-alert>

      <v-card>
        <v-card-text>
          <p class="text-body-1 mb-4">
            Upload a ZIP file containing your static website. HTML, CSS, JS, and asset files are supported.
          </p>

          <v-file-input
            v-model="selectedFile"
            label="Choose a ZIP file or drag and drop"
            accept=".zip"
            prepend-icon="mdi-file-upload"
            show-size
            :disabled="publishMutation.isPending.value"
            variant="outlined"
            class="mb-4"
          />

          <v-progress-linear
            v-if="publishMutation.isPending.value"
            :model-value="progress"
            color="primary"
            height="8"
            rounded
            class="mb-4"
          />

          <v-btn
            color="primary"
            :loading="publishMutation.isPending.value"
            :disabled="!selectedFile || publishMutation.isPending.value"
            @click="handlePublish"
          >
            Publish
          </v-btn>

          <v-btn
            v-if="publishedUrl"
            variant="text"
            color="primary"
            class="ml-2"
            :href="publishedUrl"
            target="_blank"
            rel="noopener noreferrer"
          >
            View Site
            <v-icon size="small" class="ml-1">mdi-open-in-new</v-icon>
          </v-btn>
        </v-card-text>
      </v-card>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useSite } from '@/composables/useSite'
import { usePublishSite } from '@/composables/usePublishSite'
import { ApiError } from '@/composables/useApi'

const MAX_FILE_SIZE = 50 * 1024 * 1024

const route = useRoute()
const siteId = route.params.siteId as string
const selectedFile = ref<File | null>(null)

const { site, isLoading, error } = useSite(siteId)
const { publishMutation, progress, publishedUrl } = usePublishSite(siteId)

const validationError = ref<string | null>(null)

const errorMessage = computed(() => {
  const err = publishMutation.error.value
  if (!err) return ''
  if (err instanceof ApiError) {
    if (err.status === 413) return 'File too large. Maximum size is 50 MB.'
    if (err.status === 400) return 'Invalid ZIP file. Please check the contents and try again.'
  }
  return 'Upload failed. Please try again.'
})

function validateFile(file: File): string | null {
  if (!file.name.toLowerCase().endsWith('.zip')) {
    return 'Only .zip files are accepted.'
  }
  if (file.size > MAX_FILE_SIZE) {
    return 'File too large. Maximum size is 50 MB.'
  }
  return null
}

async function handlePublish(): Promise<void> {
  validationError.value = null
  if (!selectedFile.value) return

  const error = validateFile(selectedFile.value)
  if (error) {
    validationError.value = error
    return
  }

  try {
    await publishMutation.mutateAsync(selectedFile.value)
  } catch {
    // Error is available on publishMutation.error
  }
}
</script>
