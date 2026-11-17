using Ducky.Sdk.Facade;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>The pure parts of the game facade: <see cref="ExplosionRequest{TCharacter}"/> and <see cref="WorldPoint"/>.</summary>
public class ExplosionRequestTests
{
    private sealed class Character
    {
    }

    [Test]
    public void Blast_IsARealEffectExplosionThatIgnoresDifficulty()
    {
        var source = new Character();

        var request = ExplosionRequest<Character>.Blast(source, 40f, canHurtSelf: true);

        request.Source.ShouldBeSameAs(source);
        request.Damage.ShouldBe(40f);
        request.IsExplosion.ShouldBeTrue();
        request.RealDamage.ShouldBeTrue();
        request.FromBuffOrEffect.ShouldBeTrue();
        request.IgnoreDifficulty.ShouldBeTrue();
        request.Fx.ShouldBe(ExplosionFx.Normal);
        request.ShakeStrength.ShouldBe(1f);
        request.CanHurtSelf.ShouldBeTrue();
        request.Buff.ShouldBeNull();
    }

    [Test]
    public void Blast_WithoutASource_HasNoSource()
    {
        ExplosionRequest<Character>.Blast(null, 10f, canHurtSelf: false).Source.ShouldBeNull();
    }

    [Test]
    public void ARequestBuiltByHand_KeepsTheGameDefaultsForWhatItDoesNotSet()
    {
        var request = new ExplosionRequest<Character> { Damage = 5f };

        request.IsExplosion.ShouldBeFalse();
        request.RealDamage.ShouldBeFalse();
        request.FromBuffOrEffect.ShouldBeFalse();
        request.Fx.ShouldBe(ExplosionFx.Normal);
        request.ShakeStrength.ShouldBe(1f);
        request.CanHurtSelf.ShouldBeFalse();
    }

    [Test]
    public void WorldPoint_IsEqualByItsThreeFloats()
    {
        new WorldPoint(1f, 2f, 3f).ShouldBe(new WorldPoint(1f, 2f, 3f));
        new WorldPoint(1f, 2f, 3f).ShouldNotBe(new WorldPoint(1f, 2f, 4f));
    }
}
