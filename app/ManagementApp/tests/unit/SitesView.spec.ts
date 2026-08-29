import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { ref } from 'vue'
import SitesView from '@/views/SitesView.vue'

const mockSites = ref<Array<{
  siteId: string
  siteName: string
  status: 'active'
  siteUrl: string | null
  createdAt: string
  updatedAt: string
}>>([])

const mockCreateSiteMutation = {
  mutateAsync: vi.fn().mockResolvedValue({}),
  isPending: ref(false),
  error: ref(null),
}

vi.mock('@/composables/useSites', () => ({
  useSites: () => ({
    sites: mockSites,
    isLoading: ref(false),
    error: ref(null),
    createSiteMutation: mockCreateSiteMutation,
  }),
}))

vi.mock('vue-router', () => ({
  useRouter: () => ({
    push: vi.fn(),
  }),
}))

function mountSitesView() {
  return mount(SitesView, {
    attachTo: document.body,
  })
}

describe('SitesView', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    mockSites.value = []
    mockCreateSiteMutation.mutateAsync.mockReset()
  })

  it('renders empty state when no sites exist', () => {
    const wrapper = mountSitesView()
    expect(wrapper.text()).toContain('No sites yet')
    expect(wrapper.text()).toContain('Create your first site')
  })

  it('renders sites in data table when sites exist', () => {
    mockSites.value = [
      {
        siteId: '1',
        siteName: 'My Blog',
        status: 'active' as const,
        siteUrl: 'https://cdn.example.com/user-a/1/',
        createdAt: '2026-06-01T10:00:00Z',
        updatedAt: '2026-06-04T12:00:00Z',
      },
      {
        siteId: '2',
        siteName: 'Portfolio',
        status: 'active' as const,
        siteUrl: null,
        createdAt: '2026-06-02T10:00:00Z',
        updatedAt: '2026-06-03T12:00:00Z',
      },
    ]

    const wrapper = mountSitesView()

    expect(wrapper.text()).toContain('My Blog')
    expect(wrapper.text()).toContain('Portfolio')
    expect(wrapper.text()).toContain('Not published')
  })

  it('shows create dialog when New Site button is clicked', async () => {
    const wrapper = mountSitesView()

    const newSiteBtn = wrapper.find('.v-btn')
    await newSiteBtn.trigger('click')

    const dialog = wrapper.findComponent({ name: 'VDialog' })
    expect(dialog.props('modelValue')).toBe(true)
  })

  it('renders dialog with form fields when opened', async () => {
    const wrapper = mountSitesView()

    const newSiteBtn = wrapper.find('.v-btn')
    await newSiteBtn.trigger('click')

    // Dialog content is teleported to body
    expect(document.body.textContent).toContain('Create New Site')
    expect(document.body.textContent).toContain('Site Name')
    expect(document.body.textContent).toContain('Cancel')
    expect(document.body.textContent).toContain('Create')
  })
})
