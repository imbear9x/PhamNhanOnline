# 07. Validation And Rules

## Rule: packet auth required

- Applies to: mọi packet có `[RequireAuth]`
- Where enforced: `AuthMiddleware`
- Input: authenticated session + packet type
- Condition: session phải `IsAuthenticated == true`
- Success: handler được chạy
- Fail: packet bị drop
- Error code/message: không thấy result packet chuẩn riêng; server log `Unauthorized packet`
- Potential bypass risk: thấp, vì check trước dispatcher
- Source: `GameServer/Network/Middleware/AuthMiddleware.cs`

## Rule: packet rate limit for realtime state

- Applies to: packet realtime có `MinIntervalMs > 0`
- Where enforced: `RateLimitMiddleware`
- Input: connection id + packet type + current tick
- Condition: không gửi cùng packet nhanh hơn min interval
- Success: packet qua middleware
- Fail: packet bị bỏ qua im lặng
- Error code/message: none
- Potential bypass risk: trung bình; là anti-spam mềm, không phải ban logic
- Source: `GameServer/Network/Middleware/RateLimitMiddleware.cs`

## Rule: annotation / packet shape validation

- Applies to: login/register/reconnect/create character/enter world/travel/allocate potential và packet có annotation validation
- Where enforced: `PacketValidationMiddleware`, validator cụ thể
- Input: packet properties
- Condition: các field required/range hợp lệ
- Success: handler chạy
- Fail: result packet `Success=false`, `Code=<validation code>`
- Error code/message: `ValidationFailed` hoặc code chuyên biệt
- Potential bypass risk: thấp cho malformed input; không thay thế domain validation
- Source: `GameServer/Network/Validations/*.cs`

## Rule: character name format

- Applies to: create/update character
- Where enforced: `CreateCharacterPacketValidator`, `CharacterService.NormalizeCharacterName`
- Input: `Name`
- Condition: trim xong dài `3..20`, chỉ gồm letter/digit/space/underscore, phải có ít nhất 1 letter/digit
- Success: cho phép tạo/update
- Fail: reject
- Error code/message: `CharacterNameInvalid`
- Potential bypass risk: thấp
- Source: `GameServer/Network/Validations/CreateCharacterPacketValidator.cs`, `GameServer/Services/CharacterService.cs`

## Rule: one character per account in current phase

- Applies to: create character
- Where enforced: `CharacterService.CreateCharacterAsync`
- Input: `accountId`
- Condition: account chưa có character nào
- Success: insert character + stats + state + home cave
- Fail: reject
- Error code/message: `CharacterAlreadyExists`
- Potential bypass risk: thấp nếu chỉ đi qua server service
- Source: `GameServer/Services/CharacterService.cs`

## Rule: reconnect token validity

- Applies to: reconnect flow
- Where enforced: `NetworkServer.TryResumeSession`
- Input: `resumeToken`
- Condition: token tồn tại, chưa revoked, chưa expired, currently disconnected
- Success: session resume ownership
- Fail: reject reconnect
- Error code/message: `ReconnectTokenInvalid`, `ReconnectSessionExpired`, `AccountLoggedInElsewhere`
- Potential bypass risk: thấp; token random 16-byte hex
- Source: `GameServer/Network/NetworkServer.cs`

## Rule: character actions restricted

- Applies to: authenticated packets while character expired/combat dead/restricted
- Where enforced: `CharacterActionRestrictionMiddleware`
- Input: session selected character + packet type
- Condition: packet không nằm trong allowlist recovery/query
- Success: packet continues
- Fail: reject and send transition packet
- Error code/message: `CharacterActionsRestricted`
- Potential bypass risk: trung bình-thấp; depends on packet classification by auth attribute
- Source: `GameServer/Network/Middleware/CharacterActionRestrictionMiddleware.cs`

## Rule: movement intent finite and state-safe

- Applies to: movement sync
- Where enforced: `CharacterPositionSyncHandler`
- Input: `CurrentPosX`, `CurrentPosY`
- Condition: finite coordinates; player exists; not defeated/cultivating/practicing/casting/stunned
- Success: desired movement target updated
- Fail: request ignored or target cleared
- Error code/message: none direct
- Potential bypass risk: movement still client-intent-based, but server clamp prevents raw teleport
- Source: `GameServer/Network/Handlers/CharacterPositionSyncHandler.cs`

## Rule: movement speed authoritative clamp

- Applies to: actual player runtime movement
- Where enforced: `GameLoop.ApplyDesiredPlayerMovement`
- Input: desired position, last sync anchor, effective move speed, elapsed seconds
- Condition: max step = speed * capped elapsed
- Success: player moves toward target at allowed speed
- Fail: desired overshoot is clamped; suspicious log maybe emitted
- Error code/message: server log only
- Potential bypass risk: medium if collision/path exploits exist; speed hack directly is reduced strongly
- Source: `GameServer/Runtime/GameLoop.cs`

## Rule: player action gate before interaction/combat/pickup

- Applies to: portal, pickup, combat, actions using target
- Where enforced: `WorldInteractionGate`
- Input: player, target, interaction type, map instance
- Condition: connected, same instance, not defeated/restricted/cultivating/practicing/casting/stunned, target valid, in range after settlement
- Success: action proceeds
- Fail: unified failure result
- Error code/message: e.g. `CharacterNotInWorldInstance`, range/state-related codes
- Potential bypass risk: low for supported target interactions
- Source: `GameServer/Runtime/WorldInteractionGate.cs`

## Rule: portal target validity

- Applies to: travel via portal
- Where enforced: `TravelToMapPacketValidator`, `TravelToMapHandler`, `MapCatalog`
- Input: `PortalId` or `TargetMapId`
- Condition: portal id > 0 or target map id > 0; portal exists, enabled, target spawn exists
- Success: map travel runs
- Fail: reject
- Error code/message: `MapPortalInvalid`, `MapIdInvalid`, other travel result codes
- Potential bypass risk: thấp
- Source: `GameServer/Network/Validations/TravelToMapPacketValidator.cs`, `GameServer/Network/Handlers/TravelToMapHandler.cs`

## Rule: zone switch validity

- Applies to: switch map zone
- Where enforced: `SwitchMapZoneHandler`
- Input: `mapId`, `zoneIndex`
- Condition: map supports zone selection, zone exists, not full, player state not blocked
- Success: update zone and republish snapshot
- Fail: reject
- Error code/message: map-zone related `MessageCode`
- Potential bypass risk: thấp
- Source: `GameServer/Network/Handlers/SwitchMapZoneHandler.cs`

## Rule: skill loadout slot validity

- Applies to: set/swap skill loadout
- Where enforced: `SkillService`
- Input: `slotIndex`, `playerSkillId`
- Condition: slot in range, skill owned, canonical skill exists, equipment-granted realm requirement pass
- Success: loadout rows updated
- Fail: reject or invalid rows normalized away
- Error code/message: `SkillLoadoutSlotInvalid`, `PlayerSkillInvalid`, `SkillLoadoutBlocked`, `SkillLoadoutSlotEmpty`
- Potential bypass risk: thấp; server re-normalizes stored loadout too
- Source: `GameServer/Services/SkillService.cs`

## Rule: combat target/range/cooldown

- Applies to: `AttackEnemyPacket`
- Where enforced: `AttackEnemyHandler`
- Input: target + slot
- Condition: equipped skill exists, target type supported, target still valid, range pass with grace, cooldown ready
- Success: enqueue cast
- Fail: reject
- Error code/message: skill/target/range-related `MessageCode`
- Potential bypass risk: thấp
- Source: `GameServer/Network/Handlers/AttackEnemyHandler.cs`

## Rule: inventory ownership and location

- Applies to: equip/use/drop/consume/remove/move
- Where enforced: `ItemService`, `EquipmentService`, `ItemUseService`
- Input: `playerItemId`, `playerId`, quantity
- Condition: row exists, belongs to player if inventory action, correct `LocationType`, not expired
- Success: mutation proceeds
- Fail: reject/throw
- Error code/message: often `InventoryItemInvalid`, `UnknownError`, or invalid operation converted by handler
- Potential bypass risk: thấp if all gameplay mutations go through service layer
- Source: `GameServer/Services/ItemService.cs`, `GameServer/Services/EquipmentService.cs`, `GameServer/Services/ItemUseService.cs`

## Rule: equipped or inserted item cannot leave inventory

- Applies to: remove/drop/delete/consume item
- Where enforced: `ItemService.EnsureItemCanLeaveInventoryAsync`
- Input: `playerItemId`
- Condition: item không đang equipped; soil không đang inserted vào plot
- Success: item can be removed
- Fail: reject
- Error code/message: invalid operation text, surfaced as failure
- Potential bypass risk: thấp
- Source: `GameServer/Services/ItemService.cs`

## Rule: equipment slot count

- Applies to: equip/unequip
- Where enforced: `EquipmentService`
- Input: requested slot index
- Condition: `1 <= slotIndex <= CharacterEquipmentSlotCount`
- Success: equip proceeds
- Fail: reject
- Error code/message: `EquipmentSlotInvalid`
- Potential bypass risk: thấp
- Source: `GameServer/Services/EquipmentService.cs`, `GameServer/Config/GameConfigValues.cs`

## Rule: item use type support

- Applies to: generic `UseItemPacket`
- Where enforced: `ItemUseService`
- Input: item type
- Condition: type is `Equipment`, `MartialArtBook`, `PillRecipeBook`, or supported `Consumable`
- Success: server applies effect/learn/equip
- Fail: reject unsupported type/effect
- Error code/message: `ItemUseUnsupported`-style game exception, mapped in result packet
- Potential bypass risk: thấp
- Source: `GameServer/Services/ItemUseService.cs`

## Rule: martial art duplicate learn blocked

- Applies to: using martial art book
- Where enforced: `ItemUseService`, martial art service
- Input: mapped martial art id
- Condition: player chưa học martial art đó
- Success: consume book and insert `player_martial_arts`
- Fail: reject
- Error code/message: code surfaced in `UseItemResultPacket`
- Potential bypass risk: thấp
- Source: `GameServer/Services/ItemUseService.cs`

## Rule: cultivation start restrictions

- Applies to: `StartCultivationPacket`
- Where enforced: `CharacterCultivationService`
- Input: online player runtime state
- Condition: entered world, private home instance, active martial art exists, not practicing/casting/stunned/expired, not already cultivating
- Success: current state -> cultivating
- Fail: reject
- Error code/message: cultivation-related `MessageCode`
- Potential bypass risk: thấp
- Source: `GameServer/Runtime/CharacterCultivationService.cs`

## Rule: breakthrough only at cap and with next realm

- Applies to: `BreakthroughPacket`
- Where enforced: `CharacterCultivationService`
- Input: base stats + realm config
- Condition: settle first; current cultivation reached max; next realm exists
- Success: random roll runs and maybe advances realm
- Fail: reject or fail roll with penalty
- Error code/message: breakthrough-related `MessageCode`
- Potential bypass risk: thấp
- Source: `GameServer/Runtime/CharacterCultivationService.cs`

## Rule: potential allocation by tier preview

- Applies to: `AllocatePotentialPacket`
- Where enforced: `PotentialStatCatalog`, `CharacterCultivationService`
- Input: target stat + requested potential amount
- Condition: target enum valid, tiers available, player has unallocated potential
- Success: spend potential and increase upgrade counts/base stats
- Fail: reject or partially spend only up to current tier cap
- Error code/message: allocate potential result code
- Potential bypass risk: thấp
- Source: `GameServer/Runtime/PotentialStatCatalog.cs`, `GameServer/Runtime/CharacterCultivationService.cs`

## Rule: learned recipe required for alchemy

- Applies to: recipe detail/preview/craft
- Where enforced: `AlchemyCraftQueryService`, `AlchemyService`, `AlchemyCraftActionService`
- Input: `pillRecipeTemplateId`
- Condition: player has row in `player_pill_recipes`
- Success: detail/preview/craft allowed
- Fail: reject
- Error code/message: failure reason text in alchemy preview/action
- Potential bypass risk: thấp
- Source: `GameServer/Services/AlchemyService.cs`, `GameServer/Services/AlchemyCraftActionService.cs`

## Rule: alchemy input ownership and quantity

- Applies to: preview and craft pill
- Where enforced: `AlchemyService.ValidateCraftPillAsync`
- Input: selected item ids, optional inputs, requested craft count
- Condition: all selected items belong to player, not expired, not inserted soil/equipped item where disallowed, mandatory inputs sufficient
- Success: build preview/rate plan or consume inputs on craft start
- Fail: reject with `FailureReason`
- Error code/message: human-readable failure reason
- Potential bypass risk: thấp
- Source: `GameServer/Services/AlchemyService.cs`

## Rule: herb maturity optional input not yet supported

- Applies to: alchemy recipes with `required_herb_maturity`
- Where enforced: `AlchemyService.ValidateCraftPillAsync`
- Input: recipe config
- Condition: if any input requires herb maturity, current phase rejects
- Success: none in current phase
- Fail: reject craft preview/action
- Error code/message: `"Recipe co required_herb_maturity, tinh nang nay de phase sau."`
- Potential bypass risk: thấp
- Source: `GameServer/Services/AlchemyService.cs`

## Rule: practice session world/state restriction

- Applies to: start practice, pause/resume/cancel
- Where enforced: `PracticeService`, `AlchemyCraftActionService`
- Input: online session + latest practice session
- Condition: private home, not cultivating, not stunned/casting, valid current session state
- Success: session lifecycle transition succeeds
- Fail: reject
- Error code/message: practice-related codes
- Potential bypass risk: thấp
- Source: `GameServer/Services/PracticeService.cs`, `GameServer/Services/AlchemyCraftActionService.cs`

## Rule: notification acknowledge owner check

- Applies to: `AcknowledgePlayerNotificationPacket`
- Where enforced: `PlayerNotificationService`
- Input: notification id + current player
- Condition: notification exists and belongs to current character
- Success: set `read_at_utc`
- Fail: reject
- Error code/message: `CharacterMustEnterWorld`, `NotificationInvalid`
- Potential bypass risk: thấp
- Source: `GameServer/Services/PlayerNotificationService.cs`
