<template>
  <aside class="summary-panel">
    <div class="panel-head">
      <h2><Sparkles :size="18" /> AI Review Summary</h2>
      <div>
        <div class="score-ring">{{ displayScore }}</div>
        <p class="score-note">{{ scoreNote }}</p>
      </div>
    </div>

    <p class="overview">{{ store.result?.review.overview || emptyText }}</p>

    <div class="metric-list">
      <div v-for="metric in metrics" :key="metric.label" class="metric-row">
        <span><component :is="metric.icon" :size="16" /> {{ metric.label }}</span>
        <b :class="metric.state">{{ metric.text }}</b>
      </div>
    </div>

    <section class="issues">
      <h3><CircleAlert :size="16" /> Top Issues ({{ issues.length }})</h3>
      <ol>
        <li v-for="issue in issues.slice(0, 3)" :key="issueKey(issue)">
          <span>{{ issueText(issue) }}</span>
          <b :class="issue.severity.toLowerCase()">{{ issue.severity || 'low' }}</b>
        </li>
      </ol>
    </section>

    <button class="primary wide" @click="store.submitReview" :disabled="store.loading">
      <RefreshCw :size="16" :class="{ spin: store.loading }" />
      {{ store.result ? 'Re-run Review' : 'Run Review' }}
    </button>
  </aside>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { CircleAlert, Code2, Gauge, RefreshCw, ShieldCheck, Sparkles, Wrench } from 'lucide-vue-next'
import { useReviewStore } from '@/stores/review'
import type { Impact, Violation } from '@/types/review'

const store = useReviewStore()
const emptyText = 'Upload a diff JSON or run the sample review to see AI review insights from the C# backend.'
const displayScore = computed(() => (store.result ? store.score.toFixed(1) : '--'))
const issues = computed(() => [...(store.result?.violations ?? []), ...(store.result?.impacts ?? [])])
const hasSecurity = computed(() => issues.value.some((issue) => /secret|token|security|auth/i.test(issueText(issue))))
const scoreNote = computed(() => {
  if (!store.result) return 'Score is a heuristic estimate based on review findings.'
  return 'Score is a heuristic estimate based on severity and number of review findings.'
})

const metrics = computed(() => [
  { label: 'Code Quality', icon: Code2, text: store.result ? 'Good' : 'Pending', state: 'good' },
  { label: 'Performance', icon: Gauge, text: 'Good', state: 'good' },
  { label: 'Security', icon: ShieldCheck, text: hasSecurity.value ? 'Needs Review' : 'Good', state: hasSecurity.value ? 'warn' : 'good' },
  { label: 'Maintainability', icon: Wrench, text: issues.value.length > 3 ? 'Needs Review' : 'Good', state: issues.value.length > 3 ? 'warn' : 'good' },
])

function issueText(issue: Violation | Impact) {
  return 'description' in issue ? issue.description : issue.issue
}

function issueKey(issue: Violation | Impact) {
  return `${issue.file}-${issue.line}-${issue.severity}-${issueText(issue)}`
}
</script>
