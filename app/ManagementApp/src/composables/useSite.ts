import { useQuery, useMutation } from '@tanstack/vue-query'
import { useApiClient, queryClient } from './useApi'
import type { SiteDetailResponse } from '@/types/api'

export function useSite(siteId: string) {
  const api = useApiClient()

  const { data: site, isLoading, error } = useQuery({
    queryKey: ['sites', siteId],
    queryFn: () => api.get<SiteDetailResponse>(`/sites/${siteId}`),
    staleTime: 30_000,
  })

  const deleteMutation = useMutation({
    mutationFn: () => api.delete(`/sites/${siteId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sites'] })
    },
  })

  return { site, isLoading, error, deleteMutation }
}
