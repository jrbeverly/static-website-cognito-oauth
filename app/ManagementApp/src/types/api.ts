export interface Site {
  siteId: string
  siteName: string
  status: 'active'
  siteUrl: string | null
  createdAt: string
  updatedAt: string
}

export interface CreateSiteResponse {
  siteId: string
  siteName: string
  contentPath: string
}

export interface SiteDetailResponse {
  siteId: string
  siteName: string
  status: string
  siteUrl: string | null
  createdAt: string
  updatedAt: string
  contentPath: string
}

export interface PublishSiteResponse {
  siteUrl: string
}
