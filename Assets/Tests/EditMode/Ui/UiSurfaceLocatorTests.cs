#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using EmberCrpg.Ui.Foundation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Ui
{
    public sealed class UiSurfaceLocatorTests
    {
        [SetUp]
        public void SetUp() => UiSurfaceLocator.Clear();

        [TearDown]
        public void TearDown() => UiSurfaceLocator.Clear();

        [Test]
        public void RegisterThenCurrentReturnsSurface()
        {
            var surface = new FakeSurface();
            UiSurfaceLocator.Register(surface);
            Assert.That(UiSurfaceLocator.Current, Is.SameAs(surface));
        }

        [Test]
        public void RegisterTwiceWithoutClearThrows()
        {
            UiSurfaceLocator.Register(new FakeSurface());
            Assert.Throws<InvalidOperationException>(() => UiSurfaceLocator.Register(new FakeSurface()));
        }

        [Test]
        public void ClearSetsCurrentToNull()
        {
            UiSurfaceLocator.Register(new FakeSurface());
            UiSurfaceLocator.Clear();
            Assert.That(UiSurfaceLocator.Current, Is.Null);
        }

        [Test]
        public void RegisterClearRaceDoesNotThrowUnexpectedExceptions()
        {
            Assert.DoesNotThrow(() => Parallel.For(0, 100, i =>
            {
                try
                {
                    if ((i & 1) == 0) UiSurfaceLocator.Register(new FakeSurface());
                    else UiSurfaceLocator.Clear();
                }
                catch (InvalidOperationException)
                {
                    // Double-register is the expected guard under races.
                }
            }));
        }

        private sealed class FakeSurface : IUiSurface
        {
            public UiTokens Tokens { get; } = null;
            public IUiPanel Mount(string panelId) => throw new NotImplementedException();
            public void Unmount(IUiPanel panel) { }
            public void Clear() { }
            public void Dispose() { }
        }
    }
}
#endif
