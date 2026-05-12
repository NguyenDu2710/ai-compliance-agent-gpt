<template>
  <div class="app-shell">
    <SideBar />
    <main class="workspace">
      <TopBar />
      <RouterView />
    </main>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { RouterView } from 'vue-router'
import SideBar from './components/SideBar.vue'
import TopBar from './components/TopBar.vue'
import { useReviewStore } from './stores/review'

const store = useReviewStore()

onMounted(() => {
  store.loadSecretStatus()
})
</script>
