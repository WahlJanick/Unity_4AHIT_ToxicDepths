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
- **Inventar und Hotbar:** Verwaltung und Nutzung von Items über eine Hotbar mit vier Slots.
- **Quotensystem:** Sammeln ausreichend wertvoller Ressourcen, um die Mine verlassen zu dürfen.

Das HUD informiert den Spieler über wichtige Werte wie Energie, Stamina, Geld und den aktuellen Quotenfortschritt. Ein dynamisches Fadenkreuz unterstützt die Interaktion mit der Spielwelt.

### Energie und Stamina

Das Bewegungssystem verbindet Stamina und Energie miteinander:

Beim Sprinten wird Stamina verbraucht, die sich nach einer kurzen Pause automatisch regeneriert. Zusätzlich kostet verbrauchte Stamina einen kleinen Teil der verfügbaren Energie.

Je weniger Energie vorhanden ist, desto geringer fällt die maximale Staminakapazität aus. Energie regeneriert sich nicht automatisch, sondern muss beispielsweise durch kaufbare Verbrauchsitems wiederhergestellt werden.

Die Umsetzung erfolgt über **C#-Skripte**, die Spielerbewegung, Ressourcenwerte und deren Darstellung im HUD steuern. Die wichtigsten Werte sind konfigurierbar, damit das Gameplay während der Entwicklung angepasst und ausbalanciert werden kann.

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
