import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { defineComponent } from 'vue'
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query'
import { usePublishSite } from '@/composables/usePublishSite'
import type { PublishSiteResponse } from '@/types/api'

const { mockPutForm, mockInvalidateQueries } = vi.hoisted(() => ({
  mockPutForm: vi.fn(),
  mockInvalidateQueries: vi.fn(),
}))

vi.mock('@/composables/useApi', () => ({
  useApiClient: () => ({
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
    putForm: mockPutForm,
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

function mountWithQuery(siteId: string) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  const TestComponent = defineComponent({
    setup() {
      return usePublishSite(siteId)
    },
    template: '<div></div>',
  })

  return mount(TestComponent, {
    global: {
      plugins: [[VueQueryPlugin, { queryClient }]],
    },
  })
}

describe('usePublishSite', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('calls PUT /sites/:siteId/content with FormData and returns siteUrl', async () => {
    const mockResponse: PublishSiteResponse = { siteUrl: 'https://cdn.example.com/user/1/' }
    mockPutForm.mockResolvedValue(mockResponse)

    const wrapper = mountWithQuery('site-123')

    expect(wrapper.vm.progress).toBe(0)
    expect(wrapper.vm.publishedUrl).toBeNull()

    const file = new File(['test content'], 'site.zip', { type: 'application/zip' })
    await wrapper.vm.publishMutation.mutateAsync(file)

    expect(mockPutForm).toHaveBeenCalledWith(
      '/sites/site-123/content',
      expect.any(FormData),
      expect.objectContaining({ onUploadProgress: expect.any(Function) }),
    )

    // Verify FormData contains the file
    const call = mockPutForm.mock.calls[0] as [string, FormData]
    const formData = call[1]
    expect(formData.get('file')).toBe(file)

    expect(wrapper.vm.publishedUrl).toBe('https://cdn.example.com/user/1/')
    expect(mockInvalidateQueries).toHaveBeenCalledWith({ queryKey: ['sites'] })
  })

  it('updates progress during upload', async () => {
    mockPutForm.mockImplementation(
      (_path: string, _formData: FormData, options?: { onUploadProgress?: (p: number) => void }) => {
        // Simulate progress callback
        options?.onUploadProgress?.(0.5)
        options?.onUploadProgress?.(1.0)
        return Promise.resolve({ siteUrl: 'https://cdn.example.com/user/1/' })
      },
    )

    const wrapper = mountWithQuery('site-123')
    const file = new File(['test'], 'site.zip', { type: 'application/zip' })

    expect(wrapper.vm.progress).toBe(0)
    await wrapper.vm.publishMutation.mutateAsync(file)
    expect(wrapper.vm.progress).toBe(100)
  })
})
