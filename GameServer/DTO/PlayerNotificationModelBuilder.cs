using GameServer.Descriptions;
using GameServer.Entities;
using GameServer.Runtime;
using GameShared.Models;
using System.Text.Json;

namespace GameServer.DTO;

public sealed class PlayerNotificationModelBuilder
{
    private static readonly JsonSerializerOptions NotificationJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ItemDefinitionCatalog _itemDefinitions;
    private readonly GameplayDescriptionService _descriptions;

    public PlayerNotificationModelBuilder(
        ItemDefinitionCatalog itemDefinitions,
        GameplayDescriptionService descriptions)
    {
        _itemDefinitions = itemDefinitions;
        _descriptions = descriptions;
    }

    public PlayerNotificationModel Build(PlayerNotificationEntity entity)
    {
        var items = BuildNotificationItems(entity);
        return new PlayerNotificationModel
        {
            NotificationId = entity.Id,
            NotificationType = entity.NotificationType,
            SourceType = entity.SourceType,
            SourceId = entity.SourceId,
            Title = entity.Title ?? string.Empty,
            Message = entity.Message ?? string.Empty,
            DisplayItem = entity.DisplayItemTemplateId.HasValue
                ? BuildItemTemplateSummary(entity.DisplayItemTemplateId.Value)
                : null,
            Items = items.Count > 0 ? items : null,
            CreatedUnixMs = ToUnixMs(entity.CreatedAtUtc)
        };
    }

    private List<NotificationItemModel> BuildNotificationItems(PlayerNotificationEntity entity)
    {
        var result = new List<NotificationItemModel>();
        if (!string.IsNullOrWhiteSpace(entity.PayloadJson))
        {
            try
            {
                var payload = JsonSerializer.Deserialize<NotificationPayloadDto>(entity.PayloadJson, NotificationJsonOptions);
                if (payload?.Rewards != null)
                {
                    for (var i = 0; i < payload.Rewards.Count; i++)
                    {
                        var reward = payload.Rewards[i];
                        if (reward == null || !reward.ItemTemplateId.HasValue || !reward.Quantity.HasValue || reward.Quantity.Value <= 0)
                            continue;

                        result.Add(new NotificationItemModel
                        {
                            Item = BuildItemTemplateSummary(reward.ItemTemplateId.Value),
                            Quantity = reward.Quantity.Value
                        });
                    }
                }
            }
            catch (JsonException)
            {
            }
        }

        if (result.Count == 0 && entity.DisplayItemTemplateId.HasValue)
        {
            result.Add(new NotificationItemModel
            {
                Item = BuildItemTemplateSummary(entity.DisplayItemTemplateId.Value),
                Quantity = 1
            });
        }

        return result;
    }

    private ItemTemplateSummaryModel BuildItemTemplateSummary(int itemTemplateId)
    {
        if (!_itemDefinitions.TryGetItem(itemTemplateId, out var definition))
            throw new InvalidOperationException($"Item template {itemTemplateId} was not found.");

        return new ItemTemplateSummaryModel
        {
            ItemTemplateId = definition.Id,
            Code = definition.Code,
            Name = definition.Name,
            ItemType = (int)definition.ItemType,
            Rarity = (int)definition.Rarity,
            Icon = definition.Icon,
            BackgroundIcon = definition.BackgroundIcon,
            Description = _descriptions.BuildItemDescription(definition),
            MaxStack = definition.MaxStack,
            IsStackable = definition.IsStackable
        };
    }

    private static long ToUnixMs(DateTime dateTime)
    {
        var utc = dateTime.Kind == DateTimeKind.Utc
            ? dateTime
            : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

        return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
    }

    private sealed class NotificationPayloadDto
    {
        public List<NotificationRewardDto>? Rewards { get; set; }
    }

    private sealed class NotificationRewardDto
    {
        public int? ItemTemplateId { get; set; }
        public int? Quantity { get; set; }
    }
}
