namespace Engine.Events;

[Flags]
public enum EventCategory
{
    None = 0,
    EventCategoryApplication = 1,
    EventCategoryInput = 1 << 1, 
    EventCategoryKeyboard = 1 << 2,
    EventCategoryMouse = 1 << 3,
}