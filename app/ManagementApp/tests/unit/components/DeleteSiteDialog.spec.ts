import { describe, it, expect, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import DeleteSiteDialog from '@/components/DeleteSiteDialog.vue'

function mountDialog(props?: {
  modelValue?: boolean
  siteName?: string
  loading?: boolean
  errorMessage?: string | null
}) {
  return mount(DeleteSiteDialog, {
    props: {
      modelValue: true,
      siteName: 'My Blog',
      loading: false,
      errorMessage: null,
      ...props,
    },
    attachTo: document.body,
  })
}

function getDeleteButton(): HTMLButtonElement | undefined {
  const buttons = document.querySelectorAll('button')
  return Array.from(buttons).find((b) => b.textContent?.trim() === 'Delete') as
    | HTMLButtonElement
    | undefined
}

function getCancelButton(): HTMLButtonElement | undefined {
  const buttons = document.querySelectorAll('button')
  return Array.from(buttons).find((b) => b.textContent?.trim() === 'Cancel') as
    | HTMLButtonElement
    | undefined
}

function setInputValue(value: string) {
  const input = document.body.querySelector('input[type="text"]') as HTMLInputElement
  const nativeInputValueSetter = Object.getOwnPropertyDescriptor(
    HTMLInputElement.prototype,
    'value',
  )?.set
  nativeInputValueSetter?.call(input, value)
  input.dispatchEvent(new Event('input', { bubbles: true }))
}

describe('DeleteSiteDialog', () => {
  beforeEach(() => {
    document.body.innerHTML = ''
  })

  it('dialog is hidden when modelValue is false', () => {
    const wrapper = mountDialog({ modelValue: false })
    expect(wrapper.findComponent({ name: 'VDialog' }).props('modelValue')).toBe(false)
  })

  it('dialog is visible when modelValue is true', () => {
    const wrapper = mountDialog({ modelValue: true })
    expect(wrapper.findComponent({ name: 'VDialog' }).props('modelValue')).toBe(true)
  })

  it('renders the site name in the confirmation prompt', () => {
    mountDialog()
    expect(document.body.textContent).toContain('Delete Site?')
    expect(document.body.textContent).toContain('Type the site name to confirm')
  })

  it('delete button is disabled when confirmation input does not match site name', () => {
    mountDialog({ siteName: 'My Blog' })
    const deleteBtn = getDeleteButton()
    expect(deleteBtn).not.toBeUndefined()
    expect(deleteBtn!.disabled).toBe(true)
  })

  it('delete button enables when input matches site name exactly', async () => {
    const wrapper = mountDialog({ siteName: 'My Blog' })

    setInputValue('My Blog')
    await wrapper.vm.$nextTick()

    const deleteBtn = getDeleteButton()
    expect(deleteBtn).not.toBeUndefined()
    expect(deleteBtn!.disabled).toBe(false)
  })

  it('emits confirm when delete button is clicked with matching input', async () => {
    const wrapper = mountDialog({ siteName: 'My Blog' })

    setInputValue('My Blog')
    await wrapper.vm.$nextTick()

    const deleteBtn = getDeleteButton()
    expect(deleteBtn).not.toBeUndefined()
    deleteBtn!.click()

    expect(wrapper.emitted('confirm')).toBeTruthy()
    expect(wrapper.emitted('confirm')).toHaveLength(1)
  })

  it('cancel button emits update:modelValue false', async () => {
    const wrapper = mountDialog({ modelValue: true })

    const cancelBtn = getCancelButton()
    expect(cancelBtn).not.toBeUndefined()
    cancelBtn!.click()

    expect(wrapper.emitted('update:modelValue')).toBeTruthy()
    expect(wrapper.emitted('update:modelValue')![0]).toEqual([false])
  })
})
