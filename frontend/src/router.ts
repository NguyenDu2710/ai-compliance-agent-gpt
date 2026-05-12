import { createRouter, createWebHistory } from 'vue-router'
import NewReview from './views/NewReview.vue'
import ReviewHistory from './views/ReviewHistory.vue'
import SettingsView from './views/SettingsView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/new-review' },
    { path: '/new-review', component: NewReview },
    { path: '/reviews', component: ReviewHistory },
    { path: '/history', component: ReviewHistory },
    { path: '/settings', component: SettingsView },
    { path: '/api-config', component: SettingsView },
  ],
})

export default router
