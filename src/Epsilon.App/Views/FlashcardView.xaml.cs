using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Epsilon.App.ViewModels;

namespace Epsilon.App.Views;

public partial class FlashcardView : UserControl
{
    private FlashcardViewModel? _vm;
    private bool _webViewReady;

    public FlashcardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm = DataContext as FlashcardViewModel;
        if (_vm == null) return;

        _vm.CardReady += OnCardReady;
        _vm.AnswerRevealed += OnAnswerRevealed;
        _vm.EmptyStateRequested += OnEmptyState;

        if (!_webViewReady)
        {
            var env = await CoreWebView2Environment.CreateAsync();
            await CardWebView.EnsureCoreWebView2Async(env);
            _webViewReady = true;
            CardWebView.NavigateToString(GetCardHtml());
        }

        // Show initial state
        if (_vm.CurrentCard != null)
            OnCardReady(_vm.CurrentCard.Front, _vm.CurrentCard.Back);
        else
            OnEmptyState("No cards due for review.");
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null)
        {
            _vm.CardReady -= OnCardReady;
            _vm.AnswerRevealed -= OnAnswerRevealed;
            _vm.EmptyStateRequested -= OnEmptyState;
        }
    }

    private void OnCardReady(string front, string back)
    {
        if (!_webViewReady) return;
        Dispatcher.InvokeAsync(async () =>
        {
            var frontJson = JsonSerializer.Serialize(front);
            var backJson = JsonSerializer.Serialize(back);
            await CardWebView.ExecuteScriptAsync($"showFront({frontJson}, {backJson})");
        });
    }

    private void OnAnswerRevealed()
    {
        if (!_webViewReady) return;
        Dispatcher.InvokeAsync(async () =>
        {
            await CardWebView.ExecuteScriptAsync("showBack()");
        });
    }

    private void OnEmptyState(string message)
    {
        if (!_webViewReady) return;
        Dispatcher.InvokeAsync(async () =>
        {
            var json = JsonSerializer.Serialize(message);
            await CardWebView.ExecuteScriptAsync($"showEmpty({json})");
        });
    }

    private static string GetCardHtml()
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
            display: flex; align-items: center; justify-content: center;
            min-height: 100vh; padding: 24px;
          }
          #card-container { width: 100%; max-width: 800px; perspective: 1200px; }

          .card {
            width: 100%; min-height: 280px;
            position: relative; transform-style: preserve-3d;
            transition: transform 0.5s ease;
            cursor: default;
          }
          .card.flipped { transform: rotateY(180deg); }

          .card-face {
            position: absolute; width: 100%; min-height: 280px;
            backface-visibility: hidden; -webkit-backface-visibility: hidden;
            border-radius: 16px; padding: 36px 40px;
            display: flex; flex-direction: column; justify-content: center;
          }
          .card-front {
            background: #1a1a2e; border: 1px solid #2a2a5a;
          }
          .card-back {
            background: #1a2a1a; border: 1px solid #2a5a2a;
            transform: rotateY(180deg);
          }

          .card-label {
            font-size: 11px; font-weight: 700; letter-spacing: 1px;
            color: #555; margin-bottom: 16px; text-transform: uppercase;
          }
          .card-front .card-label { color: #4c6ef5; }
          .card-back  .card-label { color: #20c997; }

          .card-content { font-size: 16px; line-height: 1.7; color: #e0e0e0; }

          .katex-display { margin: 14px 0; overflow-x: auto; }
          .katex { color: inherit; }

          h1, h2, h3 { color: #c0c8ff; margin: 12px 0 6px; }
          p { margin-bottom: 8px; }
          ul, ol { margin-left: 20px; margin-bottom: 8px; }
          strong { color: #e8ecff; }
          code {
            background: #2a2a4a; padding: 2px 6px; border-radius: 4px;
            font-family: 'Cascadia Code', monospace; font-size: 0.88em;
          }
          pre { background: #2a2a4a; padding: 12px; border-radius: 8px; margin: 10px 0; }
          pre code { background: transparent; padding: 0; }

          /* Empty state */
          #empty-state {
            text-align: center; padding: 60px 20px;
            display: none;
          }
          #empty-state .empty-icon { font-size: 48px; margin-bottom: 16px; }
          #empty-state .empty-msg { color: #666; font-size: 16px; }
        </style>
        </head>
        <body>
        <div id="card-container">
          <div class="card" id="card">
            <div class="card-face card-front" id="front-face">
              <div class="card-label">QUESTION / THEOREM</div>
              <div class="card-content" id="front-content"></div>
            </div>
            <div class="card-face card-back" id="back-face">
              <div class="card-label">ANSWER / PROOF</div>
              <div class="card-content" id="back-content"></div>
            </div>
          </div>
          <div id="empty-state">
            <div class="empty-icon">&#10003;</div>
            <div class="empty-msg" id="empty-msg">All caught up!</div>
          </div>
        </div>
        <script>
          let _backText = '';

          function renderMath(el) {
            renderMathInElement(el, {
              delimiters: [
                {left: '$$', right: '$$', display: true},
                {left: '$', right: '$', display: false},
                {left: '\\[', right: '\\]', display: true},
                {left: '\\(', right: '\\)', display: false},
              ],
              throwOnError: false,
            });
          }

          function renderMarkdown(text) {
            try {
              const div = document.createElement('div');
              div.innerHTML = marked.parse(text);
              renderMath(div);
              return div.innerHTML;
            } catch(e) { return text; }
          }

          function showFront(front, back) {
            _backText = back;
            document.getElementById('card').classList.remove('flipped');
            document.getElementById('card-container').style.display = '';
            document.getElementById('empty-state').style.display = 'none';
            document.getElementById('card').style.display = '';

            const frontEl = document.getElementById('front-content');
            frontEl.innerHTML = renderMarkdown(front);
          }

          function showBack() {
            document.getElementById('card').classList.add('flipped');
            const backEl = document.getElementById('back-content');
            backEl.innerHTML = renderMarkdown(_backText);
          }

          function showEmpty(message) {
            document.getElementById('card').style.display = 'none';
            const es = document.getElementById('empty-state');
            es.style.display = 'block';
            document.getElementById('empty-msg').textContent = message;
          }
        </script>
        </body>
        </html>
        """;
    }
}
