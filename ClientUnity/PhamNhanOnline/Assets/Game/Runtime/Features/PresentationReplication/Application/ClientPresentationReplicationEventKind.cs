namespace PhamNhanOnline.Client.Features.PresentationReplication.Application
{
    public enum ClientPresentationReplicationEventKind
    {
        None = 0,
        MapChanged = 1,
        SkillCastStarted = 2,
        SkillCastReleased = 3,
        SkillImpactResolved = 4,
        GroundRewardUpserted = 5,
        GroundRewardRemoved = 6
    }
}
