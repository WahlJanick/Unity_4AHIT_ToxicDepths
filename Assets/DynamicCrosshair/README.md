# Dynamic Crosshair

Ein Fadenkreuz aus vier weissen Strichen, dunkler Kontur und Mittelpunkt.
Es wird direkt als Unity-UI gezeichnet und braucht keine Texturen oder Fonts.

## Einsetzen und ausprobieren

1. `Prefabs/DynamicCrosshair.prefab` in die Spielszene ziehen.
   Das Prefab besitzt einen eigenen Screen Space Overlay Canvas.
2. Am Kindobjekt `Crosshair` Farbe, Gap, Line Length und Strike Scale einstellen.
   Die Groessen beziehen sich auf 1920 x 1080; der Canvas skaliert mit dem Bildschirm.
3. Im Play-Modus im Kontextmenue der Komponente **Dynamic Crosshair**
   `Schlagvorschau (im Play-Modus)` waehlen.

Das UI blockiert keine Mausklicks und reagiert nicht selbst auf Eingaben.
Es gibt bisher keine Pickaxe-/Ore-Steuerung im Projekt. Das Prefab ist deshalb
noch nicht automatisch mit einem Abbauvorgang verbunden.

## Exakt mit der Schlaganimation synchronisieren

1. Auf dem GameObject mit dem Pickaxe-Animator `PickaxeCrosshairLink` hinzufuegen.
2. Dessen Feld **Crosshair** mit dem Crosshair aus der Szene belegen.
3. Im Animator den Schlag-State auswaehlen und unter **Add Behaviour**
   `CrosshairSwingState` hinzufuegen. Nicht an Idle oder Laufanimationen haengen.
4. Am Crosshair **Impact Time** auf die relative Trefferzeit des Clips setzen.
   Beispiel: Treffer bei 0,3 Sekunden in einem 0,8-Sekunden-Clip = 0,375.

Das Fadenkreuz zieht sich vom Anfang des Clips bis zum Treffer zusammen und
oeffnet sich danach wieder. Animator-Speed und Pausen werden ueber den echten
Animationsfortschritt beruecksichtigt. Beim Verlassen des States wird es zurueckgesetzt.
Bei einer geloopten Schlaganimation wiederholt sich der Effekt pro Durchlauf.
Bei Self-Transitions am selben Schlag-State **Can Transition To Self** ausschalten;
fuer wiederholtes Schlagen stattdessen loopen oder ueber einen anderen State wechseln.

Der Abbau-Controller entscheidet, wann der Schlag-State startet, z.B. nur wenn ein
Ore in Reichweite ist. Dieses Asset fuehrt keine Treffer- oder Schadensberechnung aus.

## Alternative: Aufruf aus eigenem Code

```csharp
using ToxicDepths.UI;

// Beim Start eines Schlags mit bekannter Dauer:
crosshair.PlaySwing(0.8f);

// Oder pro Frame den eigenen Animationsfortschritt liefern:
crosshair.SetSwingProgress(normalizedSwingTime);

// Bei Abbruch, Werkzeugwechsel oder Oeffnen des Pausemenues:
crosshair.ResetCrosshair();
```

Die zeitgesteuerte Variante verwendet `Time.deltaTime` und pausiert bei
`Time.timeScale = 0`. Fuer eine vom TimeScale unabhaengige Animator-Animation die
State-Anbindung verwenden. Fuer Animation Events bietet `PickaxeCrosshairLink`
die Methoden `PlaySwing()` und `CancelSwing()`; nicht gleichzeitig mit der
State-Anbindung auf demselben Clip verwenden.
