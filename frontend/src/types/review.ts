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

export interface FileReviewSummary {
  file_path: string
  added_lines: number
  removed_lines: number
  skipped: boolean
  summary: string
  observations: string[]
  changes: LineChange[]
}

export interface ReviewNarrative {
  verdict: string
  overview: string
  reviewer_notes: string[]
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
