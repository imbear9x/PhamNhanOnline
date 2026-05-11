---
title: Cultivation, breakthrough, and potential allocation runtime
doc_type: system
status: verified
owner: knowledge-manager
last_verified: 2026-05-12
source_of_truth:
  - docs/implementation/extractions/cultivation-runtime-extraction.md
  - docs/implementation/extractions/player-stats-runtime-extraction.md
related_code:
  - GameServer/Runtime/CharacterCultivationService.cs
  - GameServer/Services/CultivationActionService.cs
  - GameServer/Runtime/PotentialStatCatalog.cs
---

# Cultivation, breakthrough, and potential allocation runtime

## Cultivation

- Start requires an online player in their private home instance, not expired/restricted, not already cultivating, not blocked by practice, and with active martial art absorption rate.
- Start sets runtime state to `Cultivating` and stamps cultivation timing fields.
- Stop, breakthrough, and potential allocation settle pending cultivation first.
- Settlement can grant cultivation and potential, and can stop cultivation at realm cap.

## Breakthrough

- Breakthrough requires valid current realm, cultivation at/above cap, and a next realm.
- It runs a random chance check, records the attempt, then applies either failure penalty or realm promotion.
- Success clears `PotentialRewardLocked`.
- The action wrapper reapplies final stats after successful mutation.

## Potential allocation

- Allocation validates supported target/tier/amount, spends unallocated potential, increments upgrade-count fields, refreshes previews, and reapplies authoritative final stats.
- Requests are tier-local; large requests are capped by the current tier window.

## Known limits

- Formation coefficient is a stub constant in the inspected cultivation service.
- Breakthrough failure penalty semantics are implemented in service helpers rather than a separate explicit rules object.

## Verification

Supported by extraction notes listed in `source_of_truth`.
