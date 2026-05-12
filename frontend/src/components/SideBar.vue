<template>
  <aside class="sidebar">
    <div class="brand">
      <div class="brand-icon"><Bot :size="24" /></div>
      <span>AI Code Reviewer</span>
    </div>

    <nav class="nav-list">
      <RouterLink v-for="item in items" :key="item.to" :to="item.to" class="nav-item">
        <component :is="item.icon" :size="17" />
        <span>{{ item.label }}</span>
      </RouterLink>
    </nav>

    <section class="plan-card">
      <Crown :size="20" />
      <strong>Pro Plan</strong>
      <p>Unlimited reviews, private projects and more.</p>
      <button>Upgrade Now</button>
    </section>

    <section class="profile-card">
      <div class="avatar">N</div>
      <div>
        <strong>Nguyen Van A</strong>
        <span>Senior Developer</span>
      </div>
      <ChevronDown :size="16" />
    </section>
  </aside>
</template>

<script setup lang="ts">
import { Bot, ChevronDown, Clock3, Crown, FileSearch, Gauge, Settings, SlidersHorizontal } from 'lucide-vue-next'

const items = [
  { to: '/new-review', label: 'New Review', icon: FileSearch },
  { to: '/reviews', label: 'Reviews', icon: Gauge },
  { to: '/history', label: 'History', icon: Clock3 },
  { to: '/settings', label: 'Settings', icon: Settings },
  { to: '/api-config', label: 'API Configuration', icon: SlidersHorizontal },
]
</script>
