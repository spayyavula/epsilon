import { useState } from 'react';
import { Calculator, Send, Square, RotateCcw } from 'lucide-react';
import { useSSE } from '../hooks/useSSE';
import { useUiStore } from '../stores/uiStore';
import { MarkdownMath } from '../components/math/MarkdownMath';
import { Button } from '../components/ui/Button';
import { Textarea } from '../components/ui/Textarea';

const examples = [
  { label: 'Quadratic', text: 'Solve: x\u00b2 - 5x + 6 = 0' },
  { label: 'Integral', text: 'Integrate: \u222b x\u00b7sin(x) dx' },
  { label: 'Limit', text: 'Find the limit: lim(x\u21920) sin(x)/x' },
  { label: 'ODE', text: 'Solve the ODE: dy/dx = 2xy' },
  { label: 'Simplify', text: 'Simplify: (a+b)\u00b3' },
  { label: 'Matrix', text: 'Find eigenvalues of [[2,1],[1,2]]' },
];

export function SolverPage() {
  const [equation, setEquation] = useState('');
  const { content, isStreaming, stream, stop, setContent } = useSSE();
  const { selectedProviderId, selectedModelId } = useUiStore();

  const handleSolve = async () => {
    if (!equation.trim() || isStreaming) return;
    await stream('/solver/solve', {
      equation: equation.trim(),
      providerId: selectedProviderId,
      modelId: selectedModelId,
    });
  };

  return (
    <div className="flex flex-col h-full">
      <header className="px-4 py-3 border-b border-border/50 glass bg-bg-secondary/30">
        <div className="flex items-center gap-2.5">
          <div className="w-7 h-7 rounded-lg bg-gradient-to-br from-emerald-500 to-teal-600 flex items-center justify-center">
            <Calculator size={14} className="text-white" />
          </div>
          <div>
            <h2 className="text-sm font-medium">Equation Solver</h2>
            <p className="text-[11px] text-text-muted">Step-by-step solutions</p>
          </div>
        </div>
      </header>

      <div className="flex-1 overflow-y-auto p-4">
        {!content && !isStreaming && (
          <div className="max-w-2xl mx-auto space-y-6 animate-fade-in">
            <div className="text-center space-y-2 py-6">
              <h3 className="text-lg font-semibold text-text-primary">What would you like to solve?</h3>
              <p className="text-sm text-text-muted">Enter any equation or expression for a detailed solution</p>
            </div>
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
              {examples.map((ex) => (
                <button
                  key={ex.text}
                  onClick={() => setEquation(ex.text)}
                  className="text-left bg-bg-secondary/40 border border-border/50 rounded-xl p-3 hover:border-emerald-500/30 hover:bg-emerald-500/5 group"
                >
                  <span className="text-[10px] font-semibold text-emerald-500 uppercase tracking-wider">{ex.label}</span>
                  <p className="text-xs text-text-secondary mt-1 group-hover:text-text-primary">{ex.text}</p>
                </button>
              ))}
            </div>
          </div>
        )}

        {content && (
          <div className="max-w-3xl mx-auto animate-fade-in">
            <div className="bg-bg-secondary/40 border border-border/50 rounded-2xl p-5">
              <MarkdownMath content={content} />
            </div>
          </div>
        )}

        {isStreaming && !content && (
          <div className="flex items-center justify-center py-12">
            <div className="flex items-center gap-3 text-text-muted text-sm">
              <div className="w-5 h-5 border-2 border-emerald-500/30 border-t-emerald-500 rounded-full animate-spin" />
              Solving...
            </div>
          </div>
        )}
      </div>

      <div className="border-t border-border/50 bg-bg-secondary/20 glass p-4">
        <div className="flex gap-2.5 items-end max-w-3xl mx-auto">
          <Textarea
            value={equation}
            onChange={(e) => setEquation(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSolve(); } }}
            placeholder="Enter equation or expression..."
            rows={1}
            className="min-h-[46px] max-h-32 bg-bg-secondary/50"
          />
          {isStreaming ? (
            <Button variant="danger" onClick={stop} className="shrink-0 h-[46px] w-[46px] p-0"><Square size={16} /></Button>
          ) : (
            <Button onClick={handleSolve} disabled={!equation.trim()} className="shrink-0 h-[46px] w-[46px] p-0"><Send size={16} /></Button>
          )}
          {content && !isStreaming && (
            <Button variant="ghost" onClick={() => { setContent(''); setEquation(''); }} className="shrink-0 h-[46px] w-[46px] p-0">
              <RotateCcw size={16} />
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
