<template>
  <div class="review-layout">
    <section class="main-column">
      <div class="repo-card">
        <div class="repo-icon"><Github :size="28" /></div>
        <div>
          <h1>{{ store.request.repo || 'New repository review' }}</h1>
          <p>
            Comparing changes
            <span>{{ store.request.files.length }} files changed</span>
            <b class="add">+{{ store.totalAdded }}</b>
            <b class="remove">-{{ store.totalRemoved }}</b>
          </p>
        </div>
        <button class="primary" @click="store.submitReview" :disabled="store.loading">
          <RefreshCw :size="16" :class="{ spin: store.loading }" />
          Re-run Review
        </button>
      </div>

      <div class="input-strip">
        <div class="url-review">
          <Search :size="16" />
          <input
            v-model="store.reviewUrl"
            placeholder="Paste GitHub PR or GitLab MR link"
            @keyup.enter="store.submitReviewFromUrl"
          />
          <button @click="store.submitReviewFromUrl" :disabled="store.loading">
            <GitPullRequestArrow :size="16" />
            Fetch & Review
          </button>
        </div>
        <RouterLink class="provider-status" to="/api-config">
          <KeyRound :size="16" />
          {{ tokenStatus }}
        </RouterLink>
        <label class="upload-button">
          <Upload :size="16" />
          Upload diff JSON
          <input type="file" accept=".json,application/json" @change="readFile" />
        </label>
        <button @click="loadSample"><FileJson2 :size="16" /> Load sample</button>
        <span v-if="store.error" class="error-text">{{ store.error }}</span>
      </div>

      <DiffViewer />

      <section class="comment-card">
        <h2><Bot :size="18" /> AI Review Comment</h2>
        <p>{{ store.result?.review.verdict || 'Ready to review this pull request.' }}</p>
        <ul>
          <li v-for="note in notes" :key="note">{{ note }}</li>
        </ul>
        <footer>
          <span>AI Model: Gemini / Local analyzer</span>
          <span>{{ store.loading ? 'Running...' : 'Ready' }}</span>
        </footer>
      </section>
    </section>

    <section class="side-column">
      <ReviewSummary />
      <section class="history-card">
        <div class="history-head">
          <h2>Review History</h2>
          <RouterLink to="/history">View all</RouterLink>
        </div>
        <p v-if="!store.history.length" class="muted">No reviews yet.</p>
        <article v-for="record in store.history.slice(0, 3)" :key="record.id">
          <strong>{{ record.title }}</strong>
          <span>{{ format(record.createdAt) }}</span>
          <b>{{ scoreFor(record.result).toFixed(1) }}</b>
        </article>
      </section>
    </section>
  </div>
</template>

<script setup lang="ts">
import dayjs from 'dayjs'
import { computed } from 'vue'
import { Bot, FileJson2, Github, GitPullRequestArrow, KeyRound, RefreshCw, Search, Upload } from 'lucide-vue-next'
import DiffViewer from '@/components/DiffViewer.vue'
import ReviewSummary from '@/components/ReviewSummary.vue'
import { useReviewStore } from '@/stores/review'
import { sampleReviewRequest } from '@/sampleReview'
import type { AnalysisResult, DiffRequest } from '@/types/review'

const store = useReviewStore()
const notes = computed(() => store.result?.review.reviewer_notes?.length ? store.result.review.reviewer_notes : ['Run a review to receive notes, violations and changed-file summaries.'])
const tokenStatus = computed(() => {
  const connected = [store.hasGeminiApiKey && 'Gemini', store.hasGitHubToken && 'GitHub', store.hasGitLabToken && 'GitLab'].filter(Boolean)
  return connected.length ? `Connected: ${connected.join(', ')}` : 'Connect API keys'
})

function loadSample() {
  store.setRequest(structuredClone(sampleReviewRequest))
}

async function readFile(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return

  try {
    const payload = JSON.parse(await file.text()) as DiffRequest
    store.setRequest(payload)
  } catch {
    store.error = 'Invalid JSON file.'
  } finally {
    input.value = ''
  }
}

function format(value: string) {
  return dayjs(value).format('HH:mm DD/MM/YYYY')
}

function scoreFor(result: AnalysisResult) {
  const high = result.violations.filter((item) => item.severity.toLowerCase() === 'high').length
  const medium = result.violations.filter((item) => item.severity.toLowerCase() === 'medium').length
  return Math.max(1, 10 - high * 1.4 - medium * 0.8)
}
</script>
