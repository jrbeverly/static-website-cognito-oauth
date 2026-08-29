import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes = [
  {
    path: '/',
    redirect: '/sites',
  },
  {
    path: '/sites',
    component: () => import('@/components/AppLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'sites',
        component: () => import('@/views/SitesView.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: ':siteId',
        name: 'site-detail',
        component: () => import('@/views/SiteDetailView.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: ':siteId/publish',
        name: 'site-publish',
        component: () => import('@/views/SitePublishView.vue'),
        meta: { requiresAuth: true },
      },
    ],
  },
  {
    path: '/callback',
    name: 'callback',
    component: () => import('@/views/CallbackView.vue'),
  },
]

export const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    auth.login()
    return false
  }
})
