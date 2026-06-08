#if UNITY_EDITOR
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.Bootstrap;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation
{
    public sealed class EmberWorldHostAdapterBindingTests
    {
        [Test]
        public void FromAggregate_ExposesEveryNarrowRole()
        {
            var adapter = new UnavailableSimulationAdapter();

            var binding = EmberWorldHostAdapterBinding.From(adapter);

            Assert.That(binding.Adapter, Is.SameAs(adapter));
            Assert.That(binding.Clock, Is.SameAs(adapter));
            Assert.That(binding.Hud, Is.SameAs(adapter));
            Assert.That(binding.WorldView, Is.SameAs(adapter));
            Assert.That(binding.Commands, Is.SameAs(adapter));
            Assert.That(binding.Oracle, Is.SameAs(adapter));
        }

        [Test]
        public void Create_UsesFallbackWhenCandidateIsNull()
        {
            var fallback = new UnavailableSimulationAdapter();
            var called = false;

            var binding = EmberWorldHostAdapterBinding.Create(null, () =>
            {
                called = true;
                return fallback;
            });

            Assert.That(called, Is.True);
            Assert.That(binding.Adapter, Is.SameAs(fallback));
            Assert.That(binding.Commands, Is.SameAs(fallback));
        }
    }
}
#endif
