# OpenClaw Local Architecture On This Machine

## Purpose

Tài liệu này giải thích cách OpenClaw đang được tổ chức trên máy local hiện tại:

- file config chính nằm ở đâu
- agent nào đang tồn tại
- mỗi agent đọc prompt/rule từ đâu
- session, transcript, trace nằm ở đâu
- tin nhắn Telegram đi qua những lớp nào trước khi thành reply

Mục tiêu là để nhìn một lần là có mental model đủ rõ để tự lần theo hệ thống.

## One-Minute Picture

```text
Telegram bot / Web UI / CLI
        |
        v
OpenClaw Gateway
  - auth
  - routing
  - tool policy
  - channel delivery
        |
        v
Agent Runtime
  - chọn agent
  - nạp workspace prompt files
  - chọn model qua 9Router
  - chạy tool
        |
        v
Session Store + Trace + Media + Logs
```

Ý chính:

- `~/.openclaw/openclaw.json` là file nối dây toàn hệ thống
- `~/.openclaw/workspaces/...` là nơi chứa prompt/rule/identity của agent
- `~/.openclaw/agents/.../sessions` là nơi chứa transcript và trace thực thi
- `9Router` là tầng model gateway, không phải agent

## Main Folders

```text
~/.openclaw/
├── openclaw.json                 # config trung tâm
├── agents/                       # private runtime state của từng agent
├── workspace/                    # workspace mặc định của main
├── workspaces/                   # workspace riêng của manager/devops/knowledge-manager...
├── secrets/                      # token, secret
├── logs/                         # log runtime/gateway
├── devices/                      # pairing / device access
├── media/                        # inbound + outbound media
├── plugins/                      # plugin state
├── plugin-skills/                # skill/plugin phụ thêm
├── telegram/                     # telegram cache/runtime data
├── tasks/                        # task state
├── delivery-queue/               # retry queue cho message delivery
├── session-delivery-queue/       # retry queue theo session
└── tools/node-v24.15.0/...       # local OpenClaw install đang chạy
```

## Central Config

File trung tâm là:

- `~/.openclaw/openclaw.json`

Đây là file quan trọng nhất. Nếu muốn hiểu hệ thống đang chạy thế nào, đọc file này trước.

### `agents.defaults`

Đoạn này định nghĩa mặc định cho mọi agent:

```json
"agents": {
  "defaults": {
    "workspace": "/home/vm-01/.openclaw/workspace",
    "model": { "primary": "9router/cx/gpt-5.4" },
    "thinkingDefault": "low",
    "params": {
      "cacheRetention": "long",
      "maxTokens": 4000
    }
  }
}
```

Dùng để làm gì:

- nếu agent không override gì thì nó kế thừa các giá trị này
- quy định model mặc định
- quy định style runtime như thinking level và token cap

### `agents.list`

Đây là roster agent thực tế:

```json
{
  "id": "dev",
  "workspace": "/home/vm-01/Project/PhamNhanOnline",
  "agentDir": "/home/vm-01/.openclaw/agents/dev/agent",
  "model": "9router/cx/gpt-5.4"
}
```

Mỗi entry thường trả lời 4 câu:

- agent tên gì
- nó làm việc trên workspace nào
- private runtime state của nó nằm ở đâu
- model mặc định là gì

### `gateway`

Ví dụ:

```json
"gateway": {
  "mode": "local",
  "auth": { "mode": "token" },
  "port": 18789,
  "bind": "loopback"
}
```

Dùng để làm gì:

- bật gateway local
- quyết định cổng đang lắng nghe
- quyết định auth mode
- quyết định có mở ra LAN/tailscale hay chỉ local

### `tools`

Ví dụ:

```json
"tools": {
  "profile": "coding",
  "alsoAllow": ["message"],
  "elevated": {
    "enabled": true,
    "allowFrom": {
      "telegram": ["797932570"]
    }
  }
}
```

Dùng để làm gì:

- agent được dùng tool gì
- có cho exec/elevated không
- user/channel nào được phép gọi hành động nhạy cảm hơn

Ghi nhớ:

- `profile` là base allowlist
- `alsoAllow` là cộng thêm tool ngoài profile
- per-agent override có thể thay đổi riêng từng agent

### `models.providers`

Máy này đang route model qua 9Router:

```json
"models": {
  "providers": {
    "9router": {
      "baseUrl": "http://127.0.0.1:20128/v1",
      "api": "openai-completions"
    }
  }
}
```

Dùng để làm gì:

- bảo OpenClaw gọi model qua endpoint nào
- model ids nào được coi là hợp lệ
- auth type nào được dùng

Mental model:

- OpenClaw chọn model id
- OpenClaw không tự host model
- request được bắn sang 9Router
- 9Router mới là thằng route tiếp xuống provider/model thật

### `plugins`

Máy này đang allow:

- `telegram`
- `opengauge`
- `memory-core`

Dùng để làm gì:

- Telegram plugin: kênh chat Telegram
- OpenGauge plugin: quan sát/guard usage
- Memory Core plugin: memory/retrieval

### `channels.telegram`

Đây là map bot Telegram:

```json
"channels": {
  "telegram": {
    "accounts": {
      "manager": { "tokenFile": "..." },
      "dev": { "tokenFile": "..." },
      "gamedesign": { "tokenFile": "..." },
      "devops": { "tokenFile": "..." },
      "knowledge-manager": { "tokenFile": "..." }
    }
  }
}
```

Dùng để làm gì:

- khai báo mỗi bot Telegram
- bật/tắt từng account bot
- chỉ ra secret token đọc từ file nào

### `bindings`

Đây là bảng route từ channel account sang agent:

```json
{
  "type": "route",
  "agentId": "dev",
  "match": {
    "channel": "telegram",
    "accountId": "dev"
  }
}
```

Dùng để làm gì:

- bot `dev` nhận tin nhắn thì route vào agent `dev`
- bot `knowledge-manager` nhận tin nhắn thì route vào agent `knowledge-manager`

Nếu route sai, user sẽ nhắn đúng bot nhưng tin lại vào nhầm agent.

## Agent Workspaces

### Current Workspaces

```text
~/.openclaw/workspace/                      -> main
~/.openclaw/workspaces/manager/            -> manager
~/.openclaw/workspaces/devops/             -> devops
~/.openclaw/workspaces/knowledge-manager/  -> knowledge-manager
/home/vm-01/Project/PhamNhanOnline         -> dev
/home/vm-01/Project/PhamNhanOnline/docs/game-design-wp -> gamedesign
```

### Common Files Inside A Workspace

Mỗi workspace thường có 5 file prompt/rule chính:

#### `AGENTS.md`

Vai trò:

- job description
- hard boundaries
- cách report
- rule chuyên môn

Ví dụ:

- `dev` đọc [dev/AGENTS.md](/home/vm-01/.openclaw/workspaces/dev/AGENTS.md)
- `knowledge-manager` đọc [knowledge-manager/AGENTS.md](/home/vm-01/.openclaw/workspaces/knowledge-manager/AGENTS.md)

Hiểu đơn giản:

- `AGENTS.md` = nghề của nó và luật chơi của nó

#### `IDENTITY.md`

Vai trò:

- tên agent
- vibe
- role identity

Hiểu đơn giản:

- `IDENTITY.md` = "mày là ai"

#### `SOUL.md`

Vai trò:

- giọng điệu
- personality
- default speaking style

Hiểu đơn giản:

- `SOUL.md` = "mày nói kiểu gì"

#### `TOOLS.md`

Vai trò:

- machine-local knowledge
- runbook
- infra notes
- shortcuts mà không nên hardcode vào skill chung

Ví dụ trên máy này:

- `devops/TOOLS.md` có runbook đổi model OpenClaw
- `manager/TOOLS.md` có shortcut `bin/delegate-dev`
- `dev/TOOLS.md` có note về 9Router

Hiểu đơn giản:

- `TOOLS.md` = cheat sheet theo máy

#### `HEARTBEAT.md`

Vai trò:

- quy định khi heartbeat/proactive wake chạy thì agent phải làm gì

Nếu file gần như trống:

- agent không có periodic duty đáng kể

## Agent Runtime State

State runtime không nằm trong workspace, mà nằm dưới:

- `~/.openclaw/agents/<agentId>/`

Ví dụ:

```text
~/.openclaw/agents/dev/
├── agent/
│   └── models.json
└── sessions/
    ├── sessions.json
    ├── <session-id>.jsonl
    ├── <session-id>.trajectory.jsonl
    └── .usage-cost-cache.json
```

### `agent/models.json`

Vai trò:

- state model cục bộ của agent
- metadata model/runtime liên quan agent đó

### `sessions/sessions.json`

Vai trò:

- session index
- session metadata
- current route / model override / provider override

Đây là file rất hữu ích khi debug:

- tại sao UI bảo model A nhưng backend vẫn đang dùng model B
- session nào đang tồn tại
- route của session đi qua channel/account nào

### `sessions/<id>.jsonl`

Vai trò:

- transcript chat của session

Chứa:

- user message
- assistant reply
- tool call/result

Hiểu đơn giản:

- đây là "lịch sử chat thật"

### `sessions/<id>.trajectory.jsonl`

Vai trò:

- execution trace kỹ thuật

Chứa:

- model đã dùng
- usage token
- tool calls
- prompt/runtime metadata

Hiểu đơn giản:

- transcript nói chuyện
- trajectory nói cách máy đã chạy

### `checkpoint.*.jsonl`

Vai trò:

- snapshot tạm khi run dài hoặc interrupted

### `.usage-cost-cache.json`

Vai trò:

- cache usage/cost để status UI/CLI nhanh hơn

## End-To-End Flow: Telegram To Agent

Ví dụ user nhắn bot `knowledge-manager`.

### Step 1: Telegram bot nhận message

Bot token được khai báo ở:

- `channels.telegram.accounts.knowledge-manager.tokenFile`

Telegram plugin đọc bot token và polling/webhook để nhận tin nhắn.

### Step 2: OpenClaw route sang agent

`bindings` nói rằng:

- `channel=telegram`
- `accountId=knowledge-manager`
- route -> `agentId=knowledge-manager`

### Step 3: Session được resolve

Config hiện tại:

```json
"session": {
  "dmScope": "per-channel-peer"
}
```

Nghĩa là:

- direct message Telegram của mỗi user sẽ có session riêng theo peer
- không phải tất cả DM đều dồn vào một bucket chung

### Step 4: Agent workspace được nạp

Runtime đọc các file prompt:

- `AGENTS.md`
- `IDENTITY.md`
- `SOUL.md`
- `TOOLS.md`
- `HEARTBEAT.md` nếu có liên quan

### Step 5: Model được resolve

Agent `knowledge-manager` hiện cấu hình:

- `9router/cx/gpt-5.5-review`

OpenClaw kiểm tra model này có nằm trong registry model hợp lệ không, rồi bắn request qua 9Router.

### Step 6: Tool execution nếu cần

Nếu agent cần:

- đọc file
- chạy lệnh
- gửi message/file

thì tool policy trong `openclaw.json` quyết định nó có được phép gọi tool đó không.

### Step 7: Reply delivery

Nếu chỉ là text reply thường:

- gateway gửi text ngược về Telegram

Nếu user yêu cầu file:

- agent cần tạo file local
- rồi dùng `message` tool để deliver file/media vào chat

### Step 8: Transcript + trace được ghi lại

Kết quả được lưu vào:

- `sessions/<id>.jsonl`
- `sessions/<id>.trajectory.jsonl`

## When To Edit Which File

### Muốn đổi model mặc định toàn hệ thống

Sửa:

- `openclaw.json` -> `agents.defaults.model.primary`

### Muốn đổi model của một agent cụ thể

Sửa:

- `openclaw.json` -> `agents.list[].model`

### Muốn đổi giọng điệu của một agent

Sửa:

- workspace của agent -> `SOUL.md`

### Muốn đổi role/rule của agent

Sửa:

- workspace của agent -> `AGENTS.md`

### Muốn thêm machine-local note/runbook

Sửa:

- workspace của agent -> `TOOLS.md`

### Muốn đổi route bot Telegram -> agent

Sửa:

- `openclaw.json` -> `bindings`

### Muốn bật/tắt bot Telegram

Sửa:

- `openclaw.json` -> `channels.telegram.accounts.<id>.enabled`

### Muốn debug session đang dùng model gì thật

Đọc:

- `~/.openclaw/agents/<agentId>/sessions/sessions.json`
- `~/.openclaw/agents/<agentId>/sessions/*.trajectory.jsonl`

## Fast Mental Model

Nếu phải nhớ thật ngắn, nhớ 4 câu này:

1. `openclaw.json` là bản nối dây toàn hệ thống.
2. `workspaces/` là não và tính cách của agent.
3. `agents/<id>/sessions/` là ký ức phiên chat và trace runtime.
4. `9Router` là cổng model backend, không phải agent.

## Good First Files To Open

Nếu muốn tự khám phá từ dễ đến khó:

1. [openclaw.json](/home/vm-01/.openclaw/openclaw.json)
2. [manager/AGENTS.md](/home/vm-01/.openclaw/workspaces/manager/AGENTS.md)
3. [dev/AGENTS.md](/home/vm-01/.openclaw/workspaces/dev/AGENTS.md)
4. [devops/TOOLS.md](/home/vm-01/.openclaw/workspaces/devops/TOOLS.md)
5. [knowledge-manager/TOOLS.md](/home/vm-01/.openclaw/workspaces/knowledge-manager/TOOLS.md)
6. [dev sessions.json](/home/vm-01/.openclaw/agents/dev/sessions/sessions.json)
7. một file `*.trajectory.jsonl` bất kỳ trong `~/.openclaw/agents/<agent>/sessions/`

## Verified Scope

Tài liệu này được viết dựa trên trạng thái máy local hiện tại:

- `~/.openclaw/openclaw.json`
- cây thư mục `~/.openclaw/`
- workspace files hiện có
- session store hiện có

Nó mô tả kiến trúc local hiện tại của máy này, không phải tài liệu generic cho mọi máy OpenClaw khác.
