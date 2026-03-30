import { Link } from 'react-router-dom';
import { Sparkles, MessageSquare, Calculator, FlaskConical, FileText, BookOpen, ArrowRight, Check, Zap, Shield, Globe } from 'lucide-react';
import { Button } from '../components/ui/Button';

const features = [
  {
    icon: MessageSquare,
    title: 'AI Math Chat',
    description: 'Chat with GPT-4, Claude, and Gemini about any math topic. Get step-by-step explanations with beautiful LaTeX rendering.',
    gradient: 'from-blue-500 to-indigo-600',
  },
  {
    icon: Calculator,
    title: 'Equation Solver',
    description: 'Solve equations, integrals, limits, ODEs, and more. Every step shown clearly with full working.',
    gradient: 'from-emerald-500 to-teal-600',
  },
  {
    icon: FlaskConical,
    title: 'Research Toolkit',
    description: 'Five guided tools: Proof Builder, Problem Solver, Concept Explorer, Lean 4 Bridge, and Practice Mode.',
    gradient: 'from-violet-500 to-purple-600',
  },
  {
    icon: FileText,
    title: 'Document RAG',
    description: 'Upload PDFs, DOCX, or notes. Epsilon uses them as context to give you grounded, cited answers.',
    gradient: 'from-orange-500 to-amber-600',
  },
  {
    icon: BookOpen,
    title: 'Flashcards',
    description: 'Spaced repetition system for math concepts. Build lasting understanding with SM-2 algorithm scheduling.',
    gradient: 'from-rose-500 to-pink-600',
  },
  {
    icon: Globe,
    title: 'Web Search',
    description: 'Augment answers with academic web sources via Exa. Find papers, proofs, and references automatically.',
    gradient: 'from-cyan-500 to-blue-600',
  },
];

const providers = [
  { name: 'OpenAI', models: 'GPT-4.1, o3, o4-mini' },
  { name: 'Anthropic', models: 'Claude Opus 4, Sonnet 4' },
  { name: 'Google', models: 'Gemini 2.5 Pro, Flash' },
  { name: 'Ollama', models: 'Local models' },
];

export function LandingPage() {
  return (
    <div className="min-h-dvh bg-bg-primary">
      {/* Nav */}
      <nav className="fixed top-0 left-0 right-0 z-50 glass bg-bg-primary/80 border-b border-border/50">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 h-14 flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-accent to-purple-500 flex items-center justify-center shadow-lg shadow-accent/20">
              <Sparkles size={16} className="text-white" />
            </div>
            <span className="font-bold text-text-primary tracking-tight">Epsilon</span>
          </div>
          <div className="flex items-center gap-3">
            <Link to="/login" className="text-sm text-text-secondary hover:text-text-primary font-medium px-3 py-1.5">
              Sign In
            </Link>
            <Link to="/register">
              <Button size="sm">Get Started</Button>
            </Link>
          </div>
        </div>
      </nav>

      {/* Hero */}
      <section className="pt-28 pb-20 px-4 sm:px-6 relative overflow-hidden">
        <div className="absolute top-20 left-1/2 -translate-x-1/2 w-[600px] h-[600px] bg-accent/[0.04] rounded-full blur-[120px] pointer-events-none" />
        <div className="absolute top-40 right-0 w-[400px] h-[400px] bg-purple-500/[0.03] rounded-full blur-[100px] pointer-events-none" />

        <div className="max-w-4xl mx-auto text-center relative animate-fade-in">
          <div className="inline-flex items-center gap-2 bg-accent/5 border border-accent/10 rounded-full px-4 py-1.5 mb-6">
            <Zap size={14} className="text-accent" />
            <span className="text-xs font-medium text-accent">Powered by frontier AI models</span>
          </div>

          <h1 className="text-4xl sm:text-5xl md:text-6xl font-bold text-text-primary tracking-tight leading-[1.1]">
            Your AI-powered
            <br />
            <span className="bg-gradient-to-r from-accent to-purple-500 bg-clip-text text-transparent">
              mathematics research
            </span>
            <br />
            assistant
          </h1>

          <p className="mt-6 text-lg sm:text-xl text-text-secondary max-w-2xl mx-auto leading-relaxed">
            Solve equations, build proofs, explore concepts, and master math with
            step-by-step AI guidance. Beautiful LaTeX rendering on every device.
          </p>

          <div className="mt-10 flex flex-col sm:flex-row items-center justify-center gap-3">
            <Link to="/register">
              <Button size="lg" className="min-w-[180px]">
                Start for Free <ArrowRight size={16} />
              </Button>
            </Link>
            <a href="#features">
              <Button variant="secondary" size="lg" className="min-w-[180px]">
                See Features
              </Button>
            </a>
          </div>

          {/* Provider logos */}
          <div className="mt-14 flex flex-wrap items-center justify-center gap-6">
            {providers.map((p) => (
              <div key={p.name} className="text-center">
                <div className="text-sm font-semibold text-text-primary">{p.name}</div>
                <div className="text-[11px] text-text-muted">{p.models}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Demo preview */}
      <section className="px-4 sm:px-6 pb-20">
        <div className="max-w-4xl mx-auto">
          <div className="bg-bg-secondary border border-border rounded-2xl p-6 sm:p-8 shadow-xl shadow-black/5">
            <div className="flex items-center gap-2 mb-4">
              <div className="w-3 h-3 rounded-full bg-danger/60" />
              <div className="w-3 h-3 rounded-full bg-warning/60" />
              <div className="w-3 h-3 rounded-full bg-success/60" />
              <span className="ml-2 text-xs text-text-muted">Epsilon — Chat</span>
            </div>
            <div className="space-y-4">
              <div className="flex justify-end">
                <div className="bg-accent text-white rounded-2xl rounded-br-lg px-4 py-2.5 text-sm max-w-md">
                  Prove that the square root of 2 is irrational
                </div>
              </div>
              <div className="flex justify-start">
                <div className="bg-bg-tertiary/50 border border-border rounded-2xl rounded-bl-lg px-4 py-3 text-sm max-w-lg text-text-secondary">
                  <p className="font-semibold text-text-primary mb-2">Proof by Contradiction</p>
                  <p>Assume √2 is rational, so √2 = <em>p/q</em> where <em>p, q</em> ∈ ℤ are coprime.</p>
                  <p className="mt-2">Then 2 = <em>p²/q²</em>, so <em>p²</em> = 2<em>q²</em>.</p>
                  <p className="mt-2">This means <em>p²</em> is even, so <em>p</em> is even. Write <em>p</em> = 2<em>k</em>...</p>
                  <p className="mt-2 text-accent font-medium">Contradiction: both p and q are even, violating coprimality. ∎</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Features */}
      <section id="features" className="px-4 sm:px-6 py-20 bg-bg-secondary/50">
        <div className="max-w-6xl mx-auto">
          <div className="text-center mb-14">
            <h2 className="text-3xl sm:text-4xl font-bold text-text-primary tracking-tight">
              Everything you need for math research
            </h2>
            <p className="mt-4 text-text-secondary text-lg max-w-2xl mx-auto">
              From solving homework to building publication-quality proofs
            </p>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {features.map((f, i) => (
              <div
                key={f.title}
                className="bg-bg-primary border border-border rounded-2xl p-6 hover:border-border-hover hover:shadow-lg hover:shadow-black/5 group animate-fade-in"
                style={{ animationDelay: `${i * 80}ms` }}
              >
                <div className={`w-11 h-11 rounded-xl bg-gradient-to-br ${f.gradient} flex items-center justify-center mb-4 shadow-lg group-hover:scale-110`}>
                  <f.icon size={20} className="text-white" />
                </div>
                <h3 className="font-semibold text-text-primary mb-2">{f.title}</h3>
                <p className="text-sm text-text-secondary leading-relaxed">{f.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Why Epsilon */}
      <section className="px-4 sm:px-6 py-20">
        <div className="max-w-4xl mx-auto">
          <div className="text-center mb-14">
            <h2 className="text-3xl sm:text-4xl font-bold text-text-primary tracking-tight">
              Built for serious math
            </h2>
            <p className="mt-4 text-text-secondary text-lg">
              Not just another AI chatbot — purpose-built for mathematical reasoning
            </p>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
            {[
              { icon: Check, title: 'LaTeX everywhere', desc: 'Beautiful math rendering with KaTeX — inline and display mode, on every screen size' },
              { icon: Shield, title: 'Your keys, your data', desc: 'Bring your own API keys. Your conversations and documents are encrypted and isolated per user' },
              { icon: Zap, title: 'Multi-model', desc: 'Switch between GPT-4, Claude, Gemini, or local models with Ollama — use the best model for each task' },
              { icon: Globe, title: 'Works everywhere', desc: 'Progressive Web App — install on your phone, tablet, or desktop. Works offline for flashcard review' },
            ].map((item, i) => (
              <div key={item.title} className="flex gap-4 animate-fade-in" style={{ animationDelay: `${i * 80}ms` }}>
                <div className="w-10 h-10 rounded-xl bg-accent/5 border border-accent/10 flex items-center justify-center shrink-0">
                  <item.icon size={18} className="text-accent" />
                </div>
                <div>
                  <h3 className="font-semibold text-text-primary mb-1">{item.title}</h3>
                  <p className="text-sm text-text-secondary leading-relaxed">{item.desc}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="px-4 sm:px-6 py-20">
        <div className="max-w-3xl mx-auto text-center">
          <div className="bg-gradient-to-br from-accent/5 to-purple-500/5 border border-accent/10 rounded-3xl p-10 sm:p-14">
            <h2 className="text-3xl sm:text-4xl font-bold text-text-primary tracking-tight">
              Ready to level up your math?
            </h2>
            <p className="mt-4 text-text-secondary text-lg">
              Join Epsilon and start solving, proving, and exploring — for free.
            </p>
            <div className="mt-8">
              <Link to="/register">
                <Button size="lg" className="min-w-[200px]">
                  Get Started Free <ArrowRight size={16} />
                </Button>
              </Link>
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t border-border px-4 sm:px-6 py-8">
        <div className="max-w-6xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-2">
            <div className="w-6 h-6 rounded-md bg-gradient-to-br from-accent to-purple-500 flex items-center justify-center">
              <Sparkles size={12} className="text-white" />
            </div>
            <span className="text-sm font-semibold text-text-primary">Epsilon</span>
          </div>
          <p className="text-xs text-text-muted">
            AI-powered mathematics research assistant
          </p>
        </div>
      </footer>
    </div>
  );
}
