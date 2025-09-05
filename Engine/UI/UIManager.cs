using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Engine.Renderer;

namespace Engine.UI;

public class UIManager
{
    private readonly List<UIElement> _elements;
    private readonly List<UIElement> _elementsToAdd;
    private readonly List<UIElement> _elementsToRemove;
    private readonly UIRenderer _uiRenderer;
    private bool _isUpdating = false;
    
    public UIManager(IGraphics2D graphics2D)
    {
        _elements = new List<UIElement>();
        _elementsToAdd = new List<UIElement>();
        _elementsToRemove = new List<UIElement>();
        _uiRenderer = new UIRenderer(graphics2D);
    }
    
    public void Update(float deltaTime)
    {
        _isUpdating = true;
        
        // Update all visible elements
        foreach (var element in _elements.Where(e => e.Visible))
        {
            element.Update(deltaTime);
        }
        
        _isUpdating = false;
        
        // Process pending additions and removals
        ProcessPendingChanges();
    }
    
    public void Render()
    {
        // Sort elements by ZOrder for proper rendering order
        var sortedElements = _elements
            .Where(e => e.Visible)
            .OrderBy(e => e.ZOrder)
            .ToList();
        
        _uiRenderer.BeginUIPass();
        
        foreach (var element in sortedElements)
        {
            _uiRenderer.RenderElement(element);
        }
        
        _uiRenderer.EndUIPass();
    }
    
    public void AddElement(UIElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        
        if (_isUpdating)
        {
            _elementsToAdd.Add(element);
        }
        else
        {
            _elements.Add(element);
        }
    }
    
    public bool RemoveElement(UIElement element)
    {
        if (element == null)
            return false;
        
        if (_isUpdating)
        {
            _elementsToRemove.Add(element);
            return true;
        }
        else
        {
            return _elements.Remove(element);
        }
    }
    
    public bool RemoveElement(string id)
    {
        var element = GetElement(id);
        return element != null && RemoveElement(element);
    }
    
    public UIElement? GetElement(string id)
    {
        return _elements.FirstOrDefault(e => e.Id == id);
    }
    
    public T? GetElement<T>(string id) where T : UIElement
    {
        return _elements.OfType<T>().FirstOrDefault(e => e.Id == id);
    }
    
    public UIElement? GetElementAt(Vector2 screenPosition)
    {
        // Check elements in reverse render order (top elements first)
        var sortedElements = _elements
            .Where(e => e.Visible && e.Interactive)
            .OrderByDescending(e => e.ZOrder)
            .ToList();
        
        foreach (var element in sortedElements)
        {
            if (element.ContainsPoint(screenPosition))
            {
                return element;
            }
        }
        
        return null;
    }
    
    public void Clear()
    {
        if (_isUpdating)
        {
            _elementsToRemove.AddRange(_elements);
        }
        else
        {
            _elements.Clear();
        }
    }
    
    public bool HandleMouseClick(Vector2 screenSpaceMousePosition)
    {
        var normalizedPosition = _uiRenderer.ScreenSpaceToNormalized(screenSpaceMousePosition);
        var element = GetElementAt(normalizedPosition);
        if (element != null)
        {
            element.OnMouseClick(normalizedPosition);
            return true; // Input consumed
        }
        return false; // Input not consumed
    }
    
    public bool HandleMouseMove(Vector2 screenSpaceMousePosition)
    {
        var normalizedPosition = _uiRenderer.ScreenSpaceToNormalized(screenSpaceMousePosition);
        var hoveredElement = GetElementAt(normalizedPosition);
        bool inputConsumed = false;
        
        foreach (var element in _elements.Where(e => e.Visible && e.Interactive))
        {
            if (element == hoveredElement)
            {
                element.OnMouseHover(normalizedPosition);
                inputConsumed = true;
            }
        }
        
        return inputConsumed;
    }
    
    public void SetElementZOrder(UIElement element, int zOrder)
    {
        if (_elements.Contains(element))
        {
            element.ZOrder = zOrder;
        }
    }
    
    public void BringToFront(UIElement element)
    {
        if (_elements.Contains(element))
        {
            var maxZOrder = _elements.Max(e => e.ZOrder);
            element.ZOrder = maxZOrder + 1;
        }
    }
    
    public void SendToBack(UIElement element)
    {
        if (_elements.Contains(element))
        {
            var minZOrder = _elements.Min(e => e.ZOrder);
            element.ZOrder = minZOrder - 1;
        }
    }
    
    public void SetScreenSize(Vector2 screenSize)
    {
        _uiRenderer.SetScreenSize(screenSize);
    }
    
    public Vector2 GetScreenSize()
    {
        return _uiRenderer.ScreenSize;
    }
    
    public IReadOnlyList<UIElement> Elements => _elements.AsReadOnly();
    
    public int ElementCount => _elements.Count;
    
    private void ProcessPendingChanges()
    {
        // Add pending elements
        foreach (var element in _elementsToAdd)
        {
            _elements.Add(element);
        }
        _elementsToAdd.Clear();
        
        // Remove pending elements
        foreach (var element in _elementsToRemove)
        {
            _elements.Remove(element);
        }
        _elementsToRemove.Clear();
    }
    
    public void DebugPrint()
    {
        Console.WriteLine($"UIManager: {_elements.Count} elements");
        foreach (var element in _elements)
        {
            Console.WriteLine($"  - {element.GetType().Name} '{element.Id}' at ({element.Position.X}, {element.Position.Y}) size ({element.Size.X}, {element.Size.Y}) Z:{element.ZOrder}");
        }
    }
}