namespace PhamNhanOnline.Client.Features.Targeting.Application
{
    public enum WorldTargetInteractionMode
    {
        None = 0,
        ContextOnly = 1,
        HostileAttack = 2
    }

    public static class WorldTargetInteractionRules
    {
        public static WorldTargetInteractionMode Resolve(WorldTargetHandle handle)
        {
            if (!handle.IsValid)
                return WorldTargetInteractionMode.None;

            switch (handle.Kind)
            {
                case WorldTargetKind.Enemy:
                case WorldTargetKind.Boss:
                    return WorldTargetInteractionMode.HostileAttack;
                case WorldTargetKind.Player:
                case WorldTargetKind.Npc:
                    return WorldTargetInteractionMode.ContextOnly;
                default:
                    return WorldTargetInteractionMode.None;
            }
        }

        public static bool IsHostile(WorldTargetHandle handle)
        {
            return Resolve(handle) == WorldTargetInteractionMode.HostileAttack;
        }
    }
}
