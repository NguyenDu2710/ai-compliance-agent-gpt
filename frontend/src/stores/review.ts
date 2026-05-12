import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import {
  clearSecretConfiguration,
  getApiErrorMessage,
  getSecretConfiguration,
  reviewFromUrl,
  runReview,
  saveSecretConfiguration,
  testReviewApi,
} from '@/services/api'
import type { AnalysisResult, DiffRequest, FileDiff, ReviewRecord } from '@/types/review'
import { sampleReviewRequest } from '@/sampleReview'

export const useReviewStore = defineStore('review', () => {
  const request = ref<DiffRequest>({ ...sampleReviewRequest })
  const result = ref<AnalysisResult | null>(null)
  const activeFile = ref(0)
  const reviewUrl = ref('')
  const geminiApiKey = ref('')
  const githubToken = ref('')
  const gitlabToken = ref('')
  const hasGeminiApiKey = ref(false)
  const hasGitHubToken = ref(false)
  const hasGitLabToken = ref(false)
  const loading = ref(false)
  const error = ref('')
  const notice = ref('')
  const history = ref<ReviewRecord[]>([])

  const currentFile = computed<FileDiff | null>(() => request.value.files[activeFile.value] ?? null)
  const totalAdded = computed(() => request.value.files.reduce((sum, file) => sum + countLines(file.diff, '+'), 0))
  const totalRemoved = computed(() => request.value.files.reduce((sum, file) => sum + countLines(file.diff, '-'), 0))
  const score = computed(() => {
    if (!result.value) return 0
    const high = result.value.violations.filter((item) => item.severity.toLowerCase() === 'high').length
    const medium = result.value.violations.filter((item) => item.severity.toLowerCase() === 'medium').length
    const low = result.value.violations.filter((item) => item.severity.toLowerCase() === 'low').length
    return Math.max(1, Number((10 - high * 1.4 - medium * 0.8 - low * 0.35).toFixed(1)))
  })

  function countLines(diff: string, marker: '+' | '-') {
    return diff.split('\n').filter((line) => line.startsWith(marker) && !line.startsWith(`${marker}${marker}${marker}`)).length
  }

  function setRequest(next: DiffRequest) {
    request.value = next
    activeFile.value = 0
    result.value = null
    error.value = ''
  }

  async function submitReview() {
    loading.value = true
    error.value = ''
    try {
      result.value = await runReview(request.value)
      history.value.unshift({
        id: crypto.randomUUID(),
        title: request.value.repo,
        createdAt: new Date().toISOString(),
        request: structuredClone(request.value),
        result: structuredClone(result.value),
      })
    } catch (err) {
      error.value = getApiErrorMessage(err, 'Cannot connect to review API.')
    } finally {
      loading.value = false
    }
  }

  async function submitReviewFromUrl() {
    if (!reviewUrl.value.trim()) {
      error.value = 'Paste a GitHub PR or GitLab MR link first.'
      return
    }

    loading.value = true
    error.value = ''
    try {
      const response = await reviewFromUrl(reviewUrl.value.trim())
      request.value = response.request
      activeFile.value = 0
      result.value = response.result
      history.value.unshift({
        id: crypto.randomUUID(),
        title: response.request.repo,
        createdAt: new Date().toISOString(),
        request: structuredClone(response.request),
        result: structuredClone(response.result),
      })
    } catch (err) {
      error.value = getApiErrorMessage(err, 'Cannot fetch or review this pull/merge request.')
    } finally {
      loading.value = false
    }
  }

  async function testConnection() {
    loading.value = true
    error.value = ''
    try {
      result.value = await testReviewApi()
    } catch (err) {
      error.value = getApiErrorMessage(err, 'Cannot connect to review API.')
    } finally {
      loading.value = false
    }
  }

  function saveTokens() {
    return saveSecrets()
  }

  async function saveSecrets() {
    loading.value = true
    error.value = ''
    notice.value = ''
    try {
      const status = await saveSecretConfiguration({
        geminiApiKey: geminiApiKey.value,
        gitHubToken: githubToken.value,
        gitLabToken: gitlabToken.value,
      })
      applySecretStatus(status)
      geminiApiKey.value = ''
      githubToken.value = ''
      gitlabToken.value = ''
      notice.value = 'Secrets saved as HttpOnly cookies.'
    } catch (err) {
      error.value = getApiErrorMessage(err, 'Cannot save secret configuration.')
    } finally {
      loading.value = false
    }
  }

  async function clearTokens() {
    return clearSecrets()
  }

  async function clearSecrets() {
    loading.value = true
    error.value = ''
    notice.value = ''
    try {
      const status = await clearSecretConfiguration()
      applySecretStatus(status)
      geminiApiKey.value = ''
      githubToken.value = ''
      gitlabToken.value = ''
      notice.value = 'Secrets removed from this browser.'
    } catch (err) {
      error.value = getApiErrorMessage(err, 'Cannot clear secret configuration.')
    } finally {
      loading.value = false
    }
  }

  async function loadSecretStatus() {
    try {
      applySecretStatus(await getSecretConfiguration())
    } catch {
      hasGeminiApiKey.value = false
      hasGitHubToken.value = false
      hasGitLabToken.value = false
    }
  }

  function applySecretStatus(status: { hasGeminiApiKey: boolean; hasGitHubToken: boolean; hasGitLabToken: boolean }) {
    hasGeminiApiKey.value = status.hasGeminiApiKey
    hasGitHubToken.value = status.hasGitHubToken
    hasGitLabToken.value = status.hasGitLabToken
  }

  function clearSecretInputs() {
    geminiApiKey.value = ''
    githubToken.value = ''
    gitlabToken.value = ''
  }

  return {
    request,
    result,
    activeFile,
    reviewUrl,
    geminiApiKey,
    githubToken,
    gitlabToken,
    hasGeminiApiKey,
    hasGitHubToken,
    hasGitLabToken,
    loading,
    error,
    notice,
    history,
    currentFile,
    totalAdded,
    totalRemoved,
    score,
    setRequest,
    submitReview,
    submitReviewFromUrl,
    testConnection,
    saveTokens,
    clearTokens,
    saveSecrets,
    clearSecrets,
    loadSecretStatus,
    clearSecretInputs,
  }
})
