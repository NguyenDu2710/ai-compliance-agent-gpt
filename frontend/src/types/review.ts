export interface FileDiff {
  file_path: string
  diff: string
}

export interface DiffRequest {
  repo: string
  src_branch: string
  dest_branch: string
  files: FileDiff[]
}

export interface Violation {
  rule_id: string
  description: string
  file: string
  line: number
  severity: string
  suggestion: string
}

export interface Impact {
  file: string
  line: number
  issue: string
  severity: string
}

export interface LineChange {
  type: string
  old_line: number
  new_line: number
  content: string
}

export interface Observation {
  severity: 'critical' | 'high' | 'medium' | 'low'
  confidence: number
  category: string
  message: string
  impact: string
  recommendation: string
}

export interface CodeQuality {
  cleanliness?: string
  solid_principles?: string
  documentation?: string
  standards_compliance?: string
  test_coverage?: string
}

export interface FileReviewSummary {
  file_path: string
  summary?: string
  code_quality?: CodeQuality
  observations?: Observation[]
  required_actions?: string[]
  added_lines?: number
  removed_lines?: number
  skipped?: boolean
  changes?: LineChange[]
}

export interface ReviewNarrative {
  verdict: string
  overview: string
  merge_recommendation?: {
    status: 'safe_with_review' | 'block' | 'low_risk'
    reason: string
  }
  key_changes?: string[]
  risk_assessment?: {
    behavior_change: boolean
    breaking_change: boolean
    security_risk: boolean
    data_flow_impact: boolean
    performance_impact: boolean
  }
  reviewer_notes: string[]
  required_actions?: string[]
  changed_files: FileReviewSummary[]
}

export interface AnalysisResult {
  status: string
  violations: Violation[]
  impacts: Impact[]
  review: ReviewNarrative
  summary: unknown
}

export interface ReviewRecord {
  id: string
  title: string
  createdAt: string
  request: DiffRequest
  result: AnalysisResult
}

export interface ReviewFromUrlResponse {
  request: DiffRequest
  result: AnalysisResult
}
