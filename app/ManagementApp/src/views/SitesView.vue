<template>
  <div>
    <v-alert
      v-if="error"
      type="error"
      variant="tonal"
      closable
      class="mb-4"
    >
      Failed to load sites. Please try again.
    </v-alert>

    <div class="d-flex align-center mb-4">
      <h2 class="text-h4">My Sites</h2>
      <v-spacer />
      <v-btn
        color="primary"
        variant="tonal"
        prepend-icon="mdi-plus"
        @click="openCreateDialog"
      >
        New Site
      </v-btn>
    </div>

    <v-skeleton-loader
      v-if="isLoading"
      type="table-tbody"
      :types="{ 'table-tbody': 'table-thead, table-tbody' }"
    />

    <v-card v-else-if="!sites || sites.length === 0" class="pa-8 text-center">
      <v-icon size="64" color="grey-lighten-1" class="mb-4">mdi-web</v-icon>
      <p class="text-h6 text-grey mb-4">No sites yet</p>
      <p class="text-body-1 text-grey mb-6">Create your first site to get started</p>
      <v-btn
        color="primary"
        variant="tonal"
        prepend-icon="mdi-plus"
        @click="openCreateDialog"
      >
        Create your first site
      </v-btn>
    </v-card>

    <v-data-table
      v-else
      :headers="headers"
      :items="sites"
      :hover="true"
      items-per-page-text="Sites per page"
    >
      <template #[`item.siteName`]="{ item }">
        <a
          class="cursor-pointer text-decoration-none"
          @click="router.push({ name: 'site-detail', params: { siteId: item.siteId } })"
        >
          {{ item.siteName }}
        </a>
      </template>

      <template #[`item.siteUrl`]="{ item }">
        <a
          v-if="item.siteUrl"
          :href="item.siteUrl"
          target="_blank"
          rel="noopener noreferrer"
          class="text-decoration-none"
        >
          {{ item.siteUrl }}
          <v-icon size="small" class="ml-1">mdi-open-in-new</v-icon>
        </a>
        <span v-else class="text-grey">Not published</span>
      </template>

      <template #[`item.updatedAt`]="{ item }">
        {{ formatDate(item.updatedAt) }}
      </template>

      <template #[`item.actions`]="{ item }">
        <v-btn
          variant="text"
          color="primary"
          size="small"
          @click="navigateToPublish(item.siteId)"
        >
          Publish
        </v-btn>
        <v-btn
          variant="text"
          color="error"
          size="small"
          @click="router.push({ name: 'site-detail', params: { siteId: item.siteId } })"
        >
          Delete
        </v-btn>
      </template>
    </v-data-table>

    <v-dialog v-model="showCreateDialog" max-width="480">
      <v-card>
        <v-card-title>Create New Site</v-card-title>
        <v-card-text>
          <v-text-field
            v-model="newSiteName"
            label="Site Name"
            placeholder="e.g., My Blog"
            variant="outlined"
            :rules="[required]"
            :disabled="createSiteMutation.isPending.value"
            @keydown.enter="handleCreate"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn
            variant="text"
            :disabled="createSiteMutation.isPending.value"
            @click="showCreateDialog = false"
          >
            Cancel
          </v-btn>
          <v-btn
            color="primary"
            variant="tonal"
            :loading="createSiteMutation.isPending.value"
            :disabled="!newSiteName.trim()"
            @click="handleCreate"
          >
            Create
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useSites } from '@/composables/useSites'

const router = useRouter()
const { sites, isLoading, error, createSiteMutation } = useSites()

const showCreateDialog = ref(false)
const newSiteName = ref('')

const headers = [
  { title: 'Name', key: 'siteName', sortable: true },
  { title: 'URL', key: 'siteUrl', sortable: false },
  { title: 'Last Published', key: 'updatedAt', sortable: true },
  { title: 'Status', key: 'status', sortable: false },
  { title: 'Actions', key: 'actions', sortable: false, align: 'end' as const },
]

function required(v: string): string | true {
  return v.trim().length > 0 || 'Site name is required'
}

function formatDate(iso: string): string {
  const d = new Date(iso)
  return d.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

function openCreateDialog(): void {
  newSiteName.value = ''
  showCreateDialog.value = true
}

async function handleCreate(): Promise<void> {
  const name = newSiteName.value.trim()
  if (!name) return
  try {
    await createSiteMutation.mutateAsync(name)
    showCreateDialog.value = false
  } catch {
    // Error is available on createSiteMutation.error
  }
}

function navigateToPublish(siteId: string): void {
  router.push({ name: 'site-publish', params: { siteId } })
}
</script>
