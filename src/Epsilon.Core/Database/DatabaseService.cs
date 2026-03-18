using Microsoft.Data.Sqlite;
using Epsilon.Core.Documents;
using Epsilon.Core.Models;

namespace Epsilon.Core.Database;

public class DatabaseService : IDisposable
{
    private readonly SqliteConnection _conn;

    public DatabaseService(string dbPath)
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        RunMigrations();
    }

    private void RunMigrations()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            PRAGMA journal_mode=WAL;
            PRAGMA foreign_keys=ON;

            CREATE TABLE IF NOT EXISTS conversations (
                id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                provider_id TEXT,
                model_id TEXT,
                system_prompt_id TEXT,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS messages (
                id TEXT PRIMARY KEY,
                conversation_id TEXT NOT NULL,
                role TEXT NOT NULL,
                content TEXT NOT NULL,
                model TEXT,
                created_at TEXT NOT NULL,
                FOREIGN KEY (conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_messages_conv
                ON messages(conversation_id, created_at);

            CREATE TABLE IF NOT EXISTS documents (
                id TEXT PRIMARY KEY,
                filename TEXT NOT NULL,
                filepath TEXT NOT NULL,
                mime_type TEXT NOT NULL,
                size_bytes INTEGER NOT NULL,
                page_count INTEGER,
                chunk_count INTEGER DEFAULT 0,
                status TEXT DEFAULT 'pending',
                ingested_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS document_chunks (
                id TEXT PRIMARY KEY,
                document_id TEXT NOT NULL,
                chunk_index INTEGER NOT NULL,
                text TEXT NOT NULL,
                char_start INTEGER NOT NULL,
                char_end INTEGER NOT NULL,
                FOREIGN KEY (document_id) REFERENCES documents(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_chunks_doc
                ON document_chunks(document_id);

            CREATE VIRTUAL TABLE IF NOT EXISTS document_chunks_fts USING fts5(
                text,
                document_id UNINDEXED,
                content='document_chunks',
                content_rowid='rowid'
            );

            CREATE TRIGGER IF NOT EXISTS chunks_ai AFTER INSERT ON document_chunks BEGIN
                INSERT INTO document_chunks_fts(rowid, text, document_id)
                VALUES (new.rowid, new.text, new.document_id);
            END;

            CREATE TRIGGER IF NOT EXISTS chunks_ad AFTER DELETE ON document_chunks BEGIN
                INSERT INTO document_chunks_fts(document_chunks_fts, rowid, text, document_id)
                VALUES ('delete', old.rowid, old.text, old.document_id);
            END;

            CREATE TABLE IF NOT EXISTS library_folders (
                id TEXT PRIMARY KEY,
                path TEXT NOT NULL UNIQUE,
                label TEXT NOT NULL,
                added_at TEXT NOT NULL,
                last_scanned_at TEXT
            );

            -- Add folder_id column to documents if not present
            CREATE TABLE IF NOT EXISTS _migration_check (id INTEGER);
            DROP TABLE IF EXISTS _migration_check;

            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL,
                is_secret INTEGER DEFAULT 0,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS research_projects (
                id TEXT PRIMARY KEY,
                tool_type TEXT NOT NULL,
                title TEXT NOT NULL,
                current_step INTEGER NOT NULL DEFAULT 0,
                status TEXT NOT NULL DEFAULT 'active',
                provider_id TEXT,
                model_id TEXT,
                web_search_enabled INTEGER DEFAULT 0,
                metadata TEXT,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS research_steps (
                id TEXT PRIMARY KEY,
                project_id TEXT NOT NULL,
                step_index INTEGER NOT NULL,
                user_input TEXT NOT NULL DEFAULT '',
                generated_content TEXT NOT NULL DEFAULT '',
                status TEXT NOT NULL DEFAULT 'empty',
                generated_at TEXT,
                FOREIGN KEY (project_id) REFERENCES research_projects(id) ON DELETE CASCADE,
                UNIQUE(project_id, step_index)
            );

            CREATE INDEX IF NOT EXISTS idx_research_steps_project
                ON research_steps(project_id, step_index);

            CREATE TABLE IF NOT EXISTS system_prompts (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                domain TEXT,
                content TEXT NOT NULL,
                is_default INTEGER DEFAULT 0,
                created_at TEXT NOT NULL
            );
        ";
        cmd.ExecuteNonQuery();

        // Migration: add folder_id column to documents
        try
        {
            using var alter = _conn.CreateCommand();
            alter.CommandText = "ALTER TABLE documents ADD COLUMN folder_id TEXT REFERENCES library_folders(id)";
            alter.ExecuteNonQuery();
        }
        catch { /* Column already exists */ }

        // Seed default math prompt
        using var check = _conn.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM system_prompts WHERE is_default = 1";
        var count = (long)(check.ExecuteScalar() ?? 0);

        if (count == 0)
        {
            using var insert = _conn.CreateCommand();
            insert.CommandText = @"
                INSERT INTO system_prompts (id, name, domain, content, is_default, created_at)
                VALUES ($id, $name, $domain, $content, 1, $created)";
            insert.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            insert.Parameters.AddWithValue("$name", "Mathematics Assistant");
            insert.Parameters.AddWithValue("$domain", "general");
            insert.Parameters.AddWithValue("$content", DefaultMathPrompt);
            insert.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("o"));
            insert.ExecuteNonQuery();
        }
    }

    // --- Conversations ---

    public void CreateConversation(Conversation conv)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO conversations (id, title, provider_id, model_id, system_prompt_id, created_at, updated_at)
            VALUES ($id, $title, $provider, $model, $prompt, $created, $updated)";
        cmd.Parameters.AddWithValue("$id", conv.Id);
        cmd.Parameters.AddWithValue("$title", conv.Title);
        cmd.Parameters.AddWithValue("$provider", (object?)conv.ProviderId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$model", (object?)conv.ModelId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$prompt", (object?)conv.SystemPromptId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", conv.CreatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$updated", conv.UpdatedAt.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public List<Conversation> ListConversations()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, title, provider_id, model_id, system_prompt_id, created_at, updated_at
            FROM conversations ORDER BY updated_at DESC";

        var list = new List<Conversation>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Conversation
            {
                Id = reader.GetString(0),
                Title = reader.GetString(1),
                ProviderId = reader.IsDBNull(2) ? null : reader.GetString(2),
                ModelId = reader.IsDBNull(3) ? null : reader.GetString(3),
                SystemPromptId = reader.IsDBNull(4) ? null : reader.GetString(4),
                CreatedAt = DateTime.Parse(reader.GetString(5)),
                UpdatedAt = DateTime.Parse(reader.GetString(6)),
            });
        }
        return list;
    }

    public void DeleteConversation(string id)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "DELETE FROM conversations WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void UpdateConversationTitle(string id, string title)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "UPDATE conversations SET title = $title, updated_at = $now WHERE id = $id";
        cmd.Parameters.AddWithValue("$title", title);
        cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    // --- Messages ---

    public void InsertMessage(ChatMessage msg)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO messages (id, conversation_id, role, content, model, created_at)
            VALUES ($id, $conv, $role, $content, $model, $created)";
        cmd.Parameters.AddWithValue("$id", msg.Id);
        cmd.Parameters.AddWithValue("$conv", msg.ConversationId);
        cmd.Parameters.AddWithValue("$role", msg.Role);
        cmd.Parameters.AddWithValue("$content", msg.Content);
        cmd.Parameters.AddWithValue("$model", (object?)msg.Model ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", msg.CreatedAt.ToString("o"));
        cmd.ExecuteNonQuery();

        // Update conversation timestamp
        using var update = _conn.CreateCommand();
        update.CommandText = "UPDATE conversations SET updated_at = $now WHERE id = $id";
        update.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));
        update.Parameters.AddWithValue("$id", msg.ConversationId);
        update.ExecuteNonQuery();
    }

    public List<ChatMessage> GetMessages(string conversationId)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, conversation_id, role, content, model, created_at
            FROM messages WHERE conversation_id = $conv ORDER BY created_at ASC";
        cmd.Parameters.AddWithValue("$conv", conversationId);

        var list = new List<ChatMessage>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ChatMessage
            {
                Id = reader.GetString(0),
                ConversationId = reader.GetString(1),
                Role = reader.GetString(2),
                Content = reader.GetString(3),
                Model = reader.IsDBNull(4) ? null : reader.GetString(4),
                CreatedAt = DateTime.Parse(reader.GetString(5)),
            });
        }
        return list;
    }

    // --- Documents ---

    public void InsertDocument(DocumentInfo doc)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO documents (id, filename, filepath, mime_type, size_bytes, page_count, chunk_count, status, ingested_at)
            VALUES ($id, $name, $path, $mime, $size, $pages, $chunks, $status, $ingested)";
        cmd.Parameters.AddWithValue("$id", doc.Id);
        cmd.Parameters.AddWithValue("$name", doc.FileName);
        cmd.Parameters.AddWithValue("$path", doc.FilePath);
        cmd.Parameters.AddWithValue("$mime", doc.MimeType);
        cmd.Parameters.AddWithValue("$size", doc.SizeBytes);
        cmd.Parameters.AddWithValue("$pages", (object?)doc.PageCount ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$chunks", doc.ChunkCount);
        cmd.Parameters.AddWithValue("$status", doc.Status);
        cmd.Parameters.AddWithValue("$ingested", doc.IngestedAt.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public List<DocumentInfo> ListDocuments()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, filename, filepath, mime_type, size_bytes, page_count, chunk_count, status, ingested_at, folder_id
            FROM documents ORDER BY ingested_at DESC";

        var list = new List<DocumentInfo>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new DocumentInfo
            {
                Id = reader.GetString(0),
                FileName = reader.GetString(1),
                FilePath = reader.GetString(2),
                MimeType = reader.GetString(3),
                SizeBytes = reader.GetInt64(4),
                PageCount = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                ChunkCount = reader.GetInt32(6),
                Status = reader.GetString(7),
                IngestedAt = DateTime.Parse(reader.GetString(8)),
                FolderId = reader.IsDBNull(9) ? null : reader.GetString(9),
            });
        }
        return list;
    }

    public void DeleteDocument(string id)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "DELETE FROM documents WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    // --- Library Folders ---

    public void InsertFolder(LibraryFolder folder)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO library_folders (id, path, label, added_at, last_scanned_at)
            VALUES ($id, $path, $label, $added, $scanned)";
        cmd.Parameters.AddWithValue("$id", folder.Id);
        cmd.Parameters.AddWithValue("$path", folder.Path);
        cmd.Parameters.AddWithValue("$label", folder.Label);
        cmd.Parameters.AddWithValue("$added", folder.AddedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$scanned", (object?)folder.LastScannedAt?.ToString("o") ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public List<LibraryFolder> ListFolders()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            SELECT f.id, f.path, f.label, f.added_at, f.last_scanned_at,
                   (SELECT COUNT(*) FROM documents d WHERE d.folder_id = f.id) as doc_count
            FROM library_folders f ORDER BY f.label ASC";

        var list = new List<LibraryFolder>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new LibraryFolder
            {
                Id = reader.GetString(0),
                Path = reader.GetString(1),
                Label = reader.GetString(2),
                AddedAt = DateTime.Parse(reader.GetString(3)),
                LastScannedAt = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                DocumentCount = reader.GetInt32(5),
            });
        }
        return list;
    }

    public void UpdateFolderScannedAt(string id, DateTime scannedAt)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "UPDATE library_folders SET last_scanned_at = $scanned WHERE id = $id";
        cmd.Parameters.AddWithValue("$scanned", scannedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteFolder(string id)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "DELETE FROM library_folders WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public List<string> GetDocumentPathsForFolder(string folderId)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT filepath FROM documents WHERE folder_id = $folderId";
        cmd.Parameters.AddWithValue("$folderId", folderId);

        var list = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(reader.GetString(0));
        return list;
    }

    public List<string> GetDocumentIdsForFolder(string folderId)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT id FROM documents WHERE folder_id = $folderId";
        cmd.Parameters.AddWithValue("$folderId", folderId);

        var list = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(reader.GetString(0));
        return list;
    }

    public void InsertDocumentWithFolder(DocumentInfo doc)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO documents (id, filename, filepath, mime_type, size_bytes, page_count, chunk_count, status, folder_id, ingested_at)
            VALUES ($id, $name, $path, $mime, $size, $pages, $chunks, $status, $folder, $ingested)";
        cmd.Parameters.AddWithValue("$id", doc.Id);
        cmd.Parameters.AddWithValue("$name", doc.FileName);
        cmd.Parameters.AddWithValue("$path", doc.FilePath);
        cmd.Parameters.AddWithValue("$mime", doc.MimeType);
        cmd.Parameters.AddWithValue("$size", doc.SizeBytes);
        cmd.Parameters.AddWithValue("$pages", (object?)doc.PageCount ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$chunks", doc.ChunkCount);
        cmd.Parameters.AddWithValue("$status", doc.Status);
        cmd.Parameters.AddWithValue("$folder", (object?)doc.FolderId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ingested", doc.IngestedAt.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    // --- Document Chunks ---

    public void InsertChunks(List<DocumentChunk> chunks)
    {
        using var transaction = _conn.BeginTransaction();
        try
        {
            foreach (var chunk in chunks)
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO document_chunks (id, document_id, chunk_index, text, char_start, char_end)
                    VALUES ($id, $docId, $idx, $text, $start, $end)";
                cmd.Parameters.AddWithValue("$id", chunk.Id);
                cmd.Parameters.AddWithValue("$docId", chunk.DocumentId);
                cmd.Parameters.AddWithValue("$idx", chunk.ChunkIndex);
                cmd.Parameters.AddWithValue("$text", chunk.Text);
                cmd.Parameters.AddWithValue("$start", chunk.CharStart);
                cmd.Parameters.AddWithValue("$end", chunk.CharEnd);
                cmd.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void DeleteChunks(string documentId)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "DELETE FROM document_chunks WHERE document_id = $docId";
        cmd.Parameters.AddWithValue("$docId", documentId);
        cmd.ExecuteNonQuery();
    }

    public void UpdateDocumentStatus(string id, string status, int chunkCount, int? pageCount = null)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE documents SET status = $status, chunk_count = $chunks, page_count = $pages
            WHERE id = $id";
        cmd.Parameters.AddWithValue("$status", status);
        cmd.Parameters.AddWithValue("$chunks", chunkCount);
        cmd.Parameters.AddWithValue("$pages", (object?)pageCount ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public List<RetrievedChunk> SearchChunks(string ftsQuery, int topK)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            SELECT dc.text, dc.document_id, d.filename
            FROM document_chunks_fts fts
            JOIN document_chunks dc ON dc.rowid = fts.rowid
            JOIN documents d ON d.id = dc.document_id
            WHERE document_chunks_fts MATCH $query
            ORDER BY rank
            LIMIT $topK";
        cmd.Parameters.AddWithValue("$query", ftsQuery);
        cmd.Parameters.AddWithValue("$topK", topK);

        var results = new List<RetrievedChunk>();
        try
        {
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new RetrievedChunk
                {
                    Text = reader.GetString(0),
                    DocumentId = reader.GetString(1),
                    FileName = reader.GetString(2),
                });
            }
        }
        catch { /* FTS query may fail on unusual input */ }

        return results;
    }

    // --- Research Projects ---

    public void CreateResearchProject(ResearchProject project)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO research_projects (id, tool_type, title, current_step, status, provider_id, model_id, web_search_enabled, metadata, created_at, updated_at)
            VALUES ($id, $tool, $title, $step, $status, $provider, $model, $web, $meta, $created, $updated)";
        cmd.Parameters.AddWithValue("$id", project.Id);
        cmd.Parameters.AddWithValue("$tool", project.ToolType);
        cmd.Parameters.AddWithValue("$title", project.Title);
        cmd.Parameters.AddWithValue("$step", project.CurrentStep);
        cmd.Parameters.AddWithValue("$status", project.Status);
        cmd.Parameters.AddWithValue("$provider", (object?)project.ProviderId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$model", (object?)project.ModelId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$web", project.WebSearchEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$meta", (object?)project.Metadata ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", project.CreatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$updated", project.UpdatedAt.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public List<ResearchProject> ListResearchProjects()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, tool_type, title, current_step, status, provider_id, model_id, web_search_enabled, metadata, created_at, updated_at
            FROM research_projects ORDER BY updated_at DESC";

        var list = new List<ResearchProject>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(ReadResearchProject(reader));
        }
        return list;
    }

    public ResearchProject? GetResearchProject(string id)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, tool_type, title, current_step, status, provider_id, model_id, web_search_enabled, metadata, created_at, updated_at
            FROM research_projects WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadResearchProject(reader) : null;
    }

    public void UpdateResearchProject(ResearchProject project)
    {
        project.UpdatedAt = DateTime.UtcNow;
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE research_projects SET title=$title, current_step=$step, status=$status,
                provider_id=$provider, model_id=$model, web_search_enabled=$web, metadata=$meta, updated_at=$updated
            WHERE id = $id";
        cmd.Parameters.AddWithValue("$title", project.Title);
        cmd.Parameters.AddWithValue("$step", project.CurrentStep);
        cmd.Parameters.AddWithValue("$status", project.Status);
        cmd.Parameters.AddWithValue("$provider", (object?)project.ProviderId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$model", (object?)project.ModelId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$web", project.WebSearchEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$meta", (object?)project.Metadata ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$updated", project.UpdatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$id", project.Id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteResearchProject(string id)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "DELETE FROM research_projects WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    // --- Research Steps ---

    public void UpsertResearchStep(ResearchStep step)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO research_steps (id, project_id, step_index, user_input, generated_content, status, generated_at)
            VALUES ($id, $proj, $idx, $input, $content, $status, $gen)
            ON CONFLICT(project_id, step_index) DO UPDATE SET
                user_input = $input, generated_content = $content, status = $status, generated_at = $gen";
        cmd.Parameters.AddWithValue("$id", step.Id);
        cmd.Parameters.AddWithValue("$proj", step.ProjectId);
        cmd.Parameters.AddWithValue("$idx", step.StepIndex);
        cmd.Parameters.AddWithValue("$input", step.UserInput);
        cmd.Parameters.AddWithValue("$content", step.GeneratedContent);
        cmd.Parameters.AddWithValue("$status", step.Status);
        cmd.Parameters.AddWithValue("$gen", (object?)step.GeneratedAt?.ToString("o") ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public List<ResearchStep> GetResearchSteps(string projectId)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, project_id, step_index, user_input, generated_content, status, generated_at
            FROM research_steps WHERE project_id = $proj ORDER BY step_index ASC";
        cmd.Parameters.AddWithValue("$proj", projectId);

        var list = new List<ResearchStep>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ResearchStep
            {
                Id = reader.GetString(0),
                ProjectId = reader.GetString(1),
                StepIndex = reader.GetInt32(2),
                UserInput = reader.GetString(3),
                GeneratedContent = reader.GetString(4),
                Status = reader.GetString(5),
                GeneratedAt = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6)),
            });
        }
        return list;
    }

    private static ResearchProject ReadResearchProject(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        return new ResearchProject
        {
            Id = reader.GetString(0),
            ToolType = reader.GetString(1),
            Title = reader.GetString(2),
            CurrentStep = reader.GetInt32(3),
            Status = reader.GetString(4),
            ProviderId = reader.IsDBNull(5) ? null : reader.GetString(5),
            ModelId = reader.IsDBNull(6) ? null : reader.GetString(6),
            WebSearchEnabled = reader.GetInt32(7) != 0,
            Metadata = reader.IsDBNull(8) ? null : reader.GetString(8),
            CreatedAt = DateTime.Parse(reader.GetString(9)),
            UpdatedAt = DateTime.Parse(reader.GetString(10)),
        };
    }

    // --- Settings ---

    public string? GetSetting(string key)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM settings WHERE key = $key";
        cmd.Parameters.AddWithValue("$key", key);
        return cmd.ExecuteScalar() as string;
    }

    public void SetSetting(string key, string value, bool isSecret = false)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO settings (key, value, is_secret, updated_at)
            VALUES ($key, $value, $secret, $now)
            ON CONFLICT(key) DO UPDATE SET value = $value, is_secret = $secret, updated_at = $now";
        cmd.Parameters.AddWithValue("$key", key);
        cmd.Parameters.AddWithValue("$value", value);
        cmd.Parameters.AddWithValue("$secret", isSecret ? 1 : 0);
        cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    // --- System Prompts ---

    public SystemPrompt? GetDefaultSystemPrompt()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, name, domain, content, is_default, created_at
            FROM system_prompts WHERE is_default = 1 LIMIT 1";

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new SystemPrompt
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            Domain = reader.IsDBNull(2) ? null : reader.GetString(2),
            Content = reader.GetString(3),
            IsDefault = reader.GetBoolean(4),
            CreatedAt = DateTime.Parse(reader.GetString(5)),
        };
    }

    public List<SystemPrompt> ListSystemPrompts()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, name, domain, content, is_default, created_at
            FROM system_prompts ORDER BY is_default DESC, name ASC";

        var list = new List<SystemPrompt>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new SystemPrompt
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Domain = reader.IsDBNull(2) ? null : reader.GetString(2),
                Content = reader.GetString(3),
                IsDefault = reader.GetBoolean(4),
                CreatedAt = DateTime.Parse(reader.GetString(5)),
            });
        }
        return list;
    }

    public void Dispose()
    {
        _conn.Dispose();
    }

    private const string DefaultMathPrompt = """
        You are Epsilon, an expert mathematics research and learning assistant.

        When answering:
        - Use precise mathematical terminology and notation.
        - Write equations in LaTeX: inline with $...$ and display with $$...$$.
        - Cite relevant theorems, lemmas, and mathematical results when applicable.
        - Distinguish between different branches: algebra, analysis, topology, number theory, etc.
        - When a question is ambiguous, state your assumptions explicitly.
        - For proofs, clearly state the proof strategy (direct, contradiction, induction, etc.).
        - Build from definitions and axioms before presenting results.
        - When explaining concepts, provide both formal definitions and intuitive understanding.

        You have access to the user's uploaded mathematics documents. When context from
        these documents is provided, ground your answers in that material and cite
        the source document and section.

        Common mathematical notation for reference:
        - Natural numbers: ℕ = {0, 1, 2, ...}
        - Integers: ℤ, Rationals: ℚ, Reals: ℝ, Complex: ℂ
        - For all: ∀, There exists: ∃
        - Element of: ∈, Subset: ⊆, Proper subset: ⊂
        - Union: ∪, Intersection: ∩
        - Implies: ⟹, If and only if: ⟺
        - QED / End of proof: □
        """;
}
