namespace Epsilon.Core.Research;

public record ToolDefinition(
    string ToolType,
    string DisplayName,
    string Icon,
    string Description,
    string AccentColor,
    IReadOnlyList<StepDefinition> Steps);

public record StepDefinition(
    int Index,
    string Label,
    string InputLabel,
    string? InputPlaceholder,
    string SystemPrompt,
    string UserPromptTemplate,
    bool IsAutoGenerate = false);

public static class ToolRegistry
{
    private static readonly List<ToolDefinition> _tools = new()
    {
        ProofBuilder(),
        ProblemSolver(),
        ConceptExplorer(),
        LeanBridge(),
        PracticeMode(),
    };

    public static IReadOnlyList<ToolDefinition> GetAll() => _tools;

    public static ToolDefinition? Get(string toolType) =>
        _tools.FirstOrDefault(t => t.ToolType == toolType);

    private static ToolDefinition ProofBuilder() => new(
        "ProofBuilder", "Proof Builder", "\U0001F4D0",
        "Construct mathematical proofs step by step: state the theorem, choose a strategy, build the argument, and produce a formal write-up.",
        "#20c997",
        new StepDefinition[]
        {
            new(0, "Statement", "Theorem / Proposition",
                "State the theorem, proposition, or claim you want to prove. Include all relevant definitions and conditions...",
                ProofSystemPrompt,
                "The student wants to prove the following:\n\n{input}\n\nAnalyze this statement:\n1. **Formal Statement** — rewrite it using precise mathematical notation with quantifiers ($\\forall$, $\\exists$)\n2. **Definitions** — list all key definitions needed to understand the statement\n3. **Type of Statement** — is this universal, existential, conditional, biconditional, or a negation?\n4. **Known Results** — list theorems, lemmas, or axioms that might be relevant\n5. **Initial Observations** — any immediate consequences or special cases to consider\n\nUse LaTeX for all mathematical notation."),

            new(1, "Strategy", "Proof Strategy",
                "Describe your initial thoughts on how to approach the proof. What technique do you think might work?",
                ProofSystemPrompt,
                "Theorem to prove:\n{previous_steps}\n\nStudent's thoughts on strategy:\n{input}\n\nAnalyze and recommend a proof strategy:\n1. **Possible Approaches** — evaluate each:\n   - **Direct proof**: assume hypotheses, derive conclusion\n   - **Proof by contradiction**: assume negation, derive contradiction\n   - **Proof by contrapositive**: prove contrapositive instead\n   - **Mathematical induction**: if the statement involves natural numbers\n   - **Proof by cases**: if the domain can be partitioned\n   - **Construction**: if proving existence\n2. **Recommended Strategy** — which approach is most elegant/efficient and why\n3. **Key Steps Outline** — sketch the main logical steps before full construction\n4. **Potential Pitfalls** — common errors or subtle points to watch for\n\nUse LaTeX for all equations."),

            new(2, "Construction", "Build the Proof",
                "Write your proof attempt, or describe the key arguments you want to make...",
                ProofSystemPrompt,
                "Proof context:\n{previous_steps}\n\nStudent's proof attempt/notes:\n{input}\n\nConstruct a rigorous proof:\n1. **Proof Setup** — clearly state what we assume and what we need to show\n2. **Step-by-Step Argument** — each step must follow logically from the previous\n   - Label each step\n   - Justify each step (by which axiom, definition, theorem, or previous step)\n   - Use proper logical connectives\n3. **Key Computations** — show all algebraic/analytic work in detail\n4. **Conclusion** — explicitly state that the desired result follows\n5. End with $\\square$ (QED)\n\nIf the student's attempt has errors, point them out constructively and show the correction.\n\nUse LaTeX throughout. Number important equations."),

            new(3, "Verification", "Review & Check",
                "Any concerns about the proof? Edge cases to verify? Alternative approaches to consider?",
                ProofSystemPrompt,
                "Complete proof so far:\n{previous_steps}\n\nStudent's concerns:\n{input}\n\nVerify the proof rigorously:\n1. **Logical Validity** — check every inference step. Is each conclusion warranted?\n2. **Completeness** — are there any gaps? Unstated assumptions?\n3. **Edge Cases** — does the proof handle boundary cases correctly?\n4. **Converse** — is the converse true? If so, is the proof actually proving if-and-only-if?\n5. **Generalization** — can the result be strengthened?\n6. **Alternative Proofs** — briefly sketch a different approach for comparison\n\nBe critical and thorough. A proof is either valid or it isn't."),

            new(4, "Formal Write-up", "Final Version",
                null,
                ProofSystemPrompt,
                "Compile a clean, formal mathematical proof from all the work done:\n\n{previous_steps}\n\n**Write a publication-quality proof:**\n\n**Theorem.** (restate precisely)\n\n*Proof.* (clean, self-contained argument)\n- Every step justified\n- No unnecessary words\n- Proper mathematical notation throughout\n- End with $\\square$\n\nAfter the proof, add:\n- **Remark.** Any interesting observations or connections\n- **Corollary.** Any immediate consequences (if applicable)\n\nThis should be clean enough to submit in a mathematics course or paper.",
                IsAutoGenerate: true),
        });

    private static ToolDefinition ProblemSolver() => new(
        "ProblemSolver", "Problem Solver", "\U0001F9E9",
        "Get step-by-step solutions to math problems with full working, or use hint mode for guided learning.",
        "#e03131",
        new StepDefinition[]
        {
            new(0, "Problem", "Mathematics Problem",
                "Enter the math problem you want to solve. Include all given information...",
                ProblemSystemPrompt,
                "Mathematics problem to solve:\n{input}\n\nBefore solving, set up the problem:\n1. **Restate** — express the problem precisely in mathematical language\n2. **Identify** — what type of problem is this? (algebraic, analytic, combinatorial, geometric, etc.)\n3. **Known quantities** — list all given information\n4. **Unknown(s)** — what exactly do we need to find?\n5. **Relevant Tools** — which theorems, formulas, or techniques apply?\n6. **Strategy** — outline the approach before calculating\n\nDo NOT solve yet — just set up the framework. Use LaTeX for all quantities."),

            new(1, "Solution", "Solution Mode",
                "Type 'solve' for a complete solution, 'hint' for a progressive hint, or ask a specific question...",
                ProblemSystemPrompt,
                "Problem setup:\n{previous_steps}\n\nStudent's request: {input}\n\nIf the student asked for a FULL SOLUTION:\n1. Start from the problem statement\n2. Show EVERY step — do not skip algebraic manipulations\n3. Justify each non-obvious step\n4. Box the final answer: $$\\boxed{{answer}}$$\n5. **Verify**: substitute back or check with alternative method\n6. **Discuss**: what does this result mean? Are there special cases?\n\nIf the student asked for a HINT:\nGive ONE hint only — the next logical step they should think about. Ask a guiding question. Do NOT give the answer.\n\nIf the student asked a SPECIFIC QUESTION:\nAnswer that question only, relating it back to the problem.\n\nUse LaTeX for ALL equations and calculations."),
        });

    private static ToolDefinition ConceptExplorer() => new(
        "ConceptExplorer", "Concept Explorer", "\U0001F30C",
        "Deep-dive into any mathematical concept: definitions, theorems, examples, intuition, and connections.",
        "#9c36b5",
        new StepDefinition[]
        {
            new(0, "Topic", "Mathematical Concept",
                "Enter a math concept to explore (e.g., 'Group homomorphisms', 'Lebesgue integration', 'Eigenvalues')...",
                ConceptSystemPrompt,
                "The student wants to deeply explore this mathematical concept:\n{input}\n\nProvide a comprehensive concept map:\n1. **Formal Definition** — precise, using standard notation\n2. **Prerequisites** — what concepts must be understood first\n3. **Historical Context** — who developed it, when, and what problem it solved\n4. **Key Theorems** — the most important results, stated precisely\n5. **Examples** — concrete, illuminating examples (at least 3)\n6. **Non-Examples** — things that look similar but aren't (common confusions)\n7. **Intuition** — explain it to someone who understands logic but not the specific math\n8. **Common Misconceptions** — what students typically get wrong\n\nUse LaTeX for all mathematical notation."),

            new(1, "Deep Dive", "What to Explore",
                "What aspect to explore deeper? (e.g., 'prove the main theorem', 'more examples', 'connections to other areas', 'practice problems')...",
                ConceptSystemPrompt,
                "Concept context:\n{previous_steps}\n\nStudent wants to explore:\n{input}\n\nProvide a deep exploration:\n1. If they want **proofs** — prove from first principles, show every step\n2. If they want **examples** — provide 3-5 worked examples of increasing difficulty\n3. If they want **connections** — map relationships to other mathematical areas\n4. If they want **applications** — real-world and cross-disciplinary applications\n5. If they want **practice** — generate 3-5 problems (don't solve them) with difficulty ratings\n\nRegardless of focus:\n- Reference important results and mathematicians\n- Show how this concept fits in the bigger picture\n- Note any open problems or active research areas\n\nUse LaTeX extensively."),
        });

    private static ToolDefinition LeanBridge() => new(
        "LeanBridge", "Lean Bridge", "\U0001F310",
        "Translate informal proofs to Lean 4 syntax. Learn formal verification by seeing your proofs in code.",
        "#4c6ef5",
        new StepDefinition[]
        {
            new(0, "Informal Proof", "Your Proof",
                "Write an informal mathematical proof that you want to formalize in Lean 4...",
                LeanSystemPrompt,
                "The student wants to formalize this proof in Lean 4:\n\n{input}\n\nFirst, analyze the proof for formalization:\n1. **Statement Analysis** — identify the precise logical structure\n   - What are the hypotheses? (∀, ∃, →, ∧, ∨)\n   - What is the conclusion?\n2. **Required Lean Concepts** — what Lean 4 features are needed?\n   - Tactics: `intro`, `apply`, `exact`, `simp`, `ring`, `omega`, `induction`, etc.\n   - Types: `Nat`, `Int`, `Real`, `Prop`, custom structures\n   - Libraries: Mathlib imports if needed\n3. **Proof Structure Map** — how each informal step maps to a Lean tactic\n4. **Potential Difficulties** — what might be tricky to formalize\n\nUse LaTeX for the math and `code blocks` for Lean syntax."),

            new(1, "Lean Translation", "Questions / Focus",
                "Any specific aspects you want to understand? Or type 'translate' for the full Lean 4 code...",
                LeanSystemPrompt,
                "Proof analysis:\n{previous_steps}\n\nStudent's request: {input}\n\nProvide the Lean 4 formalization:\n\n```lean\n-- Full Lean 4 code with detailed comments\n```\n\nFor each part of the code, explain:\n1. **Type declarations** — how mathematical objects are represented\n2. **Theorem statement** — how the informal statement becomes Lean syntax\n3. **Tactic proof** — explain each tactic used and what it does\n4. **Alternative approaches** — show `term-mode` proof if simpler\n\nAfter the code:\n- **How to Run** — instructions for trying it in Lean 4\n- **Lean vs Informal** — a side-by-side comparison table\n- **Next Steps** — what to learn next in Lean\n- **Mathlib** — mention relevant Mathlib lemmas that could simplify things\n\nMake the Lean code actually correct and runnable."),

            new(2, "Exploration", "Go Deeper",
                "Want to modify the proof? Try a harder theorem? Learn a specific Lean tactic?",
                LeanSystemPrompt,
                "Previous context:\n{previous_steps}\n\nStudent wants:\n{input}\n\nProvide:\n1. If **modifying** — show the updated Lean code with changes highlighted\n2. If **harder theorem** — suggest a natural next theorem and provide the Lean proof\n3. If **learning tactics** — explain the requested tactic with 3 examples of increasing complexity\n4. If **debugging** — explain common Lean errors and how to fix them\n\nAlways provide runnable Lean 4 code. Use Mathlib when appropriate."),
        });

    private static ToolDefinition PracticeMode() => new(
        "PracticeMode", "Practice Mode", "\U0001F3AF",
        "Progressive exercises: direct proofs, contradiction, induction, and more. Build proof skills systematically.",
        "#f59f00",
        new StepDefinition[]
        {
            new(0, "Setup", "Practice Preferences",
                "What would you like to practice? (e.g., 'proof by induction', 'epsilon-delta proofs', 'group theory', 'basic logic') and your level (beginner/intermediate/advanced)...",
                PracticeSystemPrompt,
                "The student wants to practice:\n{input}\n\nGenerate a practice set:\n1. **Warm-up** (1 problem) — straightforward application of the technique\n2. **Standard** (2 problems) — typical exam-level problems\n3. **Challenge** (1 problem) — requires combining multiple ideas\n\nFor each problem:\n- State it clearly with all definitions\n- Rate difficulty: ★☆☆ / ★★☆ / ★★★\n- Give a ONE-LINE hint (hidden behind \"Hint:\" label)\n- Do NOT provide solutions yet\n\nMake the problems interesting and varied. Use LaTeX for all notation."),

            new(1, "Work & Check", "Your Work",
                "Submit your proof/solution attempt for any of the problems, or type 'solutions' to see all answers...",
                PracticeSystemPrompt,
                "Practice set and context:\n{previous_steps}\n\nStudent's submission: {input}\n\nIf the student submitted a PROOF ATTEMPT:\n1. **Correctness** — is the proof valid? Check every step\n2. **Completeness** — are there any gaps?\n3. **Style** — is it well-written? Suggest improvements\n4. **Score** — rate it: Needs Work / Good / Excellent\n5. **Model Solution** — show the cleanest version of the proof\n\nIf the student asked for SOLUTIONS:\nProvide complete, clean solutions for all problems:\n- Show every step\n- Explain the key insights\n- Highlight common mistakes to avoid\n\nBe encouraging but honest. Use LaTeX throughout."),
        });

    private const string ProofSystemPrompt = """
        You are Epsilon, an expert mathematical proof consultant. You help students construct rigorous,
        elegant mathematical proofs. You understand proof techniques across all areas: algebra, analysis,
        topology, number theory, combinatorics, and logic. You insist on logical precision — every step
        must be justified. Use LaTeX notation: inline $...$ and display $$...$$.
        """;

    private const string ProblemSystemPrompt = """
        You are Epsilon, an expert mathematics problem solver and tutor. You help students solve problems
        using systematic approaches: understand the problem, devise a plan, carry out the plan, review.
        You can work in hint mode (one hint at a time) or full solution mode. Show every algebraic step.
        Use LaTeX: inline $...$ and display $$...$$. Always verify your answer.
        """;

    private const string ConceptSystemPrompt = """
        You are Epsilon, an expert mathematics educator who can explain concepts at any level from
        introductory to graduate. You build understanding from definitions and axioms, connect concepts
        across mathematical domains, and inspire appreciation for mathematical beauty. You balance rigor
        with intuition. Use LaTeX: inline $...$ and display $$...$$.
        """;

    private const string LeanSystemPrompt = """
        You are Epsilon, an expert in both informal mathematics and Lean 4 formal verification.
        You help students bridge the gap between informal proofs and machine-checked proofs in Lean 4.
        You know Lean 4 syntax, tactics, and Mathlib extensively. You write correct, runnable Lean code.
        When explaining, always show both the informal math (in LaTeX) and the formal Lean code side by side.
        Use LaTeX for math: inline $...$ and display $$...$$.
        Use ```lean code blocks``` for Lean 4 syntax.
        """;

    private const string PracticeSystemPrompt = """
        You are Epsilon, an expert mathematics instructor who creates pedagogically effective practice
        problems. You understand how to scaffold difficulty, give useful hints without giving away the
        answer, and provide constructive feedback on proof attempts. You are encouraging but never lower
        the standard of mathematical rigor. Use LaTeX: inline $...$ and display $$...$$.
        """;
}
