namespace AdminDesignerTool;

internal sealed record AdminTableLoadRequest(
    AdminResourceDefinition Resource,
    string? SelectSql = null,
    string? TitleOverride = null,
    string? DescriptionOverride = null,
    string? HelpTextOverride = null,
    IReadOnlyDictionary<string, object?>? NewRowDefaults = null)
{
    public static AdminTableLoadRequest ForResource(AdminResourceDefinition resource)
    {
        return new AdminTableLoadRequest(resource);
    }

    public string EffectiveTitle => TitleOverride ?? $"{Resource.DisplayName}  ({Resource.TableName})";
    public string EffectiveDescription => DescriptionOverride ?? Resource.Description;
    public string EffectiveHelpText => HelpTextOverride ?? Resource.HelpText;
    public string EffectiveSelectSql => SelectSql ?? Resource.SelectSql;
}
