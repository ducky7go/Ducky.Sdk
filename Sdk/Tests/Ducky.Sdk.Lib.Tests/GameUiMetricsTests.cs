using Ducky.Sdk.GameUi;

namespace Ducky.Sdk.Lib.Tests;

[TestFixture]
public class GameUiMetricsTests
{
    // Measured in the running game: the HUD canvas reported a scale factor of 1.125 at 2880x1800.
    [TestCase(2880f, 1800f, 1.125f)]
    [TestCase(2560f, 1440f, 1f)]
    [TestCase(3840f, 2160f, 1.5f)]
    [TestCase(1920f, 1080f, 0.75f)]
    public void The_game_scaler_follows_the_screen_width(float width, float height, float expected)
    {
        GameUiMetrics.ScaleFor(width, height).ShouldBe(expected, 0.0005f);
    }

    // The old Newbe.Dod canvas (1920x1080, match 0.5) measured 1.5811 at 2880x1800: about 1.405 times the game's scale.
    [Test]
    public void The_old_mod_canvas_was_larger_than_the_games()
    {
        var old = GameUiMetrics.ScaleFor(2880f, 1800f, 1920f, 1080f, 0.5f);
        old.ShouldBe(1.5811f, 0.0005f);
        (old / GameUiMetrics.ScaleFor(2880f, 1800f)).ShouldBe(1.405f, 0.001f);
    }

    [TestCase(0f, 1800f)]
    [TestCase(2880f, 0f)]
    [TestCase(-1f, -1f)]
    [TestCase(float.NaN, 1800f)]
    [TestCase(2880f, float.NaN)]
    public void A_screen_without_a_valid_size_leaves_the_scale_at_one(float width, float height)
    {
        GameUiMetrics.ScaleFor(width, height).ShouldBe(1f);
    }

    [Test]
    public void A_reference_without_a_valid_size_leaves_the_scale_at_one()
    {
        GameUiMetrics.ScaleFor(2880f, 1800f, 0f, 1080f).ShouldBe(1f);
        GameUiMetrics.ScaleFor(2880f, 1800f, 1920f, float.NaN).ShouldBe(1f);
    }

    [Test]
    public void A_match_below_zero_or_above_one_is_clamped()
    {
        GameUiMetrics.ScaleFor(2880f, 1800f, 1920f, 1080f, -1f)
            .ShouldBe(GameUiMetrics.ScaleFor(2880f, 1800f, 1920f, 1080f, 0f), 0.0001f);
        GameUiMetrics.ScaleFor(2880f, 1800f, 1920f, 1080f, 2f)
            .ShouldBe(GameUiMetrics.ScaleFor(2880f, 1800f, 1920f, 1080f, 1f), 0.0001f);
    }

    [Test]
    public void A_nan_match_is_treated_as_width()
    {
        GameUiMetrics.ScaleFor(2880f, 1800f, 1920f, 1080f, float.NaN)
            .ShouldBe(GameUiMetrics.ScaleFor(2880f, 1800f, 1920f, 1080f, 0f), 0.0001f);
    }

    [Test]
    public void The_type_scale_steps_down_from_display_to_micro()
    {
        float[] steps =
        {
            GameUiMetrics.Display, GameUiMetrics.Headline, GameUiMetrics.Title, GameUiMetrics.Body,
            GameUiMetrics.Caption, GameUiMetrics.Micro,
        };
        steps.ShouldBe(new float[] { 36f, 28f, 24f, 20f, 18f, 16f });
        for (var i = 1; i < steps.Length; i++)
        {
            steps[i].ShouldBeLessThan(steps[i - 1]);
        }

        // the game's smallest text is 16, nothing below it
        GameUiMetrics.Micro.ShouldBeGreaterThanOrEqualTo(16f);
    }

    [Test]
    public void The_radius_tiers_are_the_recommended_values_and_decrease()
    {
        float[] radii =
        {
            GameUiMetrics.RadiusPanel, GameUiMetrics.RadiusGroup, GameUiMetrics.RadiusControl, GameUiMetrics.RadiusTag,
        };
        radii.ShouldBe(new float[] { 16f, 11f, 8f, 6f });
        for (var i = 1; i < radii.Length; i++)
        {
            radii[i].ShouldBeLessThan(radii[i - 1]);
        }
    }

    [Test]
    public void The_reference_and_match_are_the_measured_game_values()
    {
        GameUiMetrics.ReferenceWidth.ShouldBe(2560f);
        GameUiMetrics.ReferenceHeight.ShouldBe(1440f);
        GameUiMetrics.MatchWidthOrHeight.ShouldBe(0f);
    }
}
