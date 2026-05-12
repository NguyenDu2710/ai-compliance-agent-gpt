<template>
  <section class="page-card">
    <h1>Review History</h1>
    <p class="muted">Completed reviews from this browser session.</p>
    <article v-for="record in store.history" :key="record.id" class="history-line">
      <div>
        <strong>{{ record.title }}</strong>
        <span>{{ record.request.src_branch }} -> {{ record.request.dest_branch }}</span>
      </div>
      <b>{{ record.result.status }}</b>
    </article>
    <p v-if="!store.history.length" class="empty-state">No review has been completed yet.</p>
  </section>
</template>

<script setup lang="ts">
import { useReviewStore } from '@/stores/review'

const store = useReviewStore()
</script>
