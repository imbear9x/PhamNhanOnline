---
title: Martial art ownership and activation runtime
doc_type: system
status: verified
owner: knowledge-manager
last_verified: 2026-05-12
source_of_truth:
  - docs/implementation/extractions/martial-arts-runtime-extraction.md
related_code:
  - GameServer/Services/MartialArtService.cs
  - GameServer/Services/MartialArtActionService.cs
  - GameServer/Network/Handlers/GetOwnedMartialArtsHandler.cs
  - GameServer/Network/Handlers/SetActiveMartialArtHandler.cs
  - GameServer/Network/Handlers/UseMartialArtBookHandler.cs
---

# Martial art ownership and activation runtime

## Runtime behavior

- Ownership is stored in `PlayerMartialArt` rows and projected into ordered DTOs.
- Martial art books validate item ownership and referenced definitions, reject duplicate learning, create stage `1` / exp `0` ownership, consume the book, and reinitialize base stats.
- DTOs include compiled description, stage/exp, required exp, qi absorption rate, and active flag.
- Active martial art can be cleared with a non-positive id or set to an owned martial art id.
- Action service blocks switching while cultivating or practicing, then reapplies final stats and rebuilds cultivation preview.

## Client/server surface

- `GetOwnedMartialArtsPacket`
- `SetActiveMartialArtPacket`
- `UseMartialArtBookPacket`

## Limits

- This doc covers learning/selection/runtime impact only. Stage progression beyond these paths was not evidenced here.

## Verification

Supported by `docs/implementation/extractions/martial-arts-runtime-extraction.md`.
