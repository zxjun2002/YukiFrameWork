using System;
using System.Collections.Generic;
using MIKUFramework.IOC;
using NUnit.Framework;
using UnityEngine;
using Yuki.Editor.HotUpdate;
using Yuki.HotUpdate;

public sealed class HotUpdateTests
{
    private static HotUpdateManifest Manifest() => new HotUpdateManifest
    {
        buildTarget = "StandaloneWindows64",
        hotUpdateAssemblies = new[] { "Config.dll", "Runtime.dll" },
        aotAssemblies = new[] { "mscorlib.dll", "Main.dll" }
    };

    [Test]
    public void AcceptsHotAssembliesAndMainMetadata()
    {
        Assert.DoesNotThrow(() => HotUpdateLoader.ValidateManifest(Manifest()));
    }

    [TestCase("Config.dll")]
    [TestCase("config.dll")]
    [TestCase("../Other.dll")]
    [TestCase("Other")]
    public void RejectsDuplicateOrInvalidAssemblyNames(string name)
    {
        var manifest = Manifest();
        manifest.aotAssemblies = new[] { name };
        Assert.Throws<InvalidOperationException>(() => HotUpdateLoader.ValidateManifest(manifest));
    }

    [Test]
    public void RejectsMainAsHotAssembly()
    {
        var manifest = Manifest();
        manifest.hotUpdateAssemblies = new[] { "Main.dll" };
        manifest.aotAssemblies = Array.Empty<string>();
        Assert.Throws<InvalidOperationException>(() => HotUpdateLoader.ValidateManifest(manifest));
    }

    [Test]
    public void RejectsMissingManifest()
    {
        Assert.Throws<InvalidOperationException>(() => HotUpdateLoader.ValidateManifest(null));
    }

    [Test]
    public void FrameworkDoesNotReferenceHotAssemblies()
    {
        Assert.DoesNotThrow(HotUpdateBuild.ValidateAssemblyBoundary);
    }

    [Test]
    public void IocCanBeScopedToMainWithoutHotBusiness()
    {
        using (var container = new MIKUIoC(assemblies: new[] { typeof(IoCHelper).Assembly }))
        {
            Assert.That(container.GetBean<IEventCenter>(), Is.InstanceOf<EventCenter>());
            Assert.Throws<KeyNotFoundException>(() => container.GetBean<IConfigTable>());
        }
    }

    [Test]
    public void IocReportsMissingFrameworkDependenciesInIsolatedBusiness()
    {
        Assert.Throws<Exception>(() => new MIKUIoC(assemblies: new[] { typeof(GameSettingMgr).Assembly }));
    }

    [Test]
    public void HotInitializersRespectPhaseOrderAndSceneBoundary()
    {
        InitializationProbe.Calls.Clear();
        HotfixInitialization.Invoke(new[] { GetType().Assembly }, false);
        CollectionAssert.AreEqual(new[] { "subsystem", "assemblies", "before-early", "before-late" }, InitializationProbe.Calls);
        HotfixInitialization.Invoke(new[] { GetType().Assembly }, true);
        Assert.That(InitializationProbe.Calls[4], Is.EqualTo("after"));
    }
}

internal static class InitializationProbe
{
    internal static readonly List<string> Calls = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void After() => Calls.Add("after");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad), HotfixInitOrder(100)]
    private static void BeforeLate() => Calls.Add("before-late");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad), HotfixInitOrder(-10)]
    private static void BeforeEarly() => Calls.Add("before-early");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration), HotfixInitOrder(1000)]
    private static void Subsystem() => Calls.Add("subsystem");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Assemblies() => Calls.Add("assemblies");
}

