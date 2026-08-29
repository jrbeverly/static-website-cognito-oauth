<template>
  <v-dialog :model-value="modelValue" @update:model-value="$emit('update:modelValue', $event)" max-width="480">
    <v-card>
      <v-card-title>Delete Site?</v-card-title>

      <v-card-text>
        <p class="mb-4">
          This action cannot be undone. All published content will be permanently deleted.
        </p>

        <p class="text-body-2 text-grey mb-2">
          Type the site name to confirm:
        </p>

        <v-text-field
          v-model="confirmText"
          variant="outlined"
          :placeholder="siteName"
          :disabled="loading"
          autofocus
          @keydown.enter="handleConfirm"
        />
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" :disabled="loading" @click="$emit('update:modelValue', false)">
          Cancel
        </v-btn>
        <v-btn
          color="error"
          variant="tonal"
          :loading="loading"
          :disabled="!canDelete"
          @click="handleConfirm"
        >
          Delete
        </v-btn>
      </v-card-actions>
    </v-card>

    <v-snackbar v-model="showSnackbar" color="error" timeout="5000">
      {{ errorMessage }}
      <template #actions>
        <v-btn variant="text" @click="showSnackbar = false">Dismiss</v-btn>
      </template>
    </v-snackbar>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'

const props = defineProps<{
  modelValue: boolean
  siteName: string
  loading: boolean
  errorMessage: string | null
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  confirm: []
}>()

const confirmText = ref('')
const showSnackbar = ref(false)

const canDelete = computed(() => confirmText.value === props.siteName)

watch(() => props.modelValue, (val) => {
  if (val) {
    confirmText.value = ''
  }
})

watch(() => props.errorMessage, (msg) => {
  if (msg) {
    showSnackbar.value = true
  }
})

function handleConfirm(): void {
  if (!canDelete.value) return
  showSnackbar.value = false
  emit('confirm')
}
</script>
