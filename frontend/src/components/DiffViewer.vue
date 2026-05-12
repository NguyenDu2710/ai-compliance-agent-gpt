<template>
  <section class="diff-card">
    <div class="file-tabs">
      <div class="file-tabs-scroll">
        <button
          v-for="(file, index) in store.request.files"
          :key="file.file_path"
          :class="{ active: index === store.activeFile }"
          @click="store.activeFile = index"
        >
          <FileCode2 :size="15" />
          <span class="file-name">{{ file.file_path }}</span>
          <span class="add">+{{ count(file.diff, '+') }}</span>
          <span class="remove">-{{ count(file.diff, '-') }}</span>
        </button>
      </div>
      <div class="file-count-block">
        <button type="button" class="file-nav" @click="prevFile" :disabled="!canPrev" aria-label="Previous file">‹</button>
        <span class="file-count">{{ store.activeFile + 1 }} / {{ store.request.files.length }}</span>
        <button type="button" class="file-nav" @click="nextFile" :disabled="!canNext" aria-label="Next file">›</button>
      </div>
    </div>

    <!-- File-specific review feedback above diff -->
    <div v-if="currentFileReview" class="file-feedback">
      <div class="feedback-header">
        <h3>{{ currentFileReview.file_path }}</h3>
      </div>

      <!-- Summary -->
      <div v-if="currentFileReview.summary" class="feedback-section">
        <p>{{ currentFileReview.summary }}</p>
      </div>

      <!-- Code Quality Assessment -->
      <div v-if="currentFileReview.code_quality" class="feedback-section">
        <h4>Code Quality</h4>
        <div class="quality-grid">
          <div v-if="currentFileReview.code_quality.cleanliness" class="quality-item">
            <span class="label">Cleanliness:</span>
            <span :class="['value', currentFileReview.code_quality.cleanliness]">{{ currentFileReview.code_quality.cleanliness }}</span>
          </div>
          <div v-if="currentFileReview.code_quality.solid_principles" class="quality-item">
            <span class="label">SOLID:</span>
            <span class="value">{{ currentFileReview.code_quality.solid_principles }}</span>
          </div>
          <div v-if="currentFileReview.code_quality.documentation" class="quality-item">
            <span class="label">Docs:</span>
            <span class="value">{{ currentFileReview.code_quality.documentation }}</span>
          </div>
          <div v-if="currentFileReview.code_quality.standards_compliance" class="quality-item">
            <span class="label">Standards:</span>
            <span class="value">{{ currentFileReview.code_quality.standards_compliance }}</span>
          </div>
        </div>
      </div>

      <!-- Observations/Issues -->
      <div v-if="currentFileReview.observations?.length" class="feedback-section">
        <h4>Findings ({{ currentFileReview.observations.length }})</h4>
        <div class="observations-list">
          <div v-for="obs in currentFileReview.observations" :key="`${obs.category}-${obs.message}`" class="observation-item" :class="obs.severity">
            <div class="obs-header">
              <span class="severity-badge" :class="obs.severity">{{ obs.severity }}</span>
              <span class="category-badge">{{ obs.category }}</span>
            </div>
            <p class="obs-message">{{ obs.message }}</p>
            <div v-if="obs.impact" class="obs-impact">
              <strong>Impact:</strong> {{ obs.impact }}
            </div>
            <div v-if="obs.recommendation" class="obs-recommendation">
              <strong>Recommendation:</strong> {{ obs.recommendation }}
            </div>
          </div>
        </div>
      </div>

      <!-- Required Actions -->
      <div v-if="currentFileReview.required_actions?.length" class="feedback-section">
        <h4>Manual Verification</h4>
        <ol class="actions-list">
          <li v-for="(action, index) in currentFileReview.required_actions" :key="index">{{ action }}</li>
        </ol>
      </div>
    </div>

    <div class="diff-table" v-if="store.currentFile">
      <div v-for="(line, index) in parsedLines" :key="index" class="diff-row" :class="line.kind">
        <span class="line-no">{{ line.oldLine || '' }}</span>
        <span class="line-no">{{ line.newLine || '' }}</span>
        <code>{{ line.content }}</code>
      </div>
    </div>

    <footer class="diff-footer">
      <span><i class="legend removed"></i> Removed</span>
      <span><i class="legend added"></i> Added</span>
      <div class="view-modes">
        <button type="button" :class="{ active: viewMode === 'split' }" @click="setViewMode('split')">Split</button>
        <button type="button" :class="{ active: viewMode === 'unified' }" @click="setViewMode('unified')">Unified</button>
        <button type="button" :class="{ active: viewMode === 'side-by-side' }" @click="setViewMode('side-by-side')">Side by side</button>
      </div>
    </footer>
  </section>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { FileCode2 } from 'lucide-vue-next'
import { useReviewStore } from '@/stores/review'

const store = useReviewStore()
const viewMode = ref<'split' | 'unified' | 'side-by-side'>('unified')

function setViewMode(mode: 'split' | 'unified' | 'side-by-side') {
  viewMode.value = mode
}

function count(diff: string, marker: '+' | '-') {
  return diff.split('\n').filter((line) => line.startsWith(marker) && !line.startsWith(`${marker}${marker}${marker}`)).length
}

const currentFileReview = computed(() => {
  if (!store.result) return null
  const filePath = store.currentFile?.file_path
  if (!filePath) return null
  return store.result.review.changed_files?.find((f) => f.file_path === filePath)
})

const parsedLines = computed(() => {
  const lines = store.currentFile?.diff.split('\n') ?? []
  let oldLine = 0
  let newLine = 0

  return lines.map((raw) => {
    const hunk = raw.match(/^@@ -(\d+),?\d* \+(\d+),?\d* @@/)
    if (hunk) {
      oldLine = Number(hunk[1])
      newLine = Number(hunk[2])
      return { kind: 'meta', oldLine: '', newLine: '', content: raw }
    }

    if (raw.startsWith('+') && !raw.startsWith('+++')) {
      return { kind: 'added', oldLine: '', newLine: newLine++, content: raw }
    }

    if (raw.startsWith('-') && !raw.startsWith('---')) {
      return { kind: 'removed', oldLine: oldLine++, newLine: '', content: raw }
    }

    return { kind: 'context', oldLine: oldLine++, newLine: newLine++, content: raw }
  })
})

const canPrev = computed(() => store.activeFile > 0)
const canNext = computed(() => store.activeFile < store.request.files.length - 1)

function prevFile() {
  if (canPrev.value) store.activeFile -= 1
}

function nextFile() {
  if (canNext.value) store.activeFile += 1
}

const splitLines = computed(() => {
  const lines = store.currentFile?.diff.split('\n') ?? []
  let oldLine = 0
  let newLine = 0

  return lines.map((raw) => {
    const hunk = raw.match(/^@@ -(\d+),?\d* \+(\d+),?\d* @@/)
    if (hunk) {
      oldLine = Number(hunk[1])
      newLine = Number(hunk[2])
      return { kind: 'meta', oldLine: '', newLine: '', left: raw, right: raw }
    }

    if (raw.startsWith('+') && !raw.startsWith('+++')) {
      return { kind: 'added', oldLine: '', newLine: newLine++, left: '', right: raw.slice(1) }
    }

    if (raw.startsWith('-') && !raw.startsWith('---')) {
      return { kind: 'removed', oldLine: oldLine++, newLine: '', left: raw.slice(1), right: '' }
    }

    return { kind: 'context', oldLine: oldLine++, newLine: newLine++, left: raw, right: raw }
  })
})
</script>
