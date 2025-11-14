using System.Runtime.CompilerServices;

namespace Ducky.Sdk.GameApis;

public static class DamageInfoExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DamageInfoJudgement To(this DamageInfo info, Health to)
    {
        return new DamageInfoJudgement(info, to);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFromMainToEnemy(this DamageInfoJudgement judgement)
    {
        var from = judgement.Info.fromCharacter;
        if (from == null || !from.IsMainCharacter)
        {
            return false;
        }

        if (judgement.To == null)
        {
            return false;
        }

        var toCharacter = judgement.To.TryGetCharacter();
        return toCharacter != null && !toCharacter.IsMainCharacter && toCharacter.Team != from.Team;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFromEnemyToMain(this DamageInfoJudgement judgement)
    {
        var from = judgement.Info.fromCharacter;
        if (from == null || from.IsMainCharacter)
        {
            return false;
        }

        if (judgement.To == null)
        {
            return false;
        }

        var toCharacter = judgement.To.TryGetCharacter();
        return toCharacter != null && toCharacter.IsMainCharacter && toCharacter.Team != from.Team;
    }
}

public readonly struct DamageInfoJudgement
{
    public readonly DamageInfo Info;
    public readonly Health To;

    public DamageInfoJudgement(DamageInfo info, Health to)
    {
        Info = info;
        To = to;
    }
}
