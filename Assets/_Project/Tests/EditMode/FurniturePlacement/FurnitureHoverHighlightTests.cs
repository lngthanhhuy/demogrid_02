using NUnit.Framework;
using SenCity.Features.FurniturePlacement;

namespace SenCity.Tests.FurniturePlacement
{
    public sealed class FurnitureHoverHighlightTests
    {
        private FurniturePlacementTestFactory factory;

        [SetUp]
        public void SetUp()
        {
            factory = new FurniturePlacementTestFactory();
        }

        [TearDown]
        public void TearDown()
        {
            factory.DestroyCreatedObjects();
        }

        [Test]
        public void PlacedFurnitureTracksHoverAndSelectionIndependently()
        {
            PlacedFurnitureObject placedObject = factory.AddComponent<PlacedFurnitureObject>();

            placedObject.SetHovered(true);
            placedObject.SetSelected(true);
            placedObject.SetHovered(false);

            Assert.That(placedObject.IsHovered, Is.False);
            Assert.That(placedObject.IsSelected, Is.True);
        }

        [Test]
        public void RuntimeHoverObjectClearsPreviousHover()
        {
            FurniturePlacementRuntime runtime = factory.AddComponent<FurniturePlacementRuntime>();
            PlacedFurnitureObject firstObject = factory.AddComponent<PlacedFurnitureObject>();
            PlacedFurnitureObject secondObject = factory.AddComponent<PlacedFurnitureObject>();

            runtime.HoverObject(firstObject);
            runtime.HoverObject(secondObject);

            Assert.That(firstObject.IsHovered, Is.False);
            Assert.That(secondObject.IsHovered, Is.True);
            Assert.That(runtime.HoveredObject, Is.SameAs(secondObject));
        }
    }
}
