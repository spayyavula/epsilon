using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Epsilon.App.ViewModels;

namespace Epsilon.App.Views;

public partial class ChatView : UserControl
{
    private ChatViewModel? _vm;
    private bool _webViewReady;

    public ChatView()
    {
        InitializeComponent();
        Loaded += ChatView_Loaded;
        Unloaded += ChatView_Unloaded;
    }

    private async void ChatView_Loaded(object sender, RoutedEventArgs e)
    {
        _vm = DataContext as ChatViewModel;
        if (_vm == null) return;

        _vm.MessageAdded += OnMessageAdded;
        _vm.ChatCleared += OnChatCleared;
        _vm.StreamingUpdated += OnStreamingUpdated;
        _vm.StreamingFinished += OnStreamingFinished;
        _vm.WebSourcesReady += OnWebSourcesReady;

        if (!_webViewReady)
        {
            var env = await CoreWebView2Environment.CreateAsync();
            await ChatWebView.EnsureCoreWebView2Async(env);
            ChatWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            _webViewReady = true;
            ChatWebView.NavigateToString(GetChatHtml());
        }
    }

    private void ChatView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null)
        {
            _vm.MessageAdded -= OnMessageAdded;
            _vm.ChatCleared -= OnChatCleared;
            _vm.StreamingUpdated -= OnStreamingUpdated;
            _vm.StreamingFinished -= OnStreamingFinished;
            _vm.WebSourcesReady -= OnWebSourcesReady;
        }
    }

    private void OnMessageAdded(string role, string content)
    {
        if (!_webViewReady) return;
        Dispatcher.InvokeAsync(async () =>
        {
            var roleJson = JsonSerializer.Serialize(role);
            var contentJson = JsonSerializer.Serialize(content);
            await ChatWebView.ExecuteScriptAsync($"addMessage({roleJson}, {contentJson})");
        });
    }

    private void OnChatCleared()
    {
        if (!_webViewReady) return;
        Dispatcher.InvokeAsync(async () =>
        {
            await ChatWebView.ExecuteScriptAsync("clearChat()");
        });
    }

    private void OnStreamingUpdated(string content)
    {
        if (!_webViewReady) return;
        Dispatcher.InvokeAsync(async () =>
        {
            var contentJson = JsonSerializer.Serialize(content);
            await ChatWebView.ExecuteScriptAsync($"updateStreaming({contentJson})");
        });
    }

    private void OnStreamingFinished()
    {
        if (!_webViewReady) return;
        Dispatcher.InvokeAsync(async () =>
        {
            await ChatWebView.ExecuteScriptAsync("finishStreaming()");
        });
    }

    private void OnWebSourcesReady(string sourcesJson)
    {
        if (!_webViewReady) return;
        Dispatcher.InvokeAsync(async () =>
        {
            await ChatWebView.ExecuteScriptAsync($"showWebSources({sourcesJson})");
        });
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var message = e.TryGetWebMessageAsString();
        if (message == null) return;

        // Handle save-to-library commands: "save:INDEX"
        if (message.StartsWith("save:") && int.TryParse(message[5..], out var index))
        {
            Dispatcher.InvokeAsync(() =>
            {
                _vm?.SaveWebResultCommand.Execute(index);
            });
        }
    }

    private void WebToggle_Click(object sender, MouseButtonEventArgs e)
    {
        if (_vm != null)
            _vm.WebSearchEnabled = !_vm.WebSearchEnabled;
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            e.Handled = true;
            if (_vm?.SendMessageCommand.CanExecute(null) == true)
                _vm.SendMessageCommand.Execute(null);
        }
    }

    private static string GetChatHtml()
    {
        return """
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="utf-8">
        <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/katex@0.16.11/dist/katex.min.css">
        <script src="https://cdn.jsdelivr.net/npm/katex@0.16.11/dist/katex.min.js"></script>
        <script src="https://cdn.jsdelivr.net/npm/katex@0.16.11/dist/contrib/auto-render.min.js"></script>
        <script src="https://cdn.jsdelivr.net/npm/marked/marked.min.js"></script>
        <style>
          * { margin: 0; padding: 0; box-sizing: border-box; }
          body {
            background: #0f0f23; color: #e0e0e0;
            font-family: 'Segoe UI', sans-serif; font-size: 15px;
            line-height: 1.6; padding: 24px; overflow-y: auto;
          }
          #chat { max-width: 900px; margin: 0 auto; }
          .message { margin-bottom: 20px; display: flex; }
          .message.user { justify-content: flex-end; }
          .message.assistant { justify-content: flex-start; }
          .bubble {
            max-width: 80%; padding: 14px 18px;
            border-radius: 16px; word-wrap: break-word;
          }
          .user .bubble {
            background: #20c997; color: white;
            border-bottom-right-radius: 4px;
          }
          .assistant .bubble {
            background: #1e1e3a; border: 1px solid #2a2a4a;
            border-bottom-left-radius: 4px;
          }
          .welcome { text-align: center; padding: 60px 20px; color: #666; }
          .welcome h2 { color: #aaa; font-size: 28px; margin-bottom: 8px; }
          .welcome .icon { font-size: 48px; margin-bottom: 16px; }
          .welcome p { max-width: 500px; margin: 0 auto 24px; }
          .prompts { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; max-width: 600px; margin: 0 auto; }
          .prompt-btn {
            background: #1e1e3a; border: 1px solid #2a2a4a; border-radius: 10px;
            padding: 12px; color: #bbb; text-align: left; cursor: pointer; font-size: 13px;
          }
          .prompt-btn:hover { border-color: #20c997; color: #ddd; }
          .streaming-indicator { display: inline-block; }
          .streaming-indicator span {
            display: inline-block; width: 6px; height: 6px;
            background: #63e6be; border-radius: 50%; margin: 0 2px;
            animation: bounce 1s infinite;
          }
          .streaming-indicator span:nth-child(2) { animation-delay: 0.15s; }
          .streaming-indicator span:nth-child(3) { animation-delay: 0.3s; }
          @keyframes bounce {
            0%, 80%, 100% { transform: translateY(0); }
            40% { transform: translateY(-8px); }
          }
          .bubble h1, .bubble h2, .bubble h3 { margin: 12px 0 6px; font-weight: 600; }
          .bubble h1 { font-size: 1.3em; } .bubble h2 { font-size: 1.15em; }
          .bubble p { margin-bottom: 8px; }
          .bubble ul, .bubble ol { margin-left: 24px; margin-bottom: 8px; }
          .bubble code {
            background: #2a2a4a; padding: 2px 6px; border-radius: 4px;
            font-family: 'Cascadia Code', 'Consolas', monospace; font-size: 0.9em;
          }
          .bubble pre { background: #2a2a4a; padding: 14px; border-radius: 8px; margin: 10px 0; overflow-x: auto; }
          .bubble pre code { background: transparent; padding: 0; }
          .bubble blockquote { border-left: 3px solid #20c997; padding-left: 12px; margin: 10px 0; color: #999; font-style: italic; }
          .bubble table { width: 100%; border-collapse: collapse; margin: 10px 0; }
          .bubble th, .bubble td { border: 1px solid #2a2a4a; padding: 8px; text-align: left; }
          .bubble th { background: #2a2a4a; }
          .katex-display { margin: 12px 0; overflow-x: auto; }
          .katex { color: inherit; }

          /* Web sources */
          .web-sources {
            max-width: 80%; margin: -10px 0 20px 0;
            display: flex; flex-direction: column; gap: 6px;
          }
          .web-source-card {
            background: #1a2a1a; border: 1px solid #2a4a2a; border-radius: 10px;
            padding: 10px 14px; display: flex; justify-content: space-between;
            align-items: center;
          }
          .web-source-info { flex: 1; min-width: 0; }
          .web-source-title {
            color: #69db7c; font-size: 13px; font-weight: 600;
            white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
          }
          .web-source-url {
            color: #666; font-size: 11px;
            white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
          }
          .web-source-meta { color: #555; font-size: 11px; }
          .save-btn {
            background: #2a4a2a; color: #69db7c; border: 1px solid #3a5a3a;
            border-radius: 6px; padding: 4px 10px; cursor: pointer;
            font-size: 11px; font-weight: 600; margin-left: 10px; white-space: nowrap;
          }
          .save-btn:hover { background: #3a5a3a; }
          .save-btn.saved {
            background: #1a2a1a; color: #555; border-color: #2a3a2a;
            cursor: default;
          }
        </style>
        </head>
        <body>
        <div id="chat">
          <div id="welcome" class="welcome">
            <div class="icon">&#949;</div>
            <h2>Epsilon</h2>
            <p>Your AI-powered mathematics research and learning assistant.
               Ask questions, solve proofs, and explore mathematical concepts.</p>
            <div class="prompts">
              <div class="prompt-btn" onclick="usePrompt(this)">Prove that &radic;2 is irrational by contradiction</div>
              <div class="prompt-btn" onclick="usePrompt(this)">Explain the Fundamental Theorem of Calculus</div>
              <div class="prompt-btn" onclick="usePrompt(this)">What is a group in abstract algebra?</div>
              <div class="prompt-btn" onclick="usePrompt(this)">Prove by induction that 1+2+&hellip;+n = n(n+1)/2</div>
            </div>
          </div>
          <div id="streaming-bubble" style="display:none" class="message assistant">
            <div class="bubble" id="streaming-content">
              <div class="streaming-indicator"><span></span><span></span><span></span></div>
            </div>
          </div>
        </div>
        <script>
          const chat = document.getElementById('chat');
          const welcome = document.getElementById('welcome');
          const streamingBubble = document.getElementById('streaming-bubble');
          const streamingContent = document.getElementById('streaming-content');

          function renderContent(text) {
            try {
              let html = marked.parse(text);
              const div = document.createElement('div');
              div.innerHTML = html;
              renderMathInElement(div, {
                delimiters: [
                  {left: '$$', right: '$$', display: true},
                  {left: '$', right: '$', display: false},
                  {left: '\\[', right: '\\]', display: true},
                  {left: '\\(', right: '\\)', display: false},
                ],
                throwOnError: false,
              });
              return div.innerHTML;
            } catch(e) {
              return text;
            }
          }

          function addMessage(role, content) {
            welcome.style.display = 'none';
            streamingBubble.style.display = 'none';
            const msg = document.createElement('div');
            msg.className = 'message ' + role;
            const bubble = document.createElement('div');
            bubble.className = 'bubble';
            if (role === 'user') {
              bubble.textContent = content;
            } else {
              bubble.innerHTML = renderContent(content);
            }
            msg.appendChild(bubble);
            chat.insertBefore(msg, streamingBubble);
            window.scrollTo(0, document.body.scrollHeight);
          }

          function updateStreaming(content) {
            welcome.style.display = 'none';
            streamingBubble.style.display = 'flex';
            if (content) {
              streamingContent.innerHTML = renderContent(content);
            }
            window.scrollTo(0, document.body.scrollHeight);
          }

          function finishStreaming() {
            streamingBubble.style.display = 'none';
            streamingContent.innerHTML = '<div class="streaming-indicator"><span></span><span></span><span></span></div>';
          }

          function showWebSources(sources) {
            if (!sources || sources.length === 0) return;
            const container = document.createElement('div');
            container.className = 'web-sources';

            const label = document.createElement('div');
            label.style.cssText = 'font-size:11px;color:#555;font-weight:600;margin-bottom:2px;';
            label.textContent = 'WEB SOURCES';
            container.appendChild(label);

            sources.forEach((s, i) => {
              const card = document.createElement('div');
              card.className = 'web-source-card';

              let meta = '';
              if (s.author) meta += s.author;
              if (s.date) meta += (meta ? ' · ' : '') + s.date;

              card.innerHTML = `
                <div class="web-source-info">
                  <div class="web-source-title">${escapeHtml(s.title)}</div>
                  <div class="web-source-url">${escapeHtml(s.url)}</div>
                  ${meta ? '<div class="web-source-meta">' + escapeHtml(meta) + '</div>' : ''}
                </div>
                <button class="save-btn" onclick="saveSource(this, ${i})">Save to Library</button>
              `;
              container.appendChild(card);
            });

            chat.insertBefore(container, streamingBubble);
            window.scrollTo(0, document.body.scrollHeight);
          }

          function saveSource(btn, index) {
            if (btn.classList.contains('saved')) return;
            btn.classList.add('saved');
            btn.textContent = 'Saved';
            window.chrome.webview.postMessage('save:' + index);
          }

          function clearChat() {
            const messages = chat.querySelectorAll('.message:not(#streaming-bubble), .web-sources');
            messages.forEach(m => m.remove());
            welcome.style.display = 'block';
            finishStreaming();
          }

          function usePrompt(el) {
            window.chrome.webview.postMessage(el.textContent);
          }

          function escapeHtml(text) {
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
          }
        </script>
        </body>
        </html>
        """;
    }
}
