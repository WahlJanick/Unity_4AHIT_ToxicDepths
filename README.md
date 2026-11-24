# Toxic Depths

**Trello:** https://trello.com/b/nFFamfgQ/itpprojekt4ahitunity

**Toxic Depths** ist ein 3D-First-Person-Mining-Abenteuer, das mit **Unity und C#** entwickelt wird. Im Mittelpunkt stehen Erkundung, Ressourcenmanagement und Überleben.

Tief unter der Erde befindet sich eine gefährliche Mine voller wertvoller Erze. Der Spieler muss im Auftrag seines Arbeitgebers eine bestimmte **Quote** erfüllen. Erst wenn genügend wertvolle Rohstoffe gesammelt wurden, darf er die Mine wieder verlassen.

## Die Spielidee

Der Spieler erkundet eine verzweigte Höhlenwelt, baut unterschiedliche Erze ab und sammelt wertvolle Ressourcen. Je tiefer er in die Mine vordringt, desto wertvoller können die Funde werden – gleichzeitig steigen jedoch auch die Gefahren.

Während der Erkundung müssen **Energie, Stamina und Gesundheit** berücksichtigt werden. Sprinten verbraucht Ausdauer und indirekt Energie, während giftige Erze eine zusätzliche Gefahr darstellen.

Bei einem Händler können gesammelte Rohstoffe verkauft und mit dem verdienten Geld neue Items oder bessere Ausrüstung gekauft werden. Dadurch entsteht ein Gameplay-Kreislauf aus **Erkunden, Abbauen, Verkaufen und Aufrüsten**.

## Spielmechaniken

Die wichtigsten Spielmechaniken umfassen:

- **Bewegung:** First-Person-Steuerung mit WASD, Maus, Springen und Sprinten.
- **Erzabbau:** Unterschiedliche Erze mit individuellen Eigenschaften und Werten.
- **Ressourcenmanagement:** Energie und Stamina müssen während der Erkundung sinnvoll eingesetzt werden.
- **Handelssystem:** Interaktion mit einem NPC zum Kaufen und Verkaufen von Gegenständen.
- **Inventar und Hotbar:** Verwaltung und Nutzung von Items über eine Hotbar mit sechs Slots.
- **Quotensystem:** Sammeln ausreichend wertvoller Ressourcen, um die Mine verlassen zu dürfen.

Das HUD informiert den Spieler über wichtige Werte wie Energie, Stamina, Geld und den aktuellen Quotenfortschritt. Ein dynamisches Fadenkreuz unterstützt die Interaktion mit der Spielwelt.

### Energie und Stamina

Das Bewegungssystem verbindet Stamina und Energie miteinander:

Beim Sprinten wird Stamina verbraucht, die sich nach einer kurzen Pause automatisch regeneriert. Zusätzlich kostet verbrauchte Stamina einen kleinen Teil der verfügbaren Energie.

Je weniger Energie vorhanden ist, desto geringer fällt die maximale Staminakapazität aus. Energie regeneriert sich nicht automatisch, sondern muss beispielsweise durch kaufbare Verbrauchsitems wiederhergestellt werden.

Die Umsetzung erfolgt über **C#-Skripte**, die Spielerbewegung, Ressourcenwerte und deren Darstellung im HUD steuern. Die wichtigsten Werte sind konfigurierbar, damit das Gameplay während der Entwicklung angepasst und ausbalanciert werden kann.

### Interaktion und Erzverarbeitung

Der E-Hinweis sitzt auf dem anvisierten, aufhebbaren Gegenstand. Der Shredder zeigt beim Anvisieren einer laufenden Maschine nur einen Fortschrittsbalken.

Getragenes Erz wird im ausgewählten Hotbar-Slot angezeigt und sperrt bis zum Ablegen den Wechsel per Zahlentaste oder Mausrad. Der vorherige Slotinhalt bleibt erhalten.

| Eingabe | Aktion |
| --- | --- |
| E auf einem Item | Erz, Materialien, Spitzhacke oder einen abgebauten/abgeworfenen Shredder aufheben |
| Linke Maustaste mit Spitzhacke auf einem Shredder | Shredder mit drei Treffern abbauen |
| Shredder auswählen | Rot/grüne Platzierungsvorschau sofort anzeigen |
| E / kurz Q mit ausgewähltem Shredder | Vorschau um 45° nach rechts / links drehen |
| Rechte Maustaste | Shredder an einer gültigen Position abstellen |
| Q halten und loslassen | Ausgewähltes Item werfen; nach 1,25 Sekunden ist die maximale Stärke erreicht |
| Kurz Q bei Erz, Materialien oder Spitzhacke | Mit geringer Kraft ablegen/werfen |

Das Fadenkreuz bewegt sich beim Aufladen von innen nach außen. Auch abgeworfene Spitzhacken und Shredder lassen sich mit E wieder aufnehmen. Aufgestellte Shredder müssen nach Verarbeitung und Ausgabe zuerst mit der Spitzhacke abgebaut werden. Treffer lassen das Modell wie ein Erzvorkommen kurz wackeln. Danach erscheinen sie als Pickup in Originalgröße mit E-Hinweis und landen beim Aufheben wieder in ihrem Modell-Slot. Dabei bleibt der Hotbar-Wechsel erlaubt. Die benötigten Treffer lassen sich am Shredder unter **Abbauen / Hits To Dismantle** einstellen.

Auf ebenen Schrägen richtet sich der Shredder an der Flächennormale aus. Alle vier Ecken müssen auf derselben Ebene unterstützt werden; die maximale Neigung beträgt standardmäßig 35° und ist unter **Maximum Slope** einstellbar.

Die Dauer eines Shredder-Zyklus wird am jeweiligen `OreDeposit` im Inspector unter **Verarbeitung / Shredder Seconds** eingestellt und beim Abbau an das Erzstück weitergegeben. Smaragderz benötigt im Standardmodell 6 Sekunden, im schnellen Modell 2,7 Sekunden und im Turbomodell 1,35 Sekunden. Jedes eingeworfene Stück behält seine eigene Dauer in der Warteschlange.

## Geldanzeige (CurrencyCount)

`Assets/Prefabs/UI/CurrencyCount.prefab` ist in der Spielszene `SampleScene` eingebaut und zeigt rechts oben den Geldstand (anfangs 0 Credits). Hintergrund, Rahmen und Schrift passen zur Hotbar. Einnahmen und Ausgaben werden kurz darunter grün bzw. rot angezeigt.

Das spätere Geldsystem kann die Anzeige über `ToxicDepths.UI.CurrencyCount.SetAmount(neuerGeldstand)` aktualisieren. Zum Laden eines Spielstands ohne Änderungsanzeige: `SetAmount(geladenerGeldstand, showChange: false)`. Die Komponente stellt nur den Wert dar; Smaragde, Inventar und Shop bleiben davon unabhängig.

Das bereitgestellte Münzsymbol liegt als transparentes Sprite unter `Assets/Textures/UI/Coin.png`; die SVG-Vorlage ist unter `SourceAssets/UI/coin-svgrepo-com.svg` archiviert und aus dem Hauptordner entfernt.

## Technische Umsetzung und Arbeitsweise

Das Spiel wird als **3D-PC-Spiel mit Unity** entwickelt. Die Spiellogik wird in C# umgesetzt, während Unity für die Gestaltung der Höhlenwelt, Animationen, Benutzeroberflächen und Soundeffekte verwendet wird.

Die Entwicklung erfolgt schrittweise. Zunächst werden die grundlegenden Mechaniken wie Spielerbewegung, Erzabbau und Interaktionen umgesetzt und getestet. Anschließend werden diese Systeme erweitert und miteinander verbunden.

Dabei wird auf eine **modulare Umsetzung** geachtet, sodass einzelne Spielmechaniken möglichst unabhängig entwickelt, getestet und später erweitert werden können.

Für die Zusammenarbeit werden folgende Werkzeuge verwendet:

- **Unity:** Spielentwicklung, Levelgestaltung und Einbindung der verschiedenen Spielsysteme.
- **C#:** Programmierung der Spielmechaniken und Interaktionen.
- **Git und GitHub:** Gemeinsame Versionsverwaltung und Zusammenführung der entwickelten Funktionen.
- **Trello:** Aufgabenplanung, Dokumentation des Entwicklungsfortschritts.

Das Projekt wird von **Janick Wahl und Michael Windhager** gemeinsam entwickelt. Die Aufgaben umfassen Gameplay-Programmierung, Weltgestaltung, Benutzeroberfläche und Sound.

## Spielziel

Das Hauptziel besteht darin, die vorgegebene **Quote zu erfüllen und die Mine lebendig zu verlassen**.

Dabei muss der Spieler selbst entscheiden, wie lange er weiter nach Rohstoffen sucht und wann eine Rückkehr sinnvoller ist.

Sinkt seine Energie auf null oder scheitert er durch eine Vergiftung, verliert er das Spiel. Gelingt es ihm hingegen, die Quote zu erfüllen und sicher zurückzukehren, gewinnt er.

## Atmosphäre und Gestaltung

*Toxic Depths* verwendet einen **Low-Poly-Grafikstil** mit einer geheimnisvollen, unterirdischen Höhlenwelt.

Atmosphärische Höhlengeräusche, passende Beleuchtung und Animationen sollen das Gefühl verstärken, sich allein in einer gefährlichen Mine zu befinden.

Im Vordergrund steht die Kombination aus **Erkundung, zunehmendem Risiko und langfristigem Spielfortschritt durch bessere Ausrüstung**.
