import { ref } from 'vue'
import { useMutation } from '@tanstack/vue-query'
import { useApiClient, queryClient } from './useApi'
import type { PublishSiteResponse } from '@/types/api'

export function usePublishSite(siteId: string) {
  const api = useApiClient()
  const progress = ref(0)
  const publishedUrl = ref<string | null>(null)

  const publishMutation = useMutation({
    mutationFn: (file: File) => {
      progress.value = 0
      const formData = new FormData()
      formData.append('file', file)
      return api.putForm<PublishSiteResponse>(`/sites/${siteId}/content`, formData, {
        onUploadProgress: (p) => {
          progress.value = Math.round(p * 100)
        },
      })
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['sites'] })
      publishedUrl.value = data.siteUrl
    },
  })

  return { publishMutation, progress, publishedUrl }
}
