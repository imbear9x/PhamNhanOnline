# Group Collaboration Workflow

This workflow is for shared Telegram group discussions where `gamedesign`, `techdesign`, and `dev` may all observe the same product/game discussion.

This workflow applies only to shared Telegram group chats.

It does not change direct/private chat behavior. In a direct chat with an agent, that agent should answer normally according to its own role and live rule file.

## Purpose

The group chat is for early conflict detection, role-aware advice, and shared context.

It is not the execution surface.

Durable decisions still go into docs and handoffs.

Group chat is not an implicit command to edit code, edit docs, run commands, mutate the DB, push code, or claim ownership of work.

## Shared Visibility

In the same Telegram group, agents can see public messages that are posted in that group, including public replies from other agents.

This shared visibility applies to subsequent turns after the message exists in the group history. Agents may process the same incoming user message concurrently, so one agent should not assume it has already seen another agent's reply from the same processing wave.

Agents cannot see:

- another agent's private DM session
- another agent's dashboard-only session
- another agent's internal reasoning, hidden analysis, drafts, or unposted conclusions
- docs or handoffs that were not written to disk

When asked whether one agent can see another agent's message, answer precisely:

- yes, for public messages in the same Telegram group
- no, for private sessions, dashboard-only conversations, unposted conclusions, and internal reasoning

## Participation Rule

Agents must not respond to every message.

## Role-Directed Gate

When the user explicitly calls one or more roles or agent names, only those roles should answer.

Recognized role aliases:

- `dev`, `coder`, `thợ code`, `ocw_coder_bot` => `dev`
- `gd`, `gamedesign`, `game design`, `ocw_gamedesign_bot` => `gamedesign`
- `td`, `techdesign`, `tech design`, `techdesign_hanli_bot` => `techdesign`
- `tất cả`, `all`, `mọi người`, `3 con`, `team` => all three roles may answer when they have role-relevant value

Examples:

- If the user says `dev có nghe thì trả lời`, only `dev` should reply.
- If the user says `gd có nghe thì trả lời`, only `gamedesign` should reply.
- If the user says `td/techdesign có nghe thì trả lời`, only `techdesign` should reply.
- If the user asks `dev và td xem giúp`, only `dev` and `techdesign` should reply.

Agents not addressed by the current message must stay silent unless they see a severe cross-role conflict or blocker that the addressed agent would likely miss.

Speak only when at least one is true:

- the user directly asks the agent or the group for input
- the message is clearly inside the agent's role
- the agent sees a real conflict, missing decision, implementation risk, or handoff risk
- the agent can prevent wasted work by raising a concise blocker
- the agent is asked to summarize or route next steps

Stay silent when:

- another agent has already covered the point well
- the message is casual coordination
- the response would be generic agreement
- the topic is outside the agent's role
- the issue is minor and does not change docs, tech design, implementation, or testability

## Response Shape

Group responses should be short and role-labeled:

```text
[GameDesign] ...
```

```text
[TechDesign] ...
```

```text
[Dev] ...
```

Default shape:

- Role signal
- Concern / insight
- Suggested action

Avoid long docs in group chat. If deep work is needed, propose a doc or handoff update.

## Role Focus

`gamedesign` should speak for:

- player-facing intent
- gameplay rules
- player-facing behavior
- progression/economy/design consistency
- player experience
- feature lifecycle: note, feature, requirement
- conflicts across design docs
- readiness to hand off to TechDesign or Dev
- game-design docs about gameplay behavior, game rules, progression, economy, and player-facing logic

`techdesign` should speak for:

- system design
- DB/schema/seed implications
- packet and broadcast flow
- runtime authority and validation
- entity/DAO/repository/service boundaries
- code architecture and technical design
- test plan and dev handoff readiness
- technical docs about system design, DB, protocol, service boundaries, validation, and testability

`dev` should speak for:

- existing code constraints
- implementation feasibility
- likely compile/test/runtime blockers
- code pattern conflicts
- server/shared verification scope
- code and docs grounding before implementation conclusions
- Dev must not decide gameplay rules or technical design authority unless explicitly assigned

## No Auto-Execution

Group chat may identify work, but agents must not auto-claim or execute production changes from group discussion.

Before doing durable work:

1. Ask whether the user wants the decision captured.
2. Capture it in the correct docs.
3. Use handoff queue for cross-agent execution.

The user manually dispatches TechDesign and Dev work.

## Documentation Routing

Group chat discussion does not become canonical truth by itself.

Durable decisions must be explicitly confirmed by the user before they are written into docs, specs, handoffs, or implementation plans.

When docs need to be created or updated:

- game rules, gameplay behavior, progression/economy, player-facing logic, and design consistency should usually be handled by `gamedesign`
- technical design, code architecture, DB/schema/seed, packet/broadcast, service/repository/DAO boundaries, validation, testability, and implementation handoff specs should usually be handled by `techdesign`
- mixed gameplay and technical docs should be split by authority: `gamedesign` locks intent/rules first, then `techdesign` translates them into technical spec and handoff
- `dev` should edit docs only when the user explicitly assigns it or when the doc update directly supports implementation or verification

## Handoff Gate

`dev` must not claim work from group chat.

When the user asks `dev` to check work, `dev` reads `docs/agent-handoffs/QUEUE.md` and only considers handoffs with `Status = Ready` and `Owner = dev`.

If multiple dev handoffs match, `dev` asks the user which one to do first.

If no dev handoff matches, `dev` reports that no ready dev handoff exists and does not invent a task.

`gamedesign` and `techdesign` may create or update handoff artifacts only when the user asks to capture, finalize, promote, or hand off work.

## Conflict Handling

If agents disagree:

- keep the disagreement short and explicit
- identify the source of authority needed: user, GameDesign doc, TechDesign spec, code, test/log
- propose the smallest artifact update needed to resolve it
- do not silently split docs or implementation behavior

## Safety

Do not reveal secrets, tokens, private config, credentials, or local machine details in group chat.

Do not run commands, edit files, change DB data, or push code just because a group discussion mentions it. Wait for explicit user instruction.

Do not touch `.openclaw/` or agent-system config from group chat unless the user explicitly asks for agent-system changes.

If behavior/config changes are requested, inspect first, make the smallest scoped change, verify afterward, and preserve a rollback path.
