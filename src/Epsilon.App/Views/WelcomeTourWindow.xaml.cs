using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace Epsilon.App.Views;

public partial class WelcomeTourWindow : Window
{
    public bool DontShowAgain { get; private set; }

    public WelcomeTourWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        MouseLeftButtonDown += (_, _) => DragMove();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var env = await CoreWebView2Environment.CreateAsync();
        await TourWebView.EnsureCoreWebView2Async(env);
        TourWebView.CoreWebView2.WebMessageReceived += OnWebMessage;
        TourWebView.NavigateToString(GetTourHtml());
    }

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var msg = e.TryGetWebMessageAsString();
        if (msg == "close")
            Close();
        else if (msg == "close_hide")
        {
            DontShowAgain = true;
            Close();
        }
    }

    private static string GetTourHtml()
    {
        return """
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="utf-8">
        <style>
          * { margin:0; padding:0; box-sizing:border-box; }
          body {
            background: #0f0f23; color: #e0e0e0;
            font-family: 'Segoe UI', sans-serif;
            overflow: hidden; height: 100vh;
            user-select: none;
          }

          .tour-container {
            height: 100vh; display: flex; flex-direction: column;
          }

          /* Slides */
          .slides { flex:1; position:relative; overflow:hidden; }
          .slide {
            position: absolute; inset:0; padding: 48px 56px;
            display: flex; flex-direction: column; justify-content: center;
            opacity: 0; transform: translateX(60px);
            transition: all 0.5s cubic-bezier(0.4, 0, 0.2, 1);
            pointer-events: none;
          }
          .slide.active {
            opacity: 1; transform: translateX(0); pointer-events: auto;
          }
          .slide.exit-left {
            opacity: 0; transform: translateX(-60px);
          }

          .slide-icon { font-size: 56px; margin-bottom: 16px; line-height: 1; }
          .slide-title {
            font-size: 32px; font-weight: 700; margin-bottom: 12px;
            background: linear-gradient(135deg, #63e6be, #20c997);
            -webkit-background-clip: text; -webkit-text-fill-color: transparent;
          }
          .slide-subtitle { font-size: 16px; color: #aaa; margin-bottom: 28px; line-height: 1.5; max-width: 600px; }

          .feature-grid {
            display: grid; grid-template-columns: 1fr 1fr; gap: 14px;
            max-width: 650px;
          }
          .feature-card {
            background: #1a1a2e; border: 1px solid #2a2a4a; border-radius: 12px;
            padding: 16px; transition: all 0.3s;
          }
          .feature-card:hover { border-color: #20c997; transform: translateY(-2px); }
          .feature-card .icon { font-size: 24px; margin-bottom: 8px; }
          .feature-card .title { font-size: 14px; font-weight: 600; color: #eee; margin-bottom: 4px; }
          .feature-card .desc { font-size: 12px; color: #888; line-height: 1.4; }

          .step-list { max-width: 600px; }
          .step-item {
            display: flex; align-items: flex-start; margin-bottom: 16px;
            opacity: 0; transform: translateY(20px);
            animation: fadeInUp 0.4s forwards;
          }
          .step-item:nth-child(1) { animation-delay: 0.1s; }
          .step-item:nth-child(2) { animation-delay: 0.25s; }
          .step-item:nth-child(3) { animation-delay: 0.4s; }
          .step-item:nth-child(4) { animation-delay: 0.55s; }
          .step-item:nth-child(5) { animation-delay: 0.7s; }
          .step-num {
            width: 32px; height: 32px; border-radius: 50%;
            background: #20c997; color: white; font-weight: 700;
            display: flex; align-items: center; justify-content: center;
            margin-right: 14px; flex-shrink: 0; font-size: 14px;
          }
          .step-text .title { font-size: 14px; font-weight: 600; color: #eee; }
          .step-text .desc { font-size: 12px; color: #888; margin-top: 2px; }

          .tool-cards {
            display: flex; gap: 12px; flex-wrap: wrap; max-width: 700px;
          }
          .tool-card {
            background: #1a1a2e; border-radius: 10px; padding: 14px 18px;
            border: 1px solid #2a2a4a; min-width: 120px;
            opacity: 0; animation: fadeInUp 0.4s forwards;
          }
          .tool-card:nth-child(1) { animation-delay: 0.1s; }
          .tool-card:nth-child(2) { animation-delay: 0.2s; }
          .tool-card:nth-child(3) { animation-delay: 0.3s; }
          .tool-card:nth-child(4) { animation-delay: 0.4s; }
          .tool-card:nth-child(5) { animation-delay: 0.5s; }
          .tool-card .icon { font-size: 24px; margin-bottom: 6px; }
          .tool-card .name { font-size: 13px; font-weight: 600; color: #eee; }

          .tip-box {
            background: #1a2a3a; border: 1px solid #2a4a6a; border-radius: 10px;
            padding: 16px 20px; margin-top: 20px; max-width: 600px;
          }
          .tip-box .label { font-size: 11px; font-weight: 700; color: #4ca6ff; text-transform: uppercase; margin-bottom: 6px; }
          .tip-box .text { font-size: 13px; color: #bbb; line-height: 1.5; }

          @keyframes fadeInUp {
            to { opacity: 1; transform: translateY(0); }
          }
          @keyframes pulse {
            0%, 100% { transform: scale(1); }
            50% { transform: scale(1.05); }
          }

          /* Footer */
          .footer {
            padding: 16px 56px; display: flex; align-items: center;
            justify-content: space-between; border-top: 1px solid #1a1a2e;
          }
          .dots { display: flex; gap: 8px; }
          .dot {
            width: 8px; height: 8px; border-radius: 50%;
            background: #2a2a4a; transition: all 0.3s; cursor: pointer;
          }
          .dot.active { background: #20c997; width: 24px; border-radius: 4px; }

          .footer-right { display: flex; align-items: center; gap: 12px; }
          .btn {
            padding: 8px 20px; border-radius: 8px; border: none;
            font-size: 14px; font-weight: 600; cursor: pointer;
            transition: all 0.2s;
          }
          .btn-primary { background: #20c997; color: white; }
          .btn-primary:hover { background: #12b886; }
          .btn-ghost { background: transparent; color: #888; }
          .btn-ghost:hover { color: #ccc; }
          .btn-finish { background: #40c057; color: white; animation: pulse 2s infinite; }
          .btn-finish:hover { background: #51cf66; }

          .checkbox-row {
            display: flex; align-items: center; gap: 8px;
          }
          .checkbox-row input { accent-color: #20c997; }
          .checkbox-row label { font-size: 12px; color: #666; cursor: pointer; }
        </style>
        </head>
        <body>
        <div class="tour-container">
          <div class="slides" id="slides">

            <!-- Slide 0: Welcome -->
            <div class="slide active" data-index="0">
              <div class="slide-icon">&#949;</div>
              <div class="slide-title">Welcome to Epsilon</div>
              <div class="slide-subtitle">
                Your AI-powered mathematics research and learning assistant. Chat with multiple LLMs,
                build a document library, search the web, and use guided research tools — all designed
                for mathematics students and researchers.
              </div>
              <div class="feature-grid">
                <div class="feature-card">
                  <div class="icon">&#128488;</div>
                  <div class="title">AI Chat</div>
                  <div class="desc">Chat with GPT-4, Claude, Gemini, or local models with LaTeX rendering</div>
                </div>
                <div class="feature-card">
                  <div class="icon">&#128196;</div>
                  <div class="title">Document Library</div>
                  <div class="desc">Upload PDFs, link folders, connect OneDrive — auto-indexed for search</div>
                </div>
                <div class="feature-card">
                  <div class="icon">&#127760;</div>
                  <div class="title">Web Search</div>
                  <div class="desc">Exa-powered web search finds academic content and saves to your library</div>
                </div>
                <div class="feature-card">
                  <div class="icon">&#128300;</div>
                  <div class="title">Research Toolkit</div>
                  <div class="desc">Guided tools for proofs, problem solving, concept exploration, and more</div>
                </div>
              </div>
            </div>

            <!-- Slide 1: Getting Started -->
            <div class="slide" data-index="1">
              <div class="slide-icon">&#128640;</div>
              <div class="slide-title">Getting Started</div>
              <div class="slide-subtitle">Set up Epsilon in 3 easy steps:</div>
              <div class="step-list">
                <div class="step-item">
                  <div class="step-num">1</div>
                  <div class="step-text">
                    <div class="title">Add an API Key</div>
                    <div class="desc">Go to Settings (gear icon) and enter your OpenAI, Anthropic, or Gemini API key. You only need one to start.</div>
                  </div>
                </div>
                <div class="step-item">
                  <div class="step-num">2</div>
                  <div class="step-text">
                    <div class="title">Start Chatting</div>
                    <div class="desc">Select your provider and model in the chat bar, then ask any mathematics question. Equations render beautifully with LaTeX.</div>
                  </div>
                </div>
                <div class="step-item">
                  <div class="step-num">3</div>
                  <div class="step-text">
                    <div class="title">Build Your Library</div>
                    <div class="desc">Upload textbooks, lecture notes, or link your OneDrive. The AI will reference your documents when answering questions.</div>
                  </div>
                </div>
              </div>
              <div class="tip-box">
                <div class="label">&#128161; Pro Tip</div>
                <div class="text">The more documents you add, the smarter Epsilon becomes about YOUR specific courses and research. It automatically searches your library on every question.</div>
              </div>
            </div>

            <!-- Slide 2: Chat Features -->
            <div class="slide" data-index="2">
              <div class="slide-icon">&#128488;</div>
              <div class="slide-title">Smart Mathematics Chat</div>
              <div class="slide-subtitle">More than just a chatbot — it's a mathematics-aware assistant:</div>
              <div class="step-list">
                <div class="step-item">
                  <div class="step-num">&#8704;</div>
                  <div class="step-text">
                    <div class="title">LaTeX Equations</div>
                    <div class="desc">Expressions like $\sum_{n=1}^{\infty} \frac{1}{n^2} = \frac{\pi^2}{6}$ and integrals render beautifully inline and as display math.</div>
                  </div>
                </div>
                <div class="step-item">
                  <div class="step-num">&#128269;</div>
                  <div class="step-text">
                    <div class="title">RAG: Document-Grounded Answers</div>
                    <div class="desc">Every question automatically searches your uploaded documents. Answers cite your textbooks and notes.</div>
                  </div>
                </div>
                <div class="step-item">
                  <div class="step-num">&#127760;</div>
                  <div class="step-text">
                    <div class="title">Web Search Toggle</div>
                    <div class="desc">Click "Web" in the chat bar to search academic sources via Exa. Save results to your library as PDFs.</div>
                  </div>
                </div>
                <div class="step-item">
                  <div class="step-num">&#9654;</div>
                  <div class="step-text">
                    <div class="title">Multiple Providers</div>
                    <div class="desc">Switch between OpenAI, Claude, Gemini, or Ollama mid-conversation. Each model has different strengths.</div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Slide 3: Document Library -->
            <div class="slide" data-index="3">
              <div class="slide-icon">&#128218;</div>
              <div class="slide-title">Your Mathematics Library</div>
              <div class="slide-subtitle">Three ways to build your knowledge base:</div>
              <div class="step-list">
                <div class="step-item">
                  <div class="step-num">&#128196;</div>
                  <div class="step-text">
                    <div class="title">Add Individual Files</div>
                    <div class="desc">Upload PDFs, Word docs, text, or markdown files. They're copied to your library and indexed instantly.</div>
                  </div>
                </div>
                <div class="step-item">
                  <div class="step-num">&#128193;</div>
                  <div class="step-text">
                    <div class="title">Link a Folder</div>
                    <div class="desc">Point to any folder on your computer. Files are referenced in-place (no duplication). Hit refresh to pick up changes.</div>
                  </div>
                </div>
                <div class="step-item">
                  <div class="step-num">&#9729;</div>
                  <div class="step-text">
                    <div class="title">Connect OneDrive</div>
                    <div class="desc">Auto-detects your OneDrive folder. Pick which subfolders to include — perfect for synced course materials.</div>
                  </div>
                </div>
                <div class="step-item">
                  <div class="step-num">&#127760;</div>
                  <div class="step-text">
                    <div class="title">Save from Web Search</div>
                    <div class="desc">Found a great article via web search? Click "Save to Library" and it becomes a permanent PDF in your collection.</div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Slide 4: Research Toolkit -->
            <div class="slide" data-index="4">
              <div class="slide-icon">&#128300;</div>
              <div class="slide-title">Research Toolkit</div>
              <div class="slide-subtitle">Guided tools designed for mathematics students and researchers:</div>
              <div class="tool-cards">
                <div class="tool-card">
                  <div class="icon">&#128221;</div>
                  <div class="name">Proof Writer</div>
                  <div class="desc" style="font-size:11px;color:#888;margin-top:4px">Step-by-step formal proofs</div>
                </div>
                <div class="tool-card">
                  <div class="icon">&#129513;</div>
                  <div class="name">Problem Solver</div>
                  <div class="desc" style="font-size:11px;color:#888;margin-top:4px">Step-by-step or hint mode</div>
                </div>
                <div class="tool-card">
                  <div class="icon">&#128202;</div>
                  <div class="name">Concept Explorer</div>
                  <div class="desc" style="font-size:11px;color:#888;margin-top:4px">Deep dives into any math topic</div>
                </div>
                <div class="tool-card">
                  <div class="icon">&#128300;</div>
                  <div class="name">Research Paper</div>
                  <div class="desc" style="font-size:11px;color:#888;margin-top:4px">Section-by-section academic writing</div>
                </div>
                <div class="tool-card">
                  <div class="icon">&#127756;</div>
                  <div class="name">Study Guide</div>
                  <div class="desc" style="font-size:11px;color:#888;margin-top:4px">Summaries and exam preparation</div>
                </div>
              </div>
              <div class="tip-box">
                <div class="label">How it works</div>
                <div class="text">Each tool guides you step by step. Enter your notes at each step, click Generate, and the AI builds on all previous steps. Projects auto-save and can be exported as PDF.</div>
              </div>
            </div>

            <!-- Slide 5: Ready -->
            <div class="slide" data-index="5">
              <div class="slide-icon" style="font-size:72px">&#949;</div>
              <div class="slide-title" style="font-size:36px">You're Ready!</div>
              <div class="slide-subtitle" style="font-size:17px; max-width: 500px;">
                Start by heading to <strong>Settings</strong> to add your API key, then ask your first mathematics question.
              </div>
              <div class="feature-grid" style="margin-top: 8px;">
                <div class="feature-card" style="border-color:#40c057">
                  <div class="title" style="color:#69db7c">&#9881; First: Settings</div>
                  <div class="desc">Add at least one API key (OpenAI, Anthropic, or Gemini)</div>
                </div>
                <div class="feature-card" style="border-color:#20c997">
                  <div class="title" style="color:#63e6be">&#128488; Then: Chat</div>
                  <div class="desc">Ask "Prove that there are infinitely many prime numbers"</div>
                </div>
                <div class="feature-card" style="border-color:#f59f00">
                  <div class="title" style="color:#ffd43b">&#128196; Explore: Documents</div>
                  <div class="desc">Upload your textbooks and lecture notes</div>
                </div>
                <div class="feature-card" style="border-color:#9c36b5">
                  <div class="title" style="color:#da77f2">&#128300; Try: Research</div>
                  <div class="desc">Write a proof or explore a mathematical concept</div>
                </div>
              </div>
              <div class="tip-box" style="margin-top: 24px; background: #1a1a2e; border-color: #2a2a4a;">
                <div class="label" style="color: #888;">Keyboard shortcut</div>
                <div class="text">Press <strong>Enter</strong> to send messages, <strong>Shift+Enter</strong> for new lines. Click the <strong>?</strong> in the sidebar to see this tour again anytime.</div>
              </div>
            </div>

          </div>

          <!-- Footer -->
          <div class="footer">
            <div class="dots" id="dots"></div>
            <div class="footer-right">
              <div class="checkbox-row" id="hideCheckbox" style="display:none">
                <input type="checkbox" id="dontShow">
                <label for="dontShow">Don't show on startup</label>
              </div>
              <button class="btn btn-ghost" id="skipBtn" onclick="skip()">Skip</button>
              <button class="btn btn-primary" id="nextBtn" onclick="next()">Next &rarr;</button>
            </div>
          </div>
        </div>

        <script>
          const slides = document.querySelectorAll('.slide');
          const totalSlides = slides.length;
          let current = 0;

          // Build dots
          const dotsEl = document.getElementById('dots');
          for (let i = 0; i < totalSlides; i++) {
            const dot = document.createElement('div');
            dot.className = 'dot' + (i === 0 ? ' active' : '');
            dot.onclick = () => goTo(i);
            dotsEl.appendChild(dot);
          }

          function goTo(index) {
            if (index === current) return;
            const dir = index > current ? 1 : -1;

            slides[current].classList.remove('active');
            slides[current].classList.add(dir > 0 ? 'exit-left' : '');
            setTimeout(() => {
              slides[current].classList.remove('exit-left');
              slides[current].style.transform = dir > 0 ? 'translateX(60px)' : 'translateX(-60px)';
            }, 500);

            current = index;
            slides[current].style.transform = dir > 0 ? 'translateX(60px)' : 'translateX(-60px)';
            slides[current].classList.add('active');

            // Update dots
            document.querySelectorAll('.dot').forEach((d, i) => {
              d.className = 'dot' + (i === current ? ' active' : '');
            });

            // Update buttons
            updateButtons();
          }

          function next() {
            if (current < totalSlides - 1) goTo(current + 1);
            else finish();
          }

          function skip() { finish(); }

          function finish() {
            const hide = document.getElementById('dontShow').checked;
            window.chrome.webview.postMessage(hide ? 'close_hide' : 'close');
          }

          function updateButtons() {
            const nextBtn = document.getElementById('nextBtn');
            const hideCheckbox = document.getElementById('hideCheckbox');

            if (current === totalSlides - 1) {
              nextBtn.textContent = "Get Started!";
              nextBtn.className = 'btn btn-finish';
              hideCheckbox.style.display = 'flex';
            } else {
              nextBtn.innerHTML = 'Next &rarr;';
              nextBtn.className = 'btn btn-primary';
              hideCheckbox.style.display = 'none';
            }
          }

          // Keyboard navigation
          document.addEventListener('keydown', (e) => {
            if (e.key === 'ArrowRight' || e.key === 'Enter') next();
            else if (e.key === 'ArrowLeft' && current > 0) goTo(current - 1);
            else if (e.key === 'Escape') finish();
          });
        </script>
        </body>
        </html>
        """;
    }
}
