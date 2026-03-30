import { useEffect, useState, useCallback } from 'react';
import { BookOpen, Plus } from 'lucide-react';
import { api } from '../api/client';
import { MarkdownMath } from '../components/math/MarkdownMath';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Textarea } from '../components/ui/Textarea';
import type { FlashcardDto } from '../types/api';

export function FlashcardsPage() {
  const [tab, setTab] = useState<'review' | 'manage'>('review');
  const [dueCards, setDueCards] = useState<FlashcardDto[]>([]);
  const [allCards, setAllCards] = useState<FlashcardDto[]>([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [flipped, setFlipped] = useState(false);
  const [showCreate, setShowCreate] = useState(false);
  const [front, setFront] = useState('');
  const [back, setBack] = useState('');
  const [category, setCategory] = useState('General');

  const loadDue = useCallback(async () => {
    const cards = await api.get<FlashcardDto[]>('/flashcards/due');
    setDueCards(cards);
    setCurrentIndex(0);
    setFlipped(false);
  }, []);

  const loadAll = useCallback(async () => {
    const cards = await api.get<FlashcardDto[]>('/flashcards');
    setAllCards(cards);
  }, []);

  useEffect(() => { loadDue(); loadAll(); }, [loadDue, loadAll]);

  const currentCard = dueCards[currentIndex];

  const handleReview = async (quality: number) => {
    if (!currentCard) return;
    await api.post(`/flashcards/${currentCard.id}/review`, { quality });
    setFlipped(false);
    if (currentIndex < dueCards.length - 1) {
      setCurrentIndex((i) => i + 1);
    } else {
      await loadDue();
    }
  };

  const handleCreate = async () => {
    if (!front.trim() || !back.trim()) return;
    await api.post('/flashcards', { front, back, category });
    setFront(''); setBack(''); setShowCreate(false);
    loadAll(); loadDue();
  };

  return (
    <div className="h-full overflow-y-auto p-4 space-y-5 pb-20">
      <div className="flex items-center justify-between animate-fade-in">
        <div>
          <h2 className="text-lg font-semibold">Flashcards</h2>
          <p className="text-sm text-text-muted">Spaced repetition for math mastery</p>
        </div>
        <div className="flex gap-1 bg-bg-secondary/50 rounded-xl p-1 border border-border/50">
          <button
            onClick={() => setTab('review')}
            className={`px-3 py-1.5 rounded-lg text-xs font-medium ${
              tab === 'review' ? 'bg-accent text-white shadow-sm' : 'text-text-muted hover:text-text-primary'
            }`}
          >
            Review ({dueCards.length})
          </button>
          <button
            onClick={() => setTab('manage')}
            className={`px-3 py-1.5 rounded-lg text-xs font-medium ${
              tab === 'manage' ? 'bg-accent text-white shadow-sm' : 'text-text-muted hover:text-text-primary'
            }`}
          >
            Manage
          </button>
        </div>
      </div>

      {tab === 'review' && (
        <div className="max-w-lg mx-auto space-y-5 animate-fade-in">
          {currentCard ? (
            <>
              {/* Card */}
              <div className="perspective">
                <div
                  onClick={() => setFlipped(!flipped)}
                  className={`relative w-full min-h-[220px] cursor-pointer preserve-3d ${flipped ? 'rotate-y-180' : ''}`}
                  style={{ transition: 'transform 0.5s' }}
                >
                  {/* Front */}
                  <div className="absolute inset-0 backface-hidden bg-bg-secondary/50 border border-border/50 rounded-2xl p-6 flex items-center justify-center">
                    <MarkdownMath content={currentCard.front} />
                  </div>
                  {/* Back */}
                  <div className="absolute inset-0 backface-hidden rotate-y-180 bg-bg-secondary/50 border border-accent/20 rounded-2xl p-6 flex items-center justify-center">
                    <MarkdownMath content={currentCard.back} />
                  </div>
                </div>
              </div>

              <div className="flex items-center justify-between">
                <span className="text-xs text-text-muted">{flipped ? 'Answer' : 'Tap to reveal'}</span>
                <span className="text-xs text-text-muted">{currentIndex + 1} / {dueCards.length}</span>
              </div>

              {flipped && (
                <div className="flex justify-center gap-3 animate-fade-in">
                  <Button variant="danger" size="lg" onClick={() => handleReview(1)} className="flex-1 max-w-[120px]">Hard</Button>
                  <Button variant="secondary" size="lg" onClick={() => handleReview(3)} className="flex-1 max-w-[120px]">Good</Button>
                  <Button variant="primary" size="lg" onClick={() => handleReview(5)} className="flex-1 max-w-[120px]">Easy</Button>
                </div>
              )}
            </>
          ) : (
            <div className="text-center py-16">
              <div className="w-16 h-16 rounded-2xl bg-bg-secondary/50 border border-border/50 flex items-center justify-center mx-auto mb-4">
                <BookOpen size={28} className="text-text-muted/50" />
              </div>
              <p className="text-sm font-medium text-text-primary">All caught up!</p>
              <p className="text-xs text-text-muted mt-1">No cards due for review right now</p>
            </div>
          )}
        </div>
      )}

      {tab === 'manage' && (
        <div className="space-y-4 animate-fade-in">
          <Button size="sm" onClick={() => setShowCreate(!showCreate)}>
            <Plus size={14} /> New Card
          </Button>

          {showCreate && (
            <div className="bg-bg-secondary/30 border border-border/50 rounded-2xl p-5 space-y-3 animate-fade-in">
              <Textarea value={front} onChange={(e) => setFront(e.target.value)} placeholder="Front — question or concept..." rows={2} />
              <Textarea value={back} onChange={(e) => setBack(e.target.value)} placeholder="Back — answer or explanation..." rows={2} />
              <Input value={category} onChange={(e) => setCategory(e.target.value)} placeholder="Category (e.g., Linear Algebra)" />
              <div className="flex gap-2">
                <Button size="sm" onClick={handleCreate} disabled={!front.trim() || !back.trim()}>Create</Button>
                <Button variant="ghost" size="sm" onClick={() => setShowCreate(false)}>Cancel</Button>
              </div>
            </div>
          )}

          <div className="space-y-2">
            {allCards.map((card, i) => (
              <div key={card.id} className="bg-bg-secondary/30 border border-border/50 rounded-xl p-3.5 animate-fade-in" style={{ animationDelay: `${i * 30}ms` }}>
                <div className="text-sm"><MarkdownMath content={card.front} /></div>
                <div className="flex items-center gap-2 mt-2 pt-2 border-t border-border/30 text-[11px] text-text-muted">
                  <span className="px-1.5 py-0.5 rounded-md bg-bg-tertiary/50">{card.category}</span>
                  <span>{card.repetitions} reviews</span>
                  <span>·</span>
                  <span>Next: {new Date(card.nextReview).toLocaleDateString()}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
