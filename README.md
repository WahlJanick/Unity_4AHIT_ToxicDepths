# Unity_4AHIT_ToxicDepths

Zum Starten `Assets/Scenes/MainMenu.unity` in Unity oeffnen und Play starten.
Im Menue laedt **Play > New Game** die `SampleScene`. **Continue** und
**Load Game** bleiben bis zur Einbindung eines Speichersystems deaktiviert.

Die MainMenu-Dateien liegen hier:

- Szene: `Assets/Scenes/MainMenu.unity`
- Zugehoerige Lichtdaten: `Assets/Scenes/MainMenu/`
- Menue-Prefab: `Assets/Prefabs/UI/MainMenu.prefab`
- Farben: `Assets/Settings/MainMenuTheme.asset`
- Beleuchtungseinstellungen: `Assets/Settings/MainMenuLighting.lighting`

Das Menue verwendet weiterhin Skripte, Grafiken und Animationen aus `Assets/SlimUI/`.

Die vertikalen Anzeigen `Assets/Prefabs/UI/StaminaBar.prefab` (hellblau) und
`Assets/Prefabs/UI/EnergyBar.prefab` (gruen) sind in `SampleScene` eingebunden.
Sie liegen als Screen Space Overlay links unten mit 36 Pixeln Abstand zum
unteren Rand bei einer Referenzaufloesung von 1920 x 1080. Die StaminaBar beginnt
bei x=36, die EnergyBar daneben bei x=100. Beide verwenden die Grafiken und
Skripte aus dem importierten Progress-Bar-Paket.

Am Wurzelobjekt **StaminaBar** laesst sich unter **Stamina Bar > Fill Color**
die hellblaue Farbe aendern. Entsprechend laesst sich **EnergyBar** unter
**Energy Bar > Fill Color** anpassen. **Normalized Value** bestimmt den Fuellstand:
0 = leer, 0.5 = halbvoll, 1 = voll. Der Startwert ist 1.
Die Spielersteuerung kann die Anzeige spaeter ueber
`ToxicDepths.UI.StaminaBar.SetStamina(current, maximum)` und
`ToxicDepths.UI.EnergyBar.SetEnergy(current, maximum)` unabhaengig aktualisieren;
Verbrauch und Regeneration sind noch nicht implementiert.

Die Hotbar liegt unter `Assets/Prefabs/UI/Hotbar.prefab` und ist ebenfalls in
`SampleScene` eingebunden. Ihre vier Slots liegen bei x=168, y=36 rechts neben
den beiden Balken. Mit **1-4** oder dem **Mausrad** laesst sich der aktive Slot wechseln;
das funktioniert waehrend der Ego-Steuerung, bei gesperrtem Mauszeiger.

Am Wurzelobjekt **Hotbar** im **Hotbar Controller** unter **Slots** koennen
Item-Icons (Sprite) und Mengen eingestellt werden. Ohne Icon bleibt ein Slot
leer; Mengen werden erst ab 2 angezeigt. Die Auswahl wird gruen hervorgehoben.
Ein Inventar oder ausgeruestete Werkzeuge sind noch nicht angebunden.
Das Ereignis **On Selection Changed** liefert dafuer den gewaehlten Index 0-3.
Aus Code koennen die Slots ueber `HotbarController.SetSlot(index, icon, count)`
aktualisiert und mit `SelectSlot(index)` ausgewaehlt werden.
