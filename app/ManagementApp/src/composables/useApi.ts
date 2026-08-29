import { QueryClient } from '@tanstack/vue-query'
import { useAuthStore } from '@/stores/auth'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
    },
  },
})

export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
    this.name = 'ApiError'
  }
}

async function request<T>(
  method: string,
  path: string,
  body?: unknown,
): Promise<T> {
  const auth = useAuthStore()
  const baseUrl = import.meta.env.VITE_API_BASE_URL || ''

  const headers: Record<string, string> = {}
  if (auth.accessToken) {
    headers['Authorization'] = `Bearer ${auth.accessToken}`
  }
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  const res = await fetch(`${baseUrl}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (!res.ok) {
    throw new ApiError(res.status, await res.text())
  }

  if (res.status === 204) {
    return undefined as T
  }

  return res.json() as Promise<T>
}

function requestForm<T>(
  path: string,
  formData: FormData,
  options?: { onUploadProgress?: (progress: number) => void },
): Promise<T> {
  return new Promise((resolve, reject) => {
    const auth = useAuthStore()
    const baseUrl = import.meta.env.VITE_API_BASE_URL || ''

    const xhr = new XMLHttpRequest()
    xhr.open('PUT', `${baseUrl}${path}`)

    xhr.setRequestHeader('Authorization', `Bearer ${auth.accessToken}`)

    xhr.upload.onprogress = (event) => {
      if (event.lengthComputable && options?.onUploadProgress) {
        options.onUploadProgress(event.loaded / event.total)
      }
    }

    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        resolve(JSON.parse(xhr.responseText) as T)
        return
      }
      if (xhr.status === 401) {
        auth.logout()
        reject(new ApiError(401, 'Unauthorized'))
        return
      }
      let message = xhr.responseText
      try {
        const parsed = JSON.parse(xhr.responseText)
        message = parsed.error || parsed.detail || message
      } catch {
        /* use raw text */
      }
      reject(new ApiError(xhr.status, message))
    }

    xhr.onerror = () => reject(new ApiError(0, 'Network error'))
    xhr.onabort = () => reject(new ApiError(0, 'Upload cancelled'))

    xhr.send(formData)
  })
}

export function useApiClient() {
  return {
    get<T>(path: string): Promise<T> {
      return request<T>('GET', path)
    },
    post<T>(path: string, body?: unknown): Promise<T> {
      return request<T>('POST', path, body)
    },
    put<T>(path: string, body?: unknown): Promise<T> {
      return request<T>('PUT', path, body)
    },
    delete<T>(path: string): Promise<T> {
      return request<T>('DELETE', path)
    },
    putForm<T>(
      path: string,
      formData: FormData,
      options?: { onUploadProgress?: (progress: number) => void },
    ): Promise<T> {
      return requestForm<T>(path, formData, options)
    },
  }
}
