using GameShared.Messages;

namespace GameShared.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ValidationCodeAttribute : Attribute
{
    public MessageCode Code { get; }

    public ValidationCodeAttribute(MessageCode code)
    {
        Code = code;
    }
}
