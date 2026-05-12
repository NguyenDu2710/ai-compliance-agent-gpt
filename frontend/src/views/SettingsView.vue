<template>
  <section class="page-card settings-grid">
    <div>
      <h1>API Configuration</h1>
      <p class="muted">Frontend calls the ASP.NET Core API through this base URL.</p>
    </div>

    <label>
      API Base URL
      <input :value="apiBaseUrl" readonly />
    </label>

    <div class="token-panel">
      <div class="token-head">
        <div>
          <h2>Private API Keys</h2>
          <p class="muted">Keys are stored by the API as HttpOnly cookies. The frontend cannot read them after saving.</p>
        </div>
      </div>

      <label>
        Gemini API Key
        <div class="secret-field">
          <KeyRound :size="17" />
          <input
            v-model="store.geminiApiKey"
            :type="showSecrets ? 'text' : 'password'"
            placeholder="AIza..."
            autocomplete="off"
          />
          <span v-if="store.hasGeminiApiKey" class="secret-badge">Saved</span>
        </div>
      </label>

      <label>
        GitHub Token
        <div class="secret-field">
          <Github :size="17" />
          <input
            v-model="store.githubToken"
            :type="showSecrets ? 'text' : 'password'"
            placeholder="ghp_... or github_pat_..."
            autocomplete="off"
          />
          <span v-if="store.hasGitHubToken" class="secret-badge">Saved</span>
        </div>
      </label>

      <label>
        GitLab Token
        <div class="secret-field">
          <Gitlab :size="17" />
          <input
            v-model="store.gitlabToken"
            :type="showSecrets ? 'text' : 'password'"
            placeholder="glpat-..."
            autocomplete="off"
          />
          <span v-if="store.hasGitLabToken" class="secret-badge">Saved</span>
        </div>
      </label>

      <label class="checkbox-row">
        <input v-model="showSecrets" type="checkbox" />
        Show tokens
      </label>

      <div class="settings-actions">
        <button class="primary" @click="store.saveSecrets" :disabled="store.loading">
          <Save :size="16" />
          Save Secrets
        </button>
        <button class="secondary" @click="store.clearSecrets" :disabled="store.loading">
          <Trash2 :size="16" />
          Clear Cookies
        </button>
      </div>
    </div>

    <button class="primary" @click="store.testConnection" :disabled="store.loading">
      <PlugZap :size="16" />
      Test API Connection
    </button>

    <p v-if="store.notice" class="success-text">{{ store.notice }}</p>
    <p v-if="store.error" class="error-text">{{ store.error }}</p>
    <pre v-if="store.result">{{ JSON.stringify(store.result, null, 2) }}</pre>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { Github, Gitlab, KeyRound, PlugZap, Save, Trash2 } from 'lucide-vue-next'
import { useReviewStore } from '@/stores/review'

const store = useReviewStore()
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:63152'
const showSecrets = ref(false)

onMounted(() => {
  store.loadSecretStatus()
})
</script>
