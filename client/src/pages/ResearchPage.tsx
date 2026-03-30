import { useEffect, useState, useRef } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { ArrowLeft, Send, Square, FileDown } from 'lucide-react';
import { api } from '../api/client';
import { useSSE } from '../hooks/useSSE';
import { useUiStore } from '../stores/uiStore';
import { MarkdownMath } from '../components/math/MarkdownMath';
import { Button } from '../components/ui/Button';
import { Textarea } from '../components/ui/Textarea';
import type { ToolDefinitionDto, ResearchProjectDto, ResearchStepDto } from '../types/api';

const toolGradients: Record<string, string> = {
  ProofBuilder: 'from-emerald-500 to-teal-600',
  ProblemSolver: 'from-rose-500 to-pink-600',
  ConceptExplorer: 'from-violet-500 to-purple-600',
  LeanBridge: 'from-blue-500 to-indigo-600',
  PracticeMode: 'from-amber-500 to-orange-600',
};

const toolExamples: Record<string, string[]> = {
  ProofBuilder: [
    'Prove that √2 is irrational',
    'Prove that there are infinitely many primes',
    'Prove the Bolzano-Weierstrass theorem',
    'Prove that every finite group of order p² is abelian',
    'Prove the Cauchy-Schwarz inequality',
    'Prove that a continuous function on [a,b] is uniformly continuous',
  ],
  ProblemSolver: [
    'Find all solutions to x⁴ - 5x² + 4 = 0',
    'Evaluate ∫₀^∞ e^(-x²) dx',
    'Find the eigenvalues of [[3,1],[0,2]]',
    'Solve the recurrence aₙ = 3aₙ₋₁ - 2aₙ₋₂ with a₀=1, a₁=3',
    'Find the radius of convergence of Σ(n!xⁿ/nⁿ)',
    'Minimize f(x,y) = x² + y² subject to x + y = 1',
  ],
  ConceptExplorer: [
    'Explain eigenvalues and eigenvectors intuitively',
    'What is a topological space?',
    'How does Galois theory connect fields and groups?',
    'Explain the Lebesgue integral vs Riemann integral',
    'What are homomorphisms and isomorphisms?',
    'Explain the fundamental theorem of calculus',
  ],
  LeanBridge: [
    'Formalize: sum of first n naturals = n(n+1)/2',
    'Prove in Lean 4: √2 is irrational',
    'Formalize the proof that 0 < 1 in ℝ',
    'Write a Lean proof of the triangle inequality',
    'Formalize: every natural number is even or odd',
    'Prove in Lean: composition of injections is injective',
  ],
  PracticeMode: [
    'Practice proof by mathematical induction (beginner)',
    'Epsilon-delta limit proofs (intermediate)',
    'Group theory proofs (intermediate)',
    'Contraction mapping problems (advanced)',
    'Combinatorics and counting arguments (beginner)',
    'Real analysis: sequences and series (intermediate)',
  ],
};

export function ResearchPage() {
  const { id } = useParams();
  if (id) return <ProjectView projectId={id} />;
  return <ToolSelector />;
}

function ToolSelector() {
  const [tools, setTools] = useState<ToolDefinitionDto[]>([]);
  const [projects, setProjects] = useState<ResearchProjectDto[]>([]);
  const navigate = useNavigate();
  const { selectedProviderId, selectedModelId } = useUiStore();

  useEffect(() => {
    api.get<ToolDefinitionDto[]>('/research/tools').then(setTools);
    api.get<ResearchProjectDto[]>('/research/projects').then(setProjects);
  }, []);

  const createProject = async (toolType: string, exampleInput?: string) => {
    const project = await api.post<ResearchProjectDto>('/research/projects', {
      toolType, providerId: selectedProviderId, modelId: selectedModelId,
    });
    navigate(`/research/${project.id}`, { state: exampleInput ? { initialInput: exampleInput } : undefined });
  };

  return (
    <div className="h-full overflow-y-auto p-4 space-y-8 pb-20">
      <div className="animate-fade-in">
        <h2 className="text-lg font-semibold mb-1">Research Toolkit</h2>
        <p className="text-sm text-text-muted mb-6">Guided workflows for mathematical research. Pick a tool and try an example to get started.</p>

        <div className="space-y-6">
          {tools.map((tool, i) => {
            const examples = toolExamples[tool.toolType] || [];
            return (
              <div
                key={tool.toolType}
                className="bg-bg-secondary/40 border border-border/50 rounded-2xl p-5 animate-fade-in"
                style={{ animationDelay: `${i * 80}ms` }}
              >
                {/* Tool header */}
                <div className="flex items-start gap-3.5 mb-4">
                  <div className={`w-11 h-11 rounded-xl bg-gradient-to-br ${toolGradients[tool.toolType] || 'from-accent to-purple-500'} flex items-center justify-center text-xl shadow-lg shrink-0`}>
                    {tool.icon}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between">
                      <h3 className="font-semibold text-text-primary">{tool.displayName}</h3>
                      <Button size="sm" onClick={() => createProject(tool.toolType)}>Start New</Button>
                    </div>
                    <p className="text-xs text-text-muted mt-1 leading-relaxed">{tool.description}</p>
                    <div className="flex gap-1.5 mt-2">
                      {tool.steps.map((s) => (
                        <div key={s.index} className="flex items-center gap-1">
                          <div className="h-1 w-6 rounded-full bg-border" />
                          <span className="text-[9px] text-text-muted">{s.label}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>

                {/* Examples grid */}
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2">
                  {examples.map((ex) => (
                    <button
                      key={ex}
                      onClick={() => createProject(tool.toolType, ex)}
                      className="text-left text-xs bg-bg-primary/60 border border-border/40 rounded-xl px-3.5 py-2.5 text-text-secondary hover:text-accent hover:border-accent/30 hover:bg-accent/[0.03]"
                    >
                      {ex}
                    </button>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {projects.length > 0 && (
        <div className="animate-fade-in" style={{ animationDelay: '200ms' }}>
          <h3 className="text-sm font-semibold text-text-secondary mb-3">Recent Projects</h3>
          <div className="space-y-2">
            {projects.map((p) => (
              <button
                key={p.id}
                onClick={() => navigate(`/research/${p.id}`)}
                className="w-full text-left bg-bg-secondary/30 border border-border/50 rounded-xl p-3.5 hover:border-border-hover group"
              >
                <div className="flex items-center justify-between">
                  <span className="text-sm font-medium truncate group-hover:text-accent">{p.title}</span>
                  <span className="text-[10px] px-2 py-0.5 rounded-full bg-bg-tertiary text-text-muted">{p.toolType}</span>
                </div>
                <div className="flex items-center gap-2 mt-1.5">
                  <div className="flex gap-1">
                    {Array.from({ length: 5 }).map((_, i) => (
                      <div key={i} className={`h-1 w-4 rounded-full ${i <= p.currentStep ? 'bg-accent' : 'bg-border/50'}`} />
                    ))}
                  </div>
                  <span className="text-[11px] text-text-muted">{new Date(p.updatedAt).toLocaleDateString()}</span>
                </div>
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function ProjectView({ projectId }: { projectId: string }) {
  const [project, setProject] = useState<ResearchProjectDto | null>(null);
  const [tools, setTools] = useState<ToolDefinitionDto[]>([]);
  const [steps, setSteps] = useState<ResearchStepDto[]>([]);
  const [activeStep, setActiveStep] = useState(0);
  const [stepInput, setStepInput] = useState('');
  const { content, isStreaming, stream, stop } = useSSE();
  const navigate = useNavigate();
  const location = useLocation();
  const autoSubmitted = useRef(false);

  useEffect(() => {
    api.get<ToolDefinitionDto[]>('/research/tools').then(setTools);
    api.get<ResearchProjectDto>(`/research/projects/${projectId}`).then(setProject);
    api.get<ResearchStepDto[]>(`/research/projects/${projectId}/steps`).then(setSteps);
  }, [projectId]);

  // Auto-fill and auto-generate from example
  const initialInput = (location.state as { initialInput?: string } | null)?.initialInput;
  useEffect(() => {
    if (initialInput && !autoSubmitted.current && project && tools.length > 0) {
      autoSubmitted.current = true;
      setStepInput(initialInput);
      // Auto-submit after a tick to let state settle
      setTimeout(async () => {
        await api.put(`/research/projects/${projectId}/steps/0`, { userInput: initialInput });
        const result = await stream(`/research/projects/${projectId}/steps/0/generate`, {});
        if (result) {
          const updated = await api.get<ResearchStepDto[]>(`/research/projects/${projectId}/steps`);
          setSteps(updated);
          setStepInput('');
        }
      }, 100);
    }
  }, [initialInput, project, tools, projectId, stream]);

  const tool = tools.find(t => t.toolType === project?.toolType);
  const currentStep = steps[activeStep];
  const stepDef = tool?.steps[activeStep];
  const lastStepDone = steps.length > 0 && steps[steps.length - 1]?.status === 'done';

  // Step-specific example suggestions
  const stepExamples = getStepExamples(project?.toolType, activeStep);

  const handleViewPdf = async () => {
    const token = localStorage.getItem('epsilon-auth');
    let accessToken = '';
    if (token) {
      try { accessToken = JSON.parse(token)?.state?.token || ''; } catch {}
    }
    const res = await fetch(`/api/research/projects/${projectId}/pdf`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    if (!res.ok) return;
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    window.open(url, '_blank');
  };

  const handleGenerate = async () => {
    if (!stepInput.trim() && !stepDef?.isAutoGenerate) return;
    if (stepInput.trim()) {
      await api.put(`/research/projects/${projectId}/steps/${activeStep}`, { userInput: stepInput });
    }
    await stream(`/research/projects/${projectId}/steps/${activeStep}/generate`, {});
    const updated = await api.get<ResearchStepDto[]>(`/research/projects/${projectId}/steps`);
    setSteps(updated);
    setStepInput('');
  };

  return (
    <div className="flex flex-col h-full">
      <header className="px-4 py-3 border-b border-border/50 glass bg-bg-secondary/30 flex items-center gap-3">
        <button onClick={() => navigate('/research')} className="w-7 h-7 rounded-lg flex items-center justify-center text-text-muted hover:text-text-primary hover:bg-black/5">
          <ArrowLeft size={16} />
        </button>
        <h2 className="text-sm font-medium truncate flex-1">{project?.title || 'Project'}</h2>
        {lastStepDone && (
          <Button variant="secondary" size="sm" onClick={handleViewPdf}>
            <FileDown size={14} /> View PDF
          </Button>
        )}
      </header>

      {tool && (
        <div className="flex border-b border-border/50 bg-bg-secondary/20 overflow-x-auto px-2">
          {tool.steps.map((s) => (
            <button
              key={s.index}
              onClick={() => setActiveStep(s.index)}
              className={`px-4 py-2.5 text-xs font-medium whitespace-nowrap border-b-2 ${
                activeStep === s.index
                  ? 'border-accent text-accent'
                  : 'border-transparent text-text-muted hover:text-text-secondary'
              }`}
            >
              {s.label}
            </button>
          ))}
        </div>
      )}

      <div className="flex-1 overflow-y-auto p-4">
        <div className="max-w-3xl mx-auto">
          {currentStep?.generatedContent && !isStreaming && (
            <div className="bg-bg-secondary/40 border border-border/50 rounded-2xl p-5 animate-fade-in">
              <MarkdownMath content={currentStep.generatedContent} />
            </div>
          )}
          {isStreaming && content && (
            <div className="bg-bg-secondary/40 border border-border/50 rounded-2xl p-5 animate-fade-in">
              <MarkdownMath content={content} />
            </div>
          )}
          {!currentStep?.generatedContent && !isStreaming && (
            <div className="text-center py-12 text-text-muted text-sm">
              {stepDef?.isAutoGenerate ? 'Click generate below' : 'Enter your input below to begin'}
            </div>
          )}
        </div>
      </div>

      {stepDef && !stepDef.isAutoGenerate && (
        <div className="border-t border-border/50 bg-bg-secondary/20 glass p-4">
          <div className="max-w-3xl mx-auto">
            <p className="text-xs text-text-muted mb-2">{stepDef.inputLabel}</p>
            {stepExamples.length > 0 && !currentStep?.generatedContent && !stepInput && (
              <div className="flex flex-wrap gap-1.5 mb-3">
                {stepExamples.map((ex) => (
                  <button
                    key={ex}
                    onClick={() => setStepInput(ex)}
                    className="text-[11px] bg-bg-primary/60 border border-border/40 rounded-lg px-2.5 py-1.5 text-text-muted hover:text-accent hover:border-accent/30"
                  >
                    {ex}
                  </button>
                ))}
              </div>
            )}
          </div>
          <div className="flex gap-2.5 items-end max-w-3xl mx-auto">
            <Textarea value={stepInput} onChange={(e) => setStepInput(e.target.value)} placeholder={stepDef.inputPlaceholder || ''} rows={2} className="min-h-[60px] bg-bg-secondary/50" />
            {isStreaming ? (
              <Button variant="danger" onClick={stop} className="shrink-0 h-[46px] w-[46px] p-0"><Square size={16} /></Button>
            ) : (
              <Button onClick={handleGenerate} disabled={!stepInput.trim()} className="shrink-0 h-[46px] w-[46px] p-0"><Send size={16} /></Button>
            )}
          </div>
        </div>
      )}

      {stepDef?.isAutoGenerate && currentStep?.status !== 'done' && (
        <div className="border-t border-border/50 bg-bg-secondary/20 glass p-4">
          <div className="max-w-3xl mx-auto">
            <Button onClick={handleGenerate} disabled={isStreaming} className="w-full" size="lg">
              {isStreaming ? (
                <><div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" /> Generating...</>
              ) : `Generate ${stepDef.label}`}
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}

function getStepExamples(toolType?: string, stepIndex?: number): string[] {
  if (!toolType || stepIndex === undefined) return [];

  const examples: Record<string, Record<number, string[]>> = {
    ProofBuilder: {
      0: [
        'Prove that \u221a2 is irrational',
        'Prove there are infinitely many primes',
        'Prove the Cauchy-Schwarz inequality',
        'Prove every convergent sequence is bounded',
        'Prove a continuous bijection from a compact space has continuous inverse',
        'Prove the fundamental theorem of algebra',
      ],
      1: [
        'Proof by contradiction \u2014 assume \u221a2 = p/q and derive a contradiction',
        'Try strong induction on the number of prime factors',
        'Use the epsilon-delta definition directly',
        'Apply the pigeonhole principle to show a repeat must occur',
        'Proof by contrapositive might be cleaner here',
        'Mathematical induction on n, with a careful base case',
      ],
      2: [
        'Here is my attempt at the proof...',
        'I got stuck after showing p must be even \u2014 how do I continue?',
        'Construct the full proof using the strategy we discussed',
        'I want to try a different approach using topology',
        'Let me write out the induction step in detail',
        'Please show the full formal argument',
      ],
      3: [
        'Does this proof handle the case when n = 0?',
        'I am worried about the step where we divide by (a - b)',
        'Check if the converse is also true',
        'Are there any unstated assumptions we missed?',
        'What if the space is not Hausdorff?',
        'Can this result be generalized to higher dimensions?',
      ],
    },
    ProblemSolver: {
      0: [
        'Find all solutions to x\u2074 - 5x\u00b2 + 4 = 0',
        'Evaluate \u222b\u2080^\u03c0 sin\u00b3(x) dx',
        'Find the eigenvalues of [[4,2],[1,3]]',
        'Solve: dy/dx + 2y = e^(-x)',
        'Find lim(n\u2192\u221e) (1 + 1/n)^n',
        'Minimize x\u00b2 + y\u00b2 subject to x + y = 10',
      ],
      1: [
        'solve',
        'hint',
        'Can you explain why we use substitution here?',
        'Show me an alternative method',
        'I got a different answer \u2014 where did I go wrong?',
        'What is the geometric interpretation?',
      ],
    },
    ConceptExplorer: {
      0: [
        'Explain eigenvalues and eigenvectors',
        'What is a topological space?',
        'How does the Fourier transform work?',
        'Explain group homomorphisms',
        'What is measure theory?',
        'Explain the concept of compactness',
      ],
      1: [
        'Prove the spectral theorem',
        'Give me more examples with matrices',
        'How does this connect to differential equations?',
        'What are the real-world applications?',
        'Generate practice problems on this topic',
        'Explain the historical development',
      ],
    },
    LeanBridge: {
      0: [
        'Prove by induction: sum of first n naturals = n(n+1)/2',
        'Prove that 0 is not equal to 1 in the natural numbers',
        'Prove: if a | b and b | c, then a | c',
        'Prove the triangle inequality for real numbers',
        'Prove that every natural number is either even or odd',
        'Prove: composition of injective functions is injective',
      ],
      1: [
        'translate',
        'Explain the simp tactic in detail',
        'How does omega work for arithmetic goals?',
        'Show me the term-mode proof instead',
        'What Mathlib lemmas could simplify this?',
        'How do I handle the inductive step in Lean?',
      ],
      2: [
        'Now prove the converse',
        'Can you make it work without Mathlib?',
        'Try a harder theorem: irrationality of \u221a2',
        'Teach me the ring tactic with examples',
        'How do I define custom structures in Lean 4?',
        'Show me how to debug type errors in Lean',
      ],
    },
    PracticeMode: {
      0: [
        'Proof by mathematical induction \u2014 beginner level',
        'Epsilon-delta limit proofs \u2014 intermediate',
        'Group theory: subgroups and cosets \u2014 intermediate',
        'Real analysis: sequences and convergence \u2014 advanced',
        'Linear algebra: eigenspaces \u2014 intermediate',
        'Combinatorics and counting \u2014 beginner',
      ],
      1: [
        'solutions',
        'Here is my proof for problem 1...',
        'I think the base case is n = 1, then assume for k...',
        'For the challenge problem, I tried contradiction...',
        'Can I get another hint for problem 3?',
        'I am stuck on the inductive step',
      ],
    },
  };

  return examples[toolType]?.[stepIndex] || [];
}
