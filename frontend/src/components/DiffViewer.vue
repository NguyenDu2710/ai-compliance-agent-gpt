<template>
  <section class="diff-card">
    <div class="file-tabs">
      <button
        v-for="(file, index) in store.request.files"
        :key="file.file_path"
        :class="{ active: index === store.activeFile }"
        @click="store.activeFile = index"
      >
        <FileCode2 :size="15" />
        {{ file.file_path }}
        <span class="add">+{{ count(file.diff, '+') }}</span>
        <span class="remove">-{{ count(file.diff, '-') }}</span>
      </button>
      <span class="file-count">{{ store.activeFile + 1 }} / {{ store.request.files.length }} files</span>
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
        <button>Split</button>
        <button class="active">Unified</button>
        <button>Side by side</button>
      </div>
    </footer>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { FileCode2 } from 'lucide-vue-next'
import { useReviewStore } from '@/stores/review'

const store = useReviewStore()

function count(diff: string, marker: '+' | '-') {
  return diff.split('\n').filter((line) => line.startsWith(marker) && !line.startsWith(`${marker}${marker}${marker}`)).length
}

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
</script>
