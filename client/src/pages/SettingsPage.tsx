import { useEffect, useState, useCallback } from 'react';
import { Key, Check, X, Loader2, ChevronDown } from 'lucide-react';
import { api } from '../api/client';
import { useAuthStore } from '../stores/authStore';
import { useUiStore } from '../stores/uiStore';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import type { ProviderDto, ModelDto, ApiKeyStatusDto } from '../types/api';

// Additional service keys not in the LLM provider registry
const serviceKeys = [
  { id: 'exa', name: 'Exa Web Search', description: 'Powers web search in chat and research tools' },
];

export function SettingsPage() {
  const [providers, setProviders] = useState<ProviderDto[]>([]);
  const [apiKeys, setApiKeys] = useState<ApiKeyStatusDto[]>([]);
  const [models, setModels] = useState<ModelDto[]>([]);
  const [editingKey, setEditingKey] = useState<string | null>(null);
  const [keyInput, setKeyInput] = useState('');
  const [testing, setTesting] = useState<string | null>(null);
  const [testResults, setTestResults] = useState<Record<string, boolean>>({});
  const [error, setError] = useState('');
  const { selectedProviderId, selectedModelId, setProvider, setModel } = useUiStore();
  const logout = useAuthStore((s) => s.logout);

  const load = useCallback(async () => {
    try {
      setError('');
      const [p, k] = await Promise.all([
        api.get<ProviderDto[]>('/settings/providers'),
        api.get<ApiKeyStatusDto[]>('/settings/api-keys'),
      ]);
      setProviders(p);
      setApiKeys(k);
    } catch (e) {
      setError('Failed to load settings. Please try again.');
    }
  }, []);

  const loadModels = useCallback(async (providerId: string) => {
    try {
      const m = await api.get<ModelDto[]>(`/settings/providers/${providerId}/models`);
      setModels(m);
      if (m.length > 0 && !m.find(mod => mod.id === selectedModelId)) {
        setModel(m[0].id);
      }
    } catch { setModels([]); }
  }, [selectedModelId, setModel]);

  useEffect(() => { load(); }, [load]);
  useEffect(() => { if (providers.length > 0) loadModels(selectedProviderId); }, [selectedProviderId, providers, loadModels]);

  const saveKey = async (providerId: string) => {
    if (!keyInput.trim()) return;
    try {
      await api.put(`/settings/api-keys/${providerId}`, { apiKey: keyInput });
      setEditingKey(null);
      setKeyInput('');
      await load();
      // Reload models in case the key enabled a provider
      loadModels(selectedProviderId);
    } catch {
      setError('Failed to save API key.');
    }
  };

  const testProvider = async (providerId: string) => {
    setTesting(providerId);
    try {
      const res = await api.post<{ connected: boolean }>(`/settings/providers/${providerId}/test`);
      setTestResults((r) => ({ ...r, [providerId]: res.connected }));
    } catch {
      setTestResults((r) => ({ ...r, [providerId]: false }));
    } finally {
      setTesting(null);
    }
  };

  const isKeyConfigured = (providerId: string) =>
    apiKeys.some(k => k.providerId === providerId && k.isConfigured);

  return (
    <div className="h-full overflow-y-auto p-4 space-y-6 max-w-2xl mx-auto pb-20">
      <h2 className="text-lg font-semibold">Settings</h2>

      {error && (
        <div className="bg-danger/10 border border-danger/20 text-danger text-sm px-3 py-2 rounded-lg">
          {error}
        </div>
      )}

      {/* Provider & Model Selection */}
      <section className="space-y-3">
        <h3 className="text-sm font-medium text-text-secondary">Active Provider & Model</h3>
        <div className="bg-bg-secondary border border-border rounded-xl p-4 space-y-3">
          <div>
            <label className="block text-xs text-text-muted mb-1.5">LLM Provider</label>
            <div className="relative">
              <select
                value={selectedProviderId}
                onChange={(e) => setProvider(e.target.value)}
                className="w-full appearance-none bg-bg-primary border border-border rounded-lg px-3 py-2.5 text-sm text-text-primary focus:outline-none focus:ring-2 focus:ring-accent/50 cursor-pointer"
              >
                {providers.map((p) => (
                  <option key={p.id} value={p.id}>{p.name}</option>
                ))}
              </select>
              <ChevronDown size={16} className="absolute right-3 top-1/2 -translate-y-1/2 text-text-muted pointer-events-none" />
            </div>
          </div>
          <div>
            <label className="block text-xs text-text-muted mb-1.5">Model</label>
            <div className="relative">
              <select
                value={selectedModelId}
                onChange={(e) => setModel(e.target.value)}
                className="w-full appearance-none bg-bg-primary border border-border rounded-lg px-3 py-2.5 text-sm text-text-primary focus:outline-none focus:ring-2 focus:ring-accent/50 cursor-pointer"
              >
                {models.length === 0 && (
                  <option value="">No models available — add API key first</option>
                )}
                {models.map((m) => (
                  <option key={m.id} value={m.id}>{m.name}</option>
                ))}
              </select>
              <ChevronDown size={16} className="absolute right-3 top-1/2 -translate-y-1/2 text-text-muted pointer-events-none" />
            </div>
          </div>
        </div>
      </section>

      {/* LLM API Keys */}
      <section className="space-y-3">
        <h3 className="text-sm font-medium text-text-secondary">LLM Provider Keys</h3>
        <div className="space-y-2">
          {providers.filter(p => p.requiresApiKey).map((provider) => (
            <ApiKeyCard
              key={provider.id}
              id={provider.id}
              name={provider.name}
              isConfigured={isKeyConfigured(provider.id)}
              isEditing={editingKey === provider.id}
              keyInput={keyInput}
              testing={testing}
              testResult={testResults[provider.id]}
              onEdit={() => { setEditingKey(editingKey === provider.id ? null : provider.id); setKeyInput(''); }}
              onKeyChange={setKeyInput}
              onSave={() => saveKey(provider.id)}
              onTest={() => testProvider(provider.id)}
              showTest={true}
            />
          ))}
        </div>
      </section>

      {/* Service Keys (Exa, etc.) */}
      <section className="space-y-3">
        <h3 className="text-sm font-medium text-text-secondary">Service Keys</h3>
        <div className="space-y-2">
          {serviceKeys.map((svc) => (
            <ApiKeyCard
              key={svc.id}
              id={svc.id}
              name={svc.name}
              description={svc.description}
              isConfigured={isKeyConfigured(svc.id)}
              isEditing={editingKey === svc.id}
              keyInput={keyInput}
              testing={null}
              testResult={undefined}
              onEdit={() => { setEditingKey(editingKey === svc.id ? null : svc.id); setKeyInput(''); }}
              onKeyChange={setKeyInput}
              onSave={() => saveKey(svc.id)}
              onTest={() => {}}
              showTest={false}
            />
          ))}
        </div>
      </section>

      {/* Account */}
      <section className="pt-4 border-t border-border">
        <h3 className="text-sm font-medium text-text-secondary mb-3">Account</h3>
        <Button variant="danger" size="sm" onClick={logout}>Sign Out</Button>
      </section>
    </div>
  );
}

function ApiKeyCard({
  id, name, description, isConfigured, isEditing, keyInput,
  testing, testResult, onEdit, onKeyChange, onSave, onTest, showTest,
}: {
  id: string;
  name: string;
  description?: string;
  isConfigured: boolean;
  isEditing: boolean;
  keyInput: string;
  testing: string | null;
  testResult: boolean | undefined;
  onEdit: () => void;
  onKeyChange: (v: string) => void;
  onSave: () => void;
  onTest: () => void;
  showTest: boolean;
}) {
  return (
    <div className="bg-bg-secondary border border-border rounded-xl p-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2 min-w-0">
          <Key size={16} className="text-text-muted shrink-0" />
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <span className="text-sm font-medium">{name}</span>
              {isConfigured ? (
                <span className="text-[10px] px-1.5 py-0.5 rounded-full bg-success/10 text-success font-medium">Active</span>
              ) : (
                <span className="text-[10px] px-1.5 py-0.5 rounded-full bg-warning/10 text-warning font-medium">Not set</span>
              )}
            </div>
            {description && <p className="text-xs text-text-muted mt-0.5">{description}</p>}
          </div>
        </div>
        <div className="flex gap-1.5 shrink-0 ml-2">
          {showTest && isConfigured && (
            <Button variant="ghost" size="sm" onClick={onTest} disabled={testing === id}>
              {testing === id ? (
                <Loader2 size={14} className="animate-spin" />
              ) : testResult === true ? (
                <Check size={14} className="text-success" />
              ) : testResult === false ? (
                <X size={14} className="text-danger" />
              ) : 'Test'}
            </Button>
          )}
          <Button variant="secondary" size="sm" onClick={onEdit}>
            {isEditing ? 'Cancel' : isConfigured ? 'Update' : 'Add Key'}
          </Button>
        </div>
      </div>

      {isEditing && (
        <div className="flex gap-2 mt-3">
          <Input
            type="password"
            value={keyInput}
            onChange={(e) => onKeyChange(e.target.value)}
            placeholder={`Enter ${name} API key...`}
            onKeyDown={(e) => { if (e.key === 'Enter') onSave(); }}
            autoFocus
          />
          <Button size="sm" onClick={onSave} disabled={!keyInput.trim()}>Save</Button>
        </div>
      )}
    </div>
  );
}
