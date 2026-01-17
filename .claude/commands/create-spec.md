You will help me create a detailed specification for a new project/feature/system.

## Phase 1: Information Gathering (Interactive)
Ask me clarifying questions about what needs to be specified. I'll provide answers. Continue asking until you have sufficient understanding.

## Phase 2: Planning (Plan Mode) - ENTER PLAN MODE NOW
Before writing the specification:
1. Outline the key topics/sections the spec must cover
2. Identify dependencies and prerequisite knowledge needed
3. List any assumptions you're making
4. Ask final clarification questions if anything is ambiguous
5. Confirm understanding is complete before proceeding

## Phase 3: Specification Writing
Create two separate markdown files:

**File 1: `complete-spec.md`**
- Full specification with all details
- Architecture diagrams (Mermaid)
- Implementation pseudocode where relevant
- Complete implementation steps plan
- Technical rationale and design decisions

**File 2: `developer-guide.md`**
- Simplified, developer-focused version
- Step-by-step implementation requirements with explanations
- Essential terminology/concepts glossary
- Key architecture diagrams (Mermaid)
- Only what's needed to implement - no noise

**File 3: `introduction.md`**
- Conceptual overview - what problem does this solve?
- What will this system/feature achieve?
- High-level benefits and outcomes
- All required terminology with clear definitions
- Key patterns, methodologies, and principles used (explained conceptually)
- Architecture philosophy and design approach
- No code, no pseudocode - purely educational foundation

## Rules
- No actual code - use pseudocode only when clarifying logic
- Mermaid diagrams: flowcharts, sequence diagrams, architecture diagrams
- Keep language concise and precise
- Avoid loops - if stuck, explicitly ask for clarification
- Write files to `docs/specs/{name_of_feature}` directory

**Ready to begin. What would you like me to specify?**