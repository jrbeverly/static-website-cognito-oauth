import { useQuery, useMutation } from '@tanstack/vue-query'
import { useApiClient, queryClient } from './useApi'
import type { Site, CreateSiteResponse } from '@/types/api'

export function useSites() {
  const api = useApiClient()

  const { data: sites, isLoading, error, refetch } = useQuery({
    queryKey: ['sites'],
    queryFn: () => api.get<Site[]>('/sites'),
  })

  const createSiteMutation = useMutation({
    mutationFn: (siteName: string) =>
      api.post<CreateSiteResponse>('/sites', { siteName }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['sites'] }),
  })

  return { sites, isLoading, error, createSiteMutation, refetch }
}
