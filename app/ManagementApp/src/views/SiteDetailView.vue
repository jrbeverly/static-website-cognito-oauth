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
      type="article"
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
        <h2 class="text-h4">{{ site.siteName }}</h2>
        <v-chip
          :color="site.status === 'active' ? 'success' : 'grey'"
          class="ml-3"
          size="small"
        >
          {{ site.status }}
        </v-chip>
      </div>

      <v-card class="mb-4">
        <v-card-text>
          <p class="text-subtitle-2 text-grey mb-1">Site URL</p>
          <template v-if="site.siteUrl">
            <a
              :href="site.siteUrl"
              target="_blank"
              rel="noopener noreferrer"
              class="text-decoration-none"
            >
              {{ site.siteUrl }}
              <v-icon size="small">mdi-open-in-new</v-icon>
            </a>
          </template>
          <p v-else class="text-grey">Not yet published</p>
        </v-card-text>
      </v-card>

      <v-card class="mb-4">
        <v-card-text>
          <p class="text-subtitle-2 text-grey mb-1">Metadata</p>
          <v-list density="compact">
            <v-list-item>
              <template #title>Site ID</template>
              <template #subtitle>
                <code>{{ site.siteId }}</code>
              </template>
            </v-list-item>
            <v-list-item>
              <template #title>Content Path</template>
              <template #subtitle>
                <code>{{ site.contentPath }}</code>
              </template>
            </v-list-item>
            <v-list-item>
              <template #title>Created</template>
              <template #subtitle>{{ formatDate(site.createdAt) }}</template>
            </v-list-item>
            <v-list-item>
              <template #title>Last Updated</template>
              <template #subtitle>{{ formatDate(site.updatedAt) }}</template>
            </v-list-item>
          </v-list>
        </v-card-text>
      </v-card>

      <v-card>
        <v-card-text>
          <div class="d-flex">
            <v-btn
              color="primary"
              variant="tonal"
              :to="{ name: 'site-publish', params: { siteId } }"
            >
              Publish New Content
            </v-btn>
            <v-spacer />
            <v-btn
              color="error"
              variant="tonal"
              @click="showDeleteDialog = true"
            >
              Delete Site
            </v-btn>
          </div>
        </v-card-text>
      </v-card>
    </template>

    <DeleteSiteDialog
      v-if="site"
      v-model="showDeleteDialog"
      :site-name="site.siteName"
      :loading="deleteMutation.isPending.value"
      :error-message="deleteError"
      @confirm="handleDeleteConfirm"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useSite } from '@/composables/useSite'
import DeleteSiteDialog from '@/components/DeleteSiteDialog.vue'

const route = useRoute()
const router = useRouter()
const siteId = route.params.siteId as string

const { site, isLoading, error, deleteMutation } = useSite(siteId)

const showDeleteDialog = ref(false)
const deleteError = ref<string | null>(null)

watch(showDeleteDialog, (val) => {
  if (!val) {
    deleteError.value = null
  }
})

function formatDate(iso: string): string {
  const d = new Date(iso)
  return d.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

async function handleDeleteConfirm(): Promise<void> {
  deleteError.value = null
  try {
    await deleteMutation.mutateAsync()
    router.push({ name: 'sites' })
  } catch (e) {
    deleteError.value = e instanceof Error ? e.message : 'Failed to delete site. Please try again.'
  }
}
</script>
