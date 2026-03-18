using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Epsilon.App.ViewModels;
using Epsilon.Core.Models;
using Epsilon.Core.Research;

namespace Epsilon.App.Views;

public partial class ResearchToolkitView : UserControl
{
    private ResearchToolkitViewModel? _vm;
    private bool _webViewReady;

    public ResearchToolkitView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm = DataContext as ResearchToolkitViewModel;
        if (_vm == null) return;

        _vm.StepContentStreaming += OnStreaming;
        _vm.StepContentFinished += OnFinished;
        _vm.StepContentReady += OnContentReady;
        _vm.ViewCleared += OnViewCleared;

        if (!_webViewReady)
        {
            var env = await CoreWebView2Environment.CreateAsync();
            await StepOutputWebView.EnsureCoreWebView2Async(env);
            _webViewReady = true;
            StepOutputWebView.NavigateToString(GetHtml());
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null)
        {
            _vm.StepContentStreaming -= OnStreaming;
            _vm.StepContentFinished -= OnFinished;
            _vm.StepContentReady -= OnContentReady;
            _vm.ViewCleared -= OnViewCleared;
        }
    }

    private void OnStreaming(string content)
    {
        if (!_webViewReady) return;
        Dispatcher.InvokeAsync(async () =>
        {
            var json = JsonSerializer.Serialize(content);
            await StepOutputWebView.ExecuteScriptAsync($"updateContent({json})");
        });
    }

    private void OnFinished()
    {
        if (!_webViewReady) return;
        Dispatcher.InvokeAsync(async () =>
        {
            await StepOutputWebView.ExecuteScriptAsync("finalizeContent()");
        });
    }

    private void OnContentReady(string content)
    {
        if (!_webViewReady) return;
        Dispatcher.InvokeAsync(async () =>
        {
            var json = JsonSerializer.Serialize(content);
            await StepOutputWebView.ExecuteScriptAsync($"setContent({json})");
        });
    }

    private void OnViewCleared()
    {
        if (!_webViewReady) return;
        Dispatcher.InvokeAsync(async () =>
        {
            await StepOutputWebView.ExecuteScriptAsync("clearContent()");
        });
    }

    private void ToolCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is string toolType && _vm != null)
            _vm.NewProjectCommand.Execute(toolType);
    }

    private void ProjectCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is ResearchProject project && _vm != null)
            _vm.OpenProjectCommand.Execute(project);
    }

    private void StepItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is ResearchStepInfo stepInfo && _vm != null)
            _vm.NavigateToStepCommand.Execute(stepInfo.Index);
    }

    private static string GetHtml()
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
            line-height: 1.7; padding: 24px; overflow-y: auto;
          }
          #content { max-width: 900px; margin: 0 auto; }
          h1, h2, h3 { margin: 16px 0 8px; font-weight: 600; color: #c0c8ff; }
          h1 { font-size: 1.4em; border-bottom: 1px solid #2a2a4a; padding-bottom: 6px; }
          h2 { font-size: 1.2em; }
          h3 { font-size: 1.05em; }
          p { margin-bottom: 10px; }
          ul, ol { margin-left: 24px; margin-bottom: 10px; }
          li { margin-bottom: 4px; }
          strong { color: #e8ecff; }
          code {
            background: #2a2a4a; padding: 2px 6px; border-radius: 4px;
            font-family: 'Cascadia Code', 'Consolas', monospace; font-size: 0.9em;
          }
          pre { background: #2a2a4a; padding: 14px; border-radius: 8px; margin: 12px 0; overflow-x: auto; }
          pre code { background: transparent; padding: 0; }
          blockquote { border-left: 3px solid #20c997; padding-left: 12px; margin: 12px 0; color: #999; font-style: italic; }
          table { width: 100%; border-collapse: collapse; margin: 12px 0; }
          th, td { border: 1px solid #2a2a4a; padding: 8px 12px; text-align: left; }
          th { background: #2a2a4a; font-weight: 600; }
          .katex-display { margin: 14px 0; overflow-x: auto; }
          .katex { color: inherit; }
          hr { border: none; border-top: 1px solid #2a2a4a; margin: 20px 0; }
        </style>
        </head>
        <body>
        <div id="content"></div>
        <script>
          const content = document.getElementById('content');

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
            } catch(e) {
              return text;
            }
          }

          function setContent(markdown) {
            content.innerHTML = renderMarkdown(markdown);
            window.scrollTo(0, 0);
          }

          function updateContent(partial) {
            content.innerHTML = renderMarkdown(partial);
            window.scrollTo(0, document.body.scrollHeight);
          }

          function finalizeContent() {
            // Re-render to ensure proper formatting
            const text = content.textContent;
            if (text) content.innerHTML = renderMarkdown(text);
          }

          function clearContent() {
            content.innerHTML = '';
          }
        </script>
        </body>
        </html>
        """;
    }
}
