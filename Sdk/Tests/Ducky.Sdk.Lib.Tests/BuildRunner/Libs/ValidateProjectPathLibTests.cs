namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>Task 5.1: ValidateProjectPathLib accepts only an existing, rooted, well-formed directory.</summary>
public class ValidateProjectPathLibTests
{
    private string _root = null!;
    private string _projectDir = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("project-path");
        _projectDir = Path.Combine(_root, "Ducky Project With Spaces");
        Directory.CreateDirectory(_projectDir);
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    [Test]
    public void Execute_AcceptsExistingAbsoluteDirectoryWithSpaces()
    {
        var context = LibTestSupport.NewContext(_projectDir);

        var (exit, output, error) = LibTestSupport.Capture(() => ValidateProjectPathLib.Execute(context));

        exit.ShouldBe(0);
        output.ShouldContain("Valid: True");
        output.ShouldContain("Project path validation passed");
        error.ShouldBeEmpty();
    }

    [Test]
    public void Execute_RejectsMissingDirectory()
    {
        var missing = Path.Combine(_root, "missing folder");
        var context = LibTestSupport.NewContext(missing);

        var (exit, output, error) = LibTestSupport.Capture(() => ValidateProjectPathLib.Execute(context));

        exit.ShouldBe(1);
        output.ShouldContain("Valid: False");
        output.ShouldContain("Project directory does not exist");
        error.ShouldContain("Project path validation failed");
    }

    [Test]
    public void Execute_RejectsRelativePathEvenWhenItExistsFromCurrentDirectory()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_root);
            var relative = "relative project";
            Directory.CreateDirectory(Path.Combine(_root, relative));
            var context = LibTestSupport.NewContext(relative);

            var (exit, output, _) = LibTestSupport.Capture(() => ValidateProjectPathLib.Execute(context));

            exit.ShouldBe(1);
            output.ShouldContain("Path is not absolute");
            output.ShouldNotContain("Project directory does not exist");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }

    [Test]
    public void Execute_RejectsParentDirectorySegment()
    {
        var withParent = _projectDir + "/../" + Path.GetFileName(_projectDir);
        var context = LibTestSupport.NewContext(withParent);

        var (exit, output, _) = LibTestSupport.Capture(() => ValidateProjectPathLib.Execute(context));

        exit.ShouldBe(1);
        output.ShouldContain("Path format is invalid");
    }

    [Test]
    public void Execute_RejectsDoubleSlashInPath()
    {
        var context = LibTestSupport.NewContext(_projectDir + "//");

        var (exit, output, _) = LibTestSupport.Capture(() => ValidateProjectPathLib.Execute(context));

        exit.ShouldBe(1);
        output.ShouldContain("Path format is invalid");
    }

    [Test]
    public void Execute_RejectsEmptyPath()
    {
        var context = LibTestSupport.NewContext(string.Empty);

        var (exit, output, _) = LibTestSupport.Capture(() => ValidateProjectPathLib.Execute(context));

        exit.ShouldBe(1);
        output.ShouldContain("Valid: False");
    }
}
