<template>
  <header class="top-bar">
    <div class="page-title">
      <RouterLink to="/new-review" class="back-link"><ArrowLeft :size="18" /></RouterLink>
      <strong>New Review</strong>
    </div>

    <div class="top-actions">
      <button class="icon-button" title="Theme"><SunMedium :size="16" /></button>
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
import { watch, ref } from 'vue'
import { ArrowLeft, Settings2, SunMedium } from 'lucide-vue-next'

const dark = ref(false)

watch(
  dark,
  (enabled) => {
    document.documentElement.dataset.theme = enabled ? 'dark' : 'light'
  },
  { immediate: true },
)
</script>
