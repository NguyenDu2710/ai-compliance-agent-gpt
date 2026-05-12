import axios from 'axios'
import type { AnalysisResult, DiffRequest, ReviewFromUrlResponse } from '@/types/review'

const api = axios.create({
  baseURL: getApiBaseUrl(),
  timeout: 120000,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
})

function getApiBaseUrl() {
  if (import.meta.env.VITE_API_BASE_URL) {
    return import.meta.env.VITE_API_BASE_URL
  }

  if (typeof window !== 'undefined') {
    return `${window.location.protocol}//${window.location.hostname}:63152`
  }

  return 'http://localhost:63152'
}

export async function runReview(payload: DiffRequest): Promise<AnalysisResult> {
  const { data } = await api.post<AnalysisResult>('/api/review', payload)
  return data
}

export async function testReviewApi(): Promise<AnalysisResult> {
  const { data } = await api.post<AnalysisResult>('/api/review/test')
  return data
}

export async function reviewFromUrl(url: string): Promise<ReviewFromUrlResponse> {
  const { data } = await api.post<ReviewFromUrlResponse>('/api/review/from-url', {
    url,
  })
  return data
}

export interface SecretConfigurationPayload {
  geminiApiKey?: string
  gitHubToken?: string
  gitLabToken?: string
}

export interface SecretConfigurationStatus {
  hasGeminiApiKey: boolean
  hasGitHubToken: boolean
  hasGitLabToken: boolean
}

export async function getSecretConfiguration(): Promise<SecretConfigurationStatus> {
  const { data } = await api.get<SecretConfigurationStatus>('/api/config/secrets')
  return data
}

export async function saveSecretConfiguration(payload: SecretConfigurationPayload): Promise<SecretConfigurationStatus> {
  const { data } = await api.post<SecretConfigurationStatus>('/api/config/secrets', payload)
  return data
}

export async function clearSecretConfiguration(): Promise<SecretConfigurationStatus> {
  const { data } = await api.delete<SecretConfigurationStatus>('/api/config/secrets')
  return data
}

export async function checkHealth(): Promise<{ status: string }> {
  const { data } = await api.get<{ status: string }>('/health')
  return data
}

export function getApiErrorMessage(err: unknown, fallback: string) {
  if (axios.isAxiosError(err)) {
    const message = err.response?.data?.error || err.response?.data?.message || err.message
    return typeof message === 'string' ? message : fallback
  }

  return err instanceof Error ? err.message : fallback
}
