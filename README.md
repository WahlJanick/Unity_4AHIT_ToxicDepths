# Toxic Depths

## Erz abbauen

In `Assets/Scenes/SampleScene.unity` steht das Smaragd-Erz als abbaubares Vorkommen.
Im Play-Modus mit dem Dynamic Crosshair darauf zielen und die linke Maustaste
halten. Die Spitzhacke und die Hand holen aus, schlagen zu und kehren zur
Ausgangsposition zurueck. Treffer und die Verengung des Fadenkreuzes sind synchron.
Die Figur startet mit Blick auf das Erz.

- Reichweite: 3 Meter; Waende blockieren den Treffer.
- Ein Schlag dauert 0,7 Sekunden und kostet 12 Ausdauer sowie mit den aktuellen
  PlayerVitals-Einstellungen 0,6 Energie. Auch ein Schlag ins Leere kostet Kraft.
- Vier Treffer lösen das ganze Erzstück in Originalgröße. Es fällt zu Boden.
- Mit **E** hebst du ein anvisiertes Erzstück auf. Beim Tragen mit beiden Händen
  ist Sprinten gesperrt. **Q** loslassen wirft das Erz in Blickrichtung.
  Je länger du **Q** hältst, desto weiter fliegt es. Nach 1,25 Sekunden ist
  die volle Wurfkraft erreicht. Das Erz lässt sich danach wieder aufheben.
- Bei zu wenig Ausdauer oder Energie startet kein Schlag. Die bestehende
  Ausdauererholung und der Energieeinfluss auf die maximale Ausdauer bleiben aktiv.
- Escape gibt den Mauszeiger frei und bricht einen laufenden Schlag ab. Der
  bereits begonnene Schlag wird nicht erstattet. Mit einem Klick zur Steuerung
  zurueckkehren und danach erneut zum Abbauen klicken.

Kosten, Reichweite und Schlagbewegung sind im `PlayerMining` am Player einstellbar.
Trefferanzahl und Erzname stehen im `OreDeposit` am Erz. Den Trefferzeitpunkt gibt
das vorhandene `DynamicCrosshair` vor. Weitere platzierte Smaragdmodelle lassen
sich ueber **Tools > Toxic Depths > Abbauen in aktueller Szene einrichten** verbinden.
Die Ressourcenregeln prueft `pwsh -NoProfile -File Tests/Test-MiningVitals.ps1`.

## Zerschredderer platzieren

Zu Spielbeginn liegt ein Zerschredderer in Hotbar-Slot **1**. Wähle den Slot,
ziele auf den Boden und halte die rechte Maustaste für die Vorschau. Lass bei
grüner Vorschau los, um die Maschine zu platzieren. Dabei wird der Slot geleert.

Halte ein Erzstück, visiere die Maschine an und lass **Q** los, um das Erz in
den Trichter zu werfen. Beim Anvisieren siehst du den Fortschritt des aktuellen
Durchgangs. Aus der unteren Öffnung kommen ein Smaragd und zwei Steine.

Smaragde und Steine hebst du mit **E** auf. Smaragde sammeln sich in Slot **4**,
Steine in Slot **5**. Zu Beginn liegen sechs Smaragde in Slot 4.
Öffne den Shop mit **B** und kaufe mit **2** die schnelle Variante für zwei
Smaragde oder mit **3** die Turbo-Variante für vier Smaragde. Sie landen in
Hotbar-Slot 2 beziehungsweise 3. Standard braucht 4 Sekunden, Schnell
1,8 Sekunden und Turbo 0,9 Sekunden pro Erzstück.

**Toxic Depths** ist ein 3D-First-Person-Mining-Abenteuer, in dem Erkundung, Ressourcenmanagement und Überleben im Mittelpunkt stehen.

Tief unter der Erde wartet eine gefährliche Mine voller wertvoller Erze. Doch du bist nicht freiwillig dort: Dein Arbeitgeber erwartet, dass du eine bestimmte **Quote** erfüllst. Erst wenn du genügend wertvolle Rohstoffe gesammelt hast, darfst du die Mine wieder verlassen.

## Die Spielidee

Erkunde verzweigte Höhlen, baue Erze mit unterschiedlichen Eigenschaften ab und entscheide, wie weit du dich in die Tiefe vorwagst. Je tiefer du kommst, desto wertvoller können deine Funde sein – aber desto größer werden auch die Gefahren.

Deine Energie ist begrenzt, und manche Erze können giftig sein. Deshalb musst du abwägen, ob sich ein weiterer Abstecher lohnt oder ob du lieber zum Händler zurückkehrst. Dort kannst du gesammelte Erze verkaufen und mit dem verdienten Geld nützliche Items und bessere Ausrüstung kaufen, um dich auf die nächste Erkundung vorzubereiten.

## Dein Ziel

**Erfülle deine Quote und verlasse die Mine lebendig.** Wer zu viel riskiert und seine Energie oder Gesundheit durch Gift verliert, scheitert. Erfolgreich ist, wer seine Ressourcen klug einsetzt und den richtigen Zeitpunkt für die Rückkehr findet.

## Atmosphäre

Mit einem **Low-Poly-Grafikstil**, einer geheimnisvollen Höhlenwelt und atmosphärischen Geräuschen soll *Toxic Depths* das Gefühl vermitteln, auf sich allein gestellt immer tiefer in eine unbekannte Mine vorzudringen.

Das Spiel verbindet die Entdeckung neuer Höhlenbereiche mit dem motivierenden Kreislauf aus **Abbauen, Verkaufen, Aufrüsten und Weitererkunden**.

## Sprint, Stamina und Energie

Mit WASD bewegen, mit linker oder rechter Shift-Taste sprinten und mit Leertaste springen.
Sprinten verbraucht nur bei Bewegung Stamina (20 Punkte pro Sekunde). Nach 0,8 Sekunden
Pause regeneriert sie sich mit 30 Punkten pro Sekunde. Bei Ersch?pfung ist Sprinten
wieder ab 20 % der verf?gbaren Maximalstamina m?glich.

Jeder verbrauchte Staminapunkt kostet au?erdem 0,05 Energie. Energie regeneriert sich
nicht von selbst. Die maximale Stamina sinkt linear von 100 % bei voller Energie auf
70 % bei leerer Energie; die Staminabar zeigt diese verringerte Kapazit?t mit an.
Alle Werte lassen sich am Player in `PlayerVitals` und `PlayerMovement` einstellen.

Sp?tere Verbrauchsitems k?nnen zum Beispiel
`player.GetComponent<ToxicDepths.PlayerVitals>().RestoreEnergy(25f)` aufrufen.
Das f?llt Energie bis zum Maximum auf und erh?ht damit wieder die Staminakapazit?t;
die Stamina selbst erholt sich weiterhin ?ber die normale Regeneration.
