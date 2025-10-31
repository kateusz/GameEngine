# Viewport Rulers - System Linijek w Edytorze

## Przegląd

System linijek (rulers) w viewporcie edytora zapewnia wizualne odniesienie do współrzędnych świata gry, podobnie jak w edytorze Godot dla widoków 2D. Pomaga to w precyzyjnym pozycjonowaniu obiektów i nawigacji po scenie.

## Funkcje

- **Pozioma linijka** - wyświetlana na górze viewportu
- **Pionowa linijka** - wyświetlana po lewej stronie viewportu
- **Automatyczne skalowanie** - znaczniki dostosowują się do poziomu zbliżenia kamery
- **Etykiety współrzędnych** - główne znaczniki pokazują wartości współrzędnych świata
- **Toggle włącz/wyłącz** - możliwość ukrycia linijek przez menu View

## Użycie

### Włączanie/Wyłączanie

Linijki można włączać i wyłączać poprzez menu górne:

```
View → Show Rulers
```

Domyślnie linijki są włączone.

### Znaczniki

- **Główne znaczniki** (większe, z etykietami) - występują co 10 jednostek (dostosowane do zoomu)
- **Pomocnicze znaczniki** (mniejsze) - występują między głównymi znacznikami

### Automatyczne Skalowanie

System automatycznie dostosowuje rozstaw znaczników w zależności od poziomu zbliżenia:

- Przy dużym zbliżeniu: gęstsze znaczniki (np. co 1 jednostkę)
- Przy dużym oddaleniu: rzadsze znaczniki (np. co 100 jednostek)

Algorytm zapewnia, że znaczniki są zawsze czytelne i użyteczne, niezależnie od poziomu zbliżenia.

## Implementacja

### Klasa ViewportRuler

Główna klasa: `Editor.UI.ViewportRuler`

```csharp
public class ViewportRuler
{
    public bool Enabled { get; set; }
    
    public void Render(Vector2 viewportMin, Vector2 viewportMax, 
                      Vector2 cameraPosition, float zoom = 1.0f)
}
```

### Parametry Renderowania

- `viewportMin` - lewy górny róg viewportu w przestrzeni ekranu
- `viewportMax` - prawy dolny róg viewportu w przestrzeni ekranu
- `cameraPosition` - aktualna pozycja kamery w przestrzeni świata (X, Y)
- `zoom` - poziom zbliżenia w pikselach na jednostkę świata

### Integracja z EditorLayer

Linijki są renderowane w metodzie `OnImGuiRender()` po narysowaniu obrazu z framebuffera:

```csharp
var cameraPos = new Vector2(_cameraController.Camera.Position.X, 
                           _cameraController.Camera.Position.Y);
var zoom = _viewportSize.Y / (_cameraController.Camera.OrthographicSize * 2.0f);
_viewportRuler.Render(_viewportBounds[0], _viewportBounds[1], cameraPos, zoom);
```

## Konfiguracja

### Stałe w ViewportRuler

Można dostosować wygląd linijek modyfikując następujące stałe:

```csharp
private const float RulerThickness = 20.0f;     // Grubość linijek
private const float MajorTickSize = 10.0f;      // Wysokość głównych znaczników
private const float MinorTickSize = 5.0f;       // Wysokość pomocniczych znaczników
private const float TextOffset = 2.0f;          // Odstęp tekstu od krawędzi
```

### Kolory

Kolory są definiowane jako właściwości z leniwą inicjalizacją (aby uniknąć wywoływania ImGui przed jego inicjalizacją):

```csharp
private uint BackgroundColor => ImGui.GetColorU32(...);   // Tło linijek
private uint LineColor => ImGui.GetColorU32(...);         // Linie obramowania
private uint TextColor => ImGui.GetColorU32(...);         // Tekst etykiet
private uint MajorTickColor => ImGui.GetColorU32(...);    // Główne znaczniki
private uint MinorTickColor => ImGui.GetColorU32(...);    // Pomocnicze znaczniki
```

**Uwaga:** Kolory używają leniwej inicjalizacji (computed properties) zamiast pól, aby ImGui było poprawnie zainicjalizowane przed ich użyciem.

## Wydajność

System jest zoptymalizowany pod kątem wydajności:

- Używa ImGui DrawList API dla wydajnego renderowania
- Rysuje tylko widoczne znaczniki w aktualnym zakresie viewportu
- Obliczenia są minimalne - tylko konwersje współrzędnych i formatowanie tekstu

## Przyszłe Rozszerzenia

Potencjalne ulepszenia:

1. **Własne jednostki miary** - możliwość wyświetlania w pixelach, metrach, itp.
2. **Kolorowe siatki** - różne kolory dla różnych zakresów
3. **Przeciąganie linijek** - tworzenie prowadnic (guides)
4. **Zapisywanie preferencji** - zapamiętywanie stanu włącz/wyłącz
5. **Obrócony tekst** - pionowy tekst dla linijki pionowej (wymaga custom renderingu)

## Znane Ograniczenia

- Tekst na linijce pionowej jest poziomy (ImGui nie wspiera natywnie obróconych tekstów)
- Współrzędne są zaokrąglane do liczb całkowitych w etykietach
- System jest dedykowany dla widoków 2D (ortograficznych)

## Rozwiązane Problemy

### SIGSEGV przy inicjalizacji (Fixed)

**Problem:** Crash przy starcie aplikacji z błędem `KERN_INVALID_ADDRESS` w `ImGui::GetColorU32()`.

**Przyczyna:** Kolory były inicjalizowane jako pola (`readonly uint`) w deklaracji klasy, ale ImGui nie było jeszcze zainicjalizowane w momencie konstruowania obiektu `ViewportRuler`.

**Rozwiązanie:** Zmiana z pól `readonly` na właściwości z leniwą inicjalizacją (computed properties):
```csharp
// Przed (błędne):
private readonly uint _backgroundColor = ImGui.GetColorU32(...);

// Po (poprawne):
private uint BackgroundColor => ImGui.GetColorU32(...);
```

To gwarantuje, że `ImGui.GetColorU32()` jest wywoływane dopiero w momencie renderowania, gdy ImGui jest już w pełni gotowe.

