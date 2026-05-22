using VerseStrings.Core;
using Xunit;

namespace VerseStrings.Core.Tests;

public class InstallerResolveSourceRootTests : IDisposable
{
    private readonly string _root;

    public InstallerResolveSourceRootTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"verse-resolve-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void TopLevelUserCfg_ReturnsExtractRoot()
    {
        File.WriteAllText(Path.Combine(_root, "user.cfg"), "");

        Assert.Equal(_root, Installer.ResolveSourceRoot(_root));
    }

    [Fact]
    public void TopLevelDataDirectory_ReturnsExtractRoot()
    {
        Directory.CreateDirectory(Path.Combine(_root, "data"));

        Assert.Equal(_root, Installer.ResolveSourceRoot(_root));
    }

    [Fact]
    public void SingleWrapperSubdirectory_ReturnsWrapper()
    {
        // Why: some upstreams ship `pack-name/user.cfg` instead of `user.cfg`
        // at the zip root. The wrapper peel keeps the rest of the install
        // pipeline layout-agnostic.
        var wrapper = Path.Combine(_root, "wrapper-pack");
        Directory.CreateDirectory(wrapper);
        File.WriteAllText(Path.Combine(wrapper, "user.cfg"), "");

        Assert.Equal(wrapper, Installer.ResolveSourceRoot(_root));
    }

    [Fact]
    public void MultipleSubdirectoriesWithoutMarkers_ReturnsExtractRoot()
    {
        // Why: the wrapper-peel heuristic only fires when there's exactly one
        // subdirectory. With multiple, we can't tell which is the real one,
        // so the caller will end up applying ShouldInstall against the root.
        Directory.CreateDirectory(Path.Combine(_root, "a"));
        Directory.CreateDirectory(Path.Combine(_root, "b"));

        Assert.Equal(_root, Installer.ResolveSourceRoot(_root));
    }

    [Fact]
    public void EmptyDirectory_ReturnsExtractRoot()
    {
        Assert.Equal(_root, Installer.ResolveSourceRoot(_root));
    }
}
