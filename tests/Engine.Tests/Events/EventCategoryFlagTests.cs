using Engine.Core.Input;
using Engine.Events;
using Engine.Events.Input;
using Shouldly;

namespace Engine.Tests.Events;

public class EventCategoryFlagTests
{
    [Fact]
    public void EventCategory_Flags_ShouldCombineCorrectly()
    {
        // Arrange
        var combined = EventCategory.EventCategoryKeyboard | EventCategory.EventCategoryInput;

        // Act & Assert
        combined.HasFlag(EventCategory.EventCategoryKeyboard).ShouldBeTrue();
        combined.HasFlag(EventCategory.EventCategoryInput).ShouldBeTrue();
        combined.HasFlag(EventCategory.EventCategoryMouse).ShouldBeFalse();
    }

    [Fact]
    public void EventCategory_MultipleCategories_ShouldBeDetectable()
    {
        // Arrange
        var @event = new KeyPressedEvent(KeyCodes.A, false);

        // Act & Assert - KeyEvent should be in both Keyboard and Input categories
        @event.IsInCategory(EventCategory.EventCategoryKeyboard).ShouldBeTrue();
        @event.IsInCategory(EventCategory.EventCategoryInput).ShouldBeTrue();
        @event.IsInCategory(EventCategory.EventCategoryMouse).ShouldBeFalse();
        @event.IsInCategory(EventCategory.EventCategoryApplication).ShouldBeFalse();
    }

    [Fact]
    public void MouseEvent_MultipleCategories_ShouldBeDetectable()
    {
        // Arrange
        var @event = new MouseMovedEvent(0, 0);

        // Act & Assert - MouseEvent should be in both Mouse and Input categories
        @event.IsInCategory(EventCategory.EventCategoryMouse).ShouldBeTrue();
        @event.IsInCategory(EventCategory.EventCategoryInput).ShouldBeTrue();
        @event.IsInCategory(EventCategory.EventCategoryKeyboard).ShouldBeFalse();
        @event.IsInCategory(EventCategory.EventCategoryApplication).ShouldBeFalse();
    }
}