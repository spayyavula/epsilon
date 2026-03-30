export interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface ConversationDto {
  id: string;
  title: string;
  providerId: string | null;
  modelId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface MessageDto {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  model: string | null;
  createdAt: string;
}

export interface StreamChunk {
  delta: string;
  done: boolean;
}

export interface ToolDefinitionDto {
  toolType: string;
  displayName: string;
  icon: string;
  description: string;
  accentColor: string;
  steps: StepDefinitionDto[];
}

export interface StepDefinitionDto {
  index: number;
  label: string;
  inputLabel: string;
  inputPlaceholder: string | null;
  isAutoGenerate: boolean;
}

export interface ResearchProjectDto {
  id: string;
  toolType: string;
  title: string;
  currentStep: number;
  status: string;
  providerId: string | null;
  modelId: string | null;
  webSearchEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ResearchStepDto {
  id: string;
  stepIndex: number;
  userInput: string;
  generatedContent: string;
  status: string;
  generatedAt: string | null;
}

export interface DocumentDto {
  id: string;
  fileName: string;
  mimeType: string;
  sizeBytes: number;
  pageCount: number | null;
  chunkCount: number;
  status: string;
  ingestedAt: string;
}

export interface FlashcardDto {
  id: string;
  front: string;
  back: string;
  category: string;
  easeFactor: number;
  intervalDays: number;
  repetitions: number;
  nextReview: string;
  createdAt: string;
}

export interface ProviderDto {
  id: string;
  name: string;
  requiresApiKey: boolean;
}

export interface ModelDto {
  id: string;
  name: string;
  provider: string;
}

export interface ApiKeyStatusDto {
  providerId: string;
  isConfigured: boolean;
  updatedAt: string | null;
}
