<template>
  <header class="top-bar">
    <div class="page-title">
      <button class="back-link" type="button" @click="goBack" aria-label="Go back">
        <ArrowLeft :size="18" />
      </button>
      <strong>New Review</strong>
    </div>

    <div class="top-actions">
      <button class="icon-button" title="Toggle theme" type="button" @click="toggleTheme">
        <SunMedium :size="16" />
      </button>
      <label class="switch" title="Dark mode">
        <input type="checkbox" v-model="dark" />
        <span></span>
      </label>
      <RouterLink to="/api-config" class="api-config">
        <Settings2 :size="16" />
        <span>API Config</span>
      </RouterLink>
    </div>
  </header>
</template>

<script setup lang="ts">
import { watch, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ArrowLeft, Settings2, SunMedium } from 'lucide-vue-next'

const router = useRouter()
const dark = ref(false)

function toggleTheme() {
  dark.value = !dark.value
}

function goBack() {
  if (window.history.length > 1) {
    router.back()
  } else {
    router.push('/new-review')
  }
}

onMounted(() => {
  const saved = localStorage.getItem('theme')
  dark.value = saved === 'dark'
})

watch(
  dark,
  (enabled) => {
    document.documentElement.dataset.theme = enabled ? 'dark' : 'light'
    localStorage.setItem('theme', enabled ? 'dark' : 'light')
  },
  { immediate: true },
)
</script>
