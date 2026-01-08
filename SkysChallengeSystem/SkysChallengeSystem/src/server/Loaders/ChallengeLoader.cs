using System;
using SkysChallengeSystem.Server.Challenges;
using SkysChallengeSystem.Shared;

namespace SkysChallengeSystem.Server.Loaders;

public abstract class ChallengeLoader<T> : ChallengeLoader where T : ChallengeRecord
{
    public override Type RecordType => typeof(T);
    public abstract Challenge LoadChallenge(T record);

    public override Challenge LoadChallenge(ChallengeRecord record)
        => record is T recordT ? LoadChallenge(recordT) : null;
}

// You see nothing, ignore this
// Exists purely to act as a generic return value
public abstract class ChallengeLoader
{
    public abstract Type RecordType { get; }

    public abstract Challenge LoadChallenge(ChallengeRecord record);

    // Should only ever be subclassed by the generic type!
    internal ChallengeLoader()
    {
    }
}
