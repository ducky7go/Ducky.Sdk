using System;
using Ducky.Sdk.GameApis;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// <see cref="BulletCountAccessor"/> against stand-in types with a private property, a private field, a read-only
/// property and no member. The game's <c>ItemSetting_Gun</c> cannot be built outside the game.
/// </summary>
public class GunBulletCountTests
{
    private sealed class PropertyGun
    {
        private int _value;

        private int bulletCount
        {
            get => _value;
            set => _value = value;
        }
    }

    private sealed class FieldGun
    {
        private int bulletCount;
    }

    private sealed class ReadOnlyGun
    {
        private int bulletCount => 5;
    }

    private sealed class NoBulletCountGun
    {
        public int Ammo { get; set; }
    }

    [Test]
    public void PrivateProperty_IsReadAndWritten()
    {
        var accessor = BulletCountAccessor.Create(typeof(PropertyGun));
        var gun = new PropertyGun();

        accessor.ShouldNotBeNull();
        accessor!.CanSet.ShouldBeTrue();
        accessor.TrySet(gun, 17).ShouldBeTrue();
        accessor.Get(gun).ShouldBe(17);
    }

    [Test]
    public void PrivateField_IsReadAndWritten()
    {
        var accessor = BulletCountAccessor.Create(typeof(FieldGun));
        var gun = new FieldGun();

        accessor.ShouldNotBeNull();
        accessor!.TrySet(gun, 9).ShouldBeTrue();
        accessor.Get(gun).ShouldBe(9);
    }

    [Test]
    public void ReadOnlyProperty_ReadsButDoesNotWrite()
    {
        var accessor = BulletCountAccessor.Create(typeof(ReadOnlyGun));

        accessor.ShouldNotBeNull();
        accessor!.CanSet.ShouldBeFalse();
        accessor.TrySet(new ReadOnlyGun(), 1).ShouldBeFalse();
        accessor.Get(new ReadOnlyGun()).ShouldBe(5);
    }

    [Test]
    public void NoMember_ResolvesToNull()
    {
        BulletCountAccessor.Create(typeof(NoBulletCountGun)).ShouldBeNull();
    }

    [Test]
    public void MemberName_IsTheGameMemberName()
    {
        BulletCountAccessor.MemberName.ShouldBe("bulletCount");
    }
}
