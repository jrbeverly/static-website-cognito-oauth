import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { VueQueryPlugin } from '@tanstack/vue-query'
import { createVuetify } from 'vuetify'
import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'

import App from './App.vue'
import { router } from './router'
import { queryClient } from './composables/useApi'

const app = createApp(App)

const vuetify = createVuetify({
  defaults: {
    global: {
      ripple: false,
    },
  },
})

app.use(createPinia())
app.use(router)
app.use(VueQueryPlugin, { queryClient })
app.use(vuetify)

app.mount('#app')
