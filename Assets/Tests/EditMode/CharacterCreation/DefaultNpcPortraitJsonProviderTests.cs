using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Presentation.Ember.CharacterCreation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.CharacterCreation
{
    public sealed class DefaultNpcPortraitJsonProviderTests
    {
        [Test]
        public async Task RequestAsync_CanceledToken_ReturnsEmptyWithoutThrowing()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                var result = await DefaultNpcPortraitJsonProvider.RequestAsync(42u, string.Empty, cts.Token);
                Assert.That(result, Is.EqualTo(string.Empty));
            }
        }
    }
}
