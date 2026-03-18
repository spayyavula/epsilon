using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Epsilon.App.ViewModels;

namespace Epsilon.App.Views;

public partial class SolverView : UserControl
{
    private SolverViewModel? _vm;
    private bool _webViewReady;

    public SolverView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm = DataContext as SolverViewModel;
        if (_vm == null) return;

        _vm.SolutionStreaming += OnStreaming;
        _vm.SolutionFinished += OnFinished;
        _vm.ViewCleared += OnCleared;

        if (!_webViewReady)
        {
            var env = await CoreWebView2Environment.CreateAsync();
            await SolverWebView.EnsureCoreWebView2Async(env);
            _webViewReady = true;
            SolverWebView.NavigateToString(GetSolverHtml());
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null)
        {
            _vm.SolutionStreaming -= OnStreaming;
            _vm.SolutionFinished -= OnFinished;
            _vm.ViewCleared -= OnCleared;
        }
    }

    private void OnStreaming(string content)
    {
        if (!_webViewReady) return;
        Dispatcher.InvokeAsync(async () =>
        {
            var json = JsonSerializer.Serialize(content);
            await SolverWebView.ExecuteScriptAsync($"updateSolution({json})");
        });
    }

    private void OnFinished()
    {
        if (!_webViewReady) return;
        Dispatcher.InvokeAsync(async () =>
        {
            await SolverWebView.ExecuteScriptAsync("finalizeSolution()");
        });
    }

    private void OnCleared()
    {
        if (!_webViewReady) return;
        Dispatcher.InvokeAsync(async () =>
        {
            await SolverWebView.ExecuteScriptAsync("clearSolution()");
        });
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            e.Handled = true;
            if (_vm?.SolveCommand.CanExecute(null) == true)
                _vm.SolveCommand.Execute(null);
        }
    }

    private void Example_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is string example && _vm != null)
            _vm.EquationInput = example;
    }

    private static string GetSolverHtml()
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
          * { margin:0; padding:0; box-sizing:border-box; }
          body {
            background: #0f0f23; color: #e0e0e0;
            font-family: 'Segoe UI', sans-serif; font-size: 15px;
            line-height: 1.7; padding: 28px 32px; overflow-y: auto;
          }
          #solution { max-width: 900px; margin: 0 auto; }

          /* Step styling */
          h3 {
            color: #20c997; font-size: 1.1em; font-weight: 700;
            margin: 24px 0 8px; padding: 8px 14px;
            background: #1a2e2e; border-left: 4px solid #20c997;
            border-radius: 0 8px 8px 0;
          }
          h3:first-child { margin-top: 0; }

          h1 { color: #63e6be; font-size: 1.3em; margin: 16px 0 8px; border-bottom: 1px solid #2a2a4a; padding-bottom: 6px; }
          h2 { color: #63e6be; font-size: 1.15em; margin: 14px 0 8px; }

          p { margin-bottom: 10px; }
          ul, ol { margin-left: 24px; margin-bottom: 10px; }
          li { margin-bottom: 4px; }
          strong { color: #e8ecff; }

          code {
            background: #2a2a4a; padding: 2px 6px; border-radius: 4px;
            font-family: 'Cascadia Code', 'Consolas', monospace; font-size: 0.9em;
          }
          pre {
            background: #2a2a4a; padding: 14px; border-radius: 8px;
            margin: 12px 0; overflow-x: auto;
          }
          pre code { background: transparent; padding: 0; }

          blockquote {
            border-left: 3px solid #20c997; padding-left: 12px;
            margin: 12px 0; color: #999; font-style: italic;
          }

          table { width: 100%; border-collapse: collapse; margin: 12px 0; }
          th, td { border: 1px solid #2a2a4a; padding: 8px 12px; text-align: left; }
          th { background: #2a2a4a; }

          /* LaTeX */
          .katex-display { margin: 16px 0; overflow-x: auto; }
          .katex { color: inherit; }

          /* Answer box highlight */
          .katex-display .boxed,
          .katex-display .fbox {
            border: 2px solid #20c997 !important;
            padding: 8px 16px !important;
            border-radius: 8px;
            background: #1a2e2e;
          }

          /* Streaming indicator */
          .solving {
            display: inline-block; margin-top: 16px;
          }
          .solving span {
            display: inline-block; width: 8px; height: 8px;
            background: #20c997; border-radius: 50%; margin: 0 3px;
            animation: pulse 1s infinite;
          }
          .solving span:nth-child(2) { animation-delay: 0.15s; }
          .solving span:nth-child(3) { animation-delay: 0.3s; }
          @keyframes pulse {
            0%, 80%, 100% { transform: scale(0.6); opacity: 0.4; }
            40% { transform: scale(1); opacity: 1; }
          }

          hr { border: none; border-top: 1px solid #2a2a4a; margin: 20px 0; }
        </style>
        </head>
        <body>
        <div id="solution"></div>
        <script>
          const solution = document.getElementById('solution');

          function renderMarkdown(text) {
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
            } catch(e) { return text; }
          }

          function updateSolution(content) {
            solution.innerHTML = renderMarkdown(content) +
              '<div class="solving"><span></span><span></span><span></span></div>';
            window.scrollTo(0, document.body.scrollHeight);
          }

          function finalizeSolution() {
            // Remove solving indicator
            const indicator = solution.querySelector('.solving');
            if (indicator) indicator.remove();
            // Re-render for clean output
            const raw = solution.textContent;
            // Just leave as-is since content is already rendered
          }

          function clearSolution() {
            solution.innerHTML = '';
          }
        </script>
        </body>
        </html>
        """;
    }
}
