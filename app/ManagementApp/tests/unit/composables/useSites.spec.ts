import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { defineComponent } from 'vue'
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query'
import { useSites } from '@/composables/useSites'
import type { Site, CreateSiteResponse } from '@/types/api'

const { mockGet, mockPost, mockInvalidateQueries } = vi.hoisted(() => ({
  mockGet: vi.fn(),
  mockPost: vi.fn(),
  mockInvalidateQueries: vi.fn(),
}))

vi.mock('@/composables/useApi', () => ({
  useApiClient: () => ({
    get: mockGet,
    post: mockPost,
    put: vi.fn(),
    delete: vi.fn(),
    putForm: vi.fn(),
  }),
  queryClient: { invalidateQueries: mockInvalidateQueries },
  ApiError: class ApiError extends Error {
    status: number
    constructor(status: number, message: string) {
      super(message)
      this.status = status
      this.name = 'ApiError'
    }
  },
}))

const TestComponent = defineComponent({
  setup() {
    return useSites()
  },
  template: '<div></div>',
})

function mountWithQuery() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  return mount(TestComponent, {
    global: {
      plugins: [[VueQueryPlugin, { queryClient }]],
    },
  })
}

describe('useSites', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('returns loading state initially', () => {
    // Don't resolve the get call yet so isLoading stays true
    let resolve: (value: unknown) => void = () => {}
    mockGet.mockReturnValue(new Promise((r) => { resolve = r }))

    const wrapper = mountWithQuery()
    expect(wrapper.vm.isLoading).toBe(true)
    expect(wrapper.vm.sites).toBeUndefined()

    // Cleanup: resolve to avoid hanging
    resolve([])
  })

  it('returns sites data after API resolves', async () => {
    const mockSites: Site[] = [
      { siteId: '1', siteName: 'My Blog', status: 'active', siteUrl: 'https://cdn.example.com/u/1/', createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-02T00:00:00Z' },
      { siteId: '2', siteName: 'Portfolio', status: 'active', siteUrl: null, createdAt: '2026-02-01T00:00:00Z', updatedAt: '2026-02-02T00:00:00Z' },
    ]
    mockGet.mockResolvedValue(mockSites)

    const wrapper = mountWithQuery()

    await vi.waitFor(() => {
      expect(wrapper.vm.isLoading).toBe(false)
    })

    expect(wrapper.vm.sites).toEqual(mockSites)
    expect(wrapper.vm.error).toBeNull()
    expect(mockGet).toHaveBeenCalledWith('/sites')
  })

  it('createSiteMutation calls POST /sites and invalidates query cache', async () => {
    mockGet.mockResolvedValue([])
    const mockResponse: CreateSiteResponse = { siteId: '3', siteName: 'New Site', contentPath: 'user/3/' }
    mockPost.mockResolvedValue(mockResponse)

    const wrapper = mountWithQuery()

    await wrapper.vm.createSiteMutation.mutateAsync('New Site')

    expect(mockPost).toHaveBeenCalledWith('/sites', { siteName: 'New Site' })
    expect(mockInvalidateQueries).toHaveBeenCalledWith({ queryKey: ['sites'] })
  })

  it('refetch triggers a new GET request', async () => {
    mockGet.mockResolvedValue([])

    const wrapper = mountWithQuery()

    await vi.waitFor(() => {
      expect(wrapper.vm.isLoading).toBe(false)
    })

    mockGet.mockClear()
    mockGet.mockResolvedValue([{ siteId: '1', siteName: 'Updated', status: 'active', siteUrl: null, createdAt: '', updatedAt: '' }])

    await wrapper.vm.refetch()

    expect(mockGet).toHaveBeenCalledWith('/sites')
  })
})
