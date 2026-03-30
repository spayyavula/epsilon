import { useEffect, useState, useCallback, useRef } from 'react';
import { FileText, Trash2, Loader2, CloudUpload } from 'lucide-react';
import { api } from '../api/client';
import type { DocumentDto } from '../types/api';

export function DocumentsPage() {
  const [documents, setDocuments] = useState<DocumentDto[]>([]);
  const [uploading, setUploading] = useState(false);
  const [dragOver, setDragOver] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);

  const load = useCallback(async () => {
    const docs = await api.get<DocumentDto[]>('/documents');
    setDocuments(docs);
  }, []);

  useEffect(() => { load(); }, [load]);

  const uploadFile = async (file: File) => {
    setUploading(true);
    try {
      await api.upload<DocumentDto>('/documents/upload', file);
      await load();
    } finally {
      setUploading(false);
    }
  };

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    await uploadFile(file);
    e.target.value = '';
  };

  const handleDrop = async (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);
    const file = e.dataTransfer.files?.[0];
    if (file) await uploadFile(file);
  };

  const handleDelete = async (id: string) => {
    await api.delete(`/documents/${id}`);
    setDocuments((d) => d.filter((doc) => doc.id !== id));
  };

  const formatSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1048576).toFixed(1)} MB`;
  };

  return (
    <div className="h-full overflow-y-auto p-4 space-y-5">
      <div className="animate-fade-in">
        <h2 className="text-lg font-semibold mb-1">Document Library</h2>
        <p className="text-sm text-text-muted">Upload documents to give Epsilon context for RAG-powered answers</p>
      </div>

      {/* Upload zone */}
      <div
        onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
        onDragLeave={() => setDragOver(false)}
        onDrop={handleDrop}
        onClick={() => fileRef.current?.click()}
        className={`border-2 border-dashed rounded-2xl p-8 text-center cursor-pointer animate-fade-in ${
          dragOver
            ? 'border-accent bg-accent/5'
            : 'border-border/50 hover:border-border-hover hover:bg-white/[0.02]'
        }`}
      >
        <input ref={fileRef} type="file" className="hidden" accept=".pdf,.txt,.md,.docx" onChange={handleUpload} />
        <div className={`w-12 h-12 rounded-xl flex items-center justify-center mx-auto mb-3 ${
          dragOver ? 'bg-accent/10' : 'bg-bg-secondary/50'
        }`}>
          {uploading ? (
            <Loader2 size={22} className="animate-spin text-accent" />
          ) : (
            <CloudUpload size={22} className={dragOver ? 'text-accent' : 'text-text-muted'} />
          )}
        </div>
        <p className="text-sm font-medium text-text-primary">
          {uploading ? 'Uploading...' : dragOver ? 'Drop file here' : 'Drop files here or click to upload'}
        </p>
        <p className="text-xs text-text-muted mt-1">PDF, DOCX, TXT, Markdown — up to 50MB</p>
      </div>

      {/* Document list */}
      {documents.length === 0 ? (
        <div className="text-center py-8 text-text-muted animate-fade-in">
          <FileText size={36} className="mx-auto mb-2 opacity-30" />
          <p className="text-sm">No documents yet</p>
        </div>
      ) : (
        <div className="space-y-2">
          {documents.map((doc, i) => (
            <div
              key={doc.id}
              className="bg-bg-secondary/30 border border-border/50 rounded-xl p-3.5 flex items-center gap-3 group hover:border-border-hover animate-fade-in"
              style={{ animationDelay: `${i * 40}ms` }}
            >
              <div className="w-9 h-9 rounded-lg bg-bg-tertiary/50 flex items-center justify-center shrink-0">
                <FileText size={16} className="text-text-muted" />
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium truncate">{doc.fileName}</p>
                <p className="text-xs text-text-muted flex items-center gap-1.5 mt-0.5">
                  {formatSize(doc.sizeBytes)}
                  {doc.pageCount != null && <><span className="text-border">·</span> {doc.pageCount} pages</>}
                  {doc.chunkCount > 0 && <><span className="text-border">·</span> {doc.chunkCount} chunks</>}
                  <span className="text-border">·</span>
                  <span className={`inline-flex items-center gap-1 ${doc.status === 'ready' ? 'text-success' : doc.status === 'error' ? 'text-danger' : 'text-warning'}`}>
                    {doc.status === 'processing' && <div className="w-1.5 h-1.5 rounded-full bg-warning animate-pulse" />}
                    {doc.status === 'ready' && <div className="w-1.5 h-1.5 rounded-full bg-success" />}
                    {doc.status}
                  </span>
                </p>
              </div>
              <button
                onClick={(e) => { e.stopPropagation(); handleDelete(doc.id); }}
                className="opacity-0 group-hover:opacity-100 text-text-muted hover:text-danger p-1.5 rounded-lg hover:bg-danger/5"
              >
                <Trash2 size={14} />
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
