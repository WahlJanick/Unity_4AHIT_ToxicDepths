# Toxic Depths

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
