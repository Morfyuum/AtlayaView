# AtlayaView Release Notes

## 2.0.35 - 2026-07-19

- Neu: **Farbprofile sind jetzt eine exklusive Fokus-Ansicht.** Ist ein Profil aktiv (z. B.
  „Bilder“), behalten NUR dessen Erweiterungen ihre Profilfarben – alle übrigen Erweiterungen
  werden einheitlich silbergrau dargestellt. So hebt ein Profil genau die Dateiarten hervor,
  um die es geht. Der freie-Speicher-Block behält seine Farbe.
- Neu: Fest eingebautes **„Startprofil“** (lokalisiert in allen 5 Sprachen) an Position 1 der
  Profilliste: Auswahl schaltet zurück auf die Standardfarben (inkl. eigener Einzelfarb-
  Anpassungen aus dem Farbschema-Dialog) – kein Profil mehr aktiv. Nicht lösch- oder editierbar.
  Die Profilliste ist damit die alleinige Umschaltung zwischen Standardfarben und beliebig
  vielen eigenen, weiterhin erstell-/bearbeit-/löschbaren Profilen.
- Das aktive Profil wird gespeichert und beim nächsten Programmstart wiederhergestellt; beim
  Öffnen des Farbprofil-Dialogs ist es vorselektiert. Wird das aktive Profil gelöscht, springt
  die Anzeige automatisch auf das Startprofil zurück. „Alle zurücksetzen“ im Farbschema-Dialog
  beendet zusätzlich ein aktives Profil.
- Verifiziert per UI-Automation (echte kompilierte App, Screenshots + Pixelvergleich):
  Profil „Bilder“ anklicken → Bilddateien gelb, `.txt`-Kissen silbergrau; „Startprofil“
  anklicken → Standardfarben zurück; Neustart mit aktivem Profil → Fokus-Ansicht bleibt.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.35-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.35-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.35-win-x64.zip`, `dist/AtlayaView-2.0.35-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.35-win-x64/atlaya_renderer.dll`

## 2.0.34 - 2026-07-19

- Neu: **Umschalten = Anwenden.** Das bloße Auswählen eines Farbprofils in der Profilliste
  (z. B. Klick auf „Bilder“) überträgt dessen Farben jetzt sofort und automatisch auf die
  Kissendarstellung – ohne zusätzlichen Klick auf „Speichern“ oder „Auf Liste anwenden“
  (beide Knöpfe bleiben für Bearbeitung bzw. Schließen-mit-Übernahme erhalten). Per
  UI-Automation gegen die echte kompilierte App verifiziert: Treemap startet rot (.bmp-Override),
  wird durch reines Anklicken des Profils „Bilder“ sofort gelb.
- Neu: Eine Profilanwendung wird jetzt sofort auf die Festplatte gespeichert (nicht mehr erst
  beim „OK“ des Farbschema-Dialogs) – sie überlebt damit auch das Schließen der Dialoge über
  „X“/Escape und den nächsten Programmstart. Ebenfalls per Neustart-Test verifiziert.
- Statustext „… angewendet – auf ‚OK‘ klicken, um zu speichern“ entsprechend geändert zu
  „… angewendet und gespeichert“ (alle 5 Sprachen).

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.34-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.34-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.34-win-x64.zip`, `dist/AtlayaView-2.0.34-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.34-win-x64/atlaya_renderer.dll`

## 2.0.33 - 2026-07-19

- Wichtiger Bugfix (Persistenz): Über den Farbschema-Dialog gesetzte Farben (auch über ein
  angewendetes Farbprofil) wurden bisher nur im laufenden Prozess in `ColorScheme` geschrieben.
  `SettingsStore.Save()` – die Funktion, die Einstellungen tatsächlich auf die Festplatte
  schreibt – wurde bislang ausschließlich vom Optionen-Dialog aus aufgerufen, nie vom
  Farbschema-Dialog. Ergebnis: Eine Farbänderung wirkte im laufenden Programm korrekt, ging aber
  beim nächsten Start wieder verloren, sofern man nicht zusätzlich (zufällig) auch den
  Optionen-Dialog bestätigt hatte. `MainWindow.MenuColors_Click` ruft nach einem erfolgreichen
  „OK“ jetzt ebenfalls `SettingsStore.Save()` auf.
- Bugfix: Nach Anwenden eines Farbprofils („Speichern“/„Auf Liste anwenden“) zeigte die rechte
  Farbvorschau (Muster, RGB-Regler, Hex-Feld) im Farbschema-Dialog für die gerade ausgewählte
  Erweiterung weiterhin die alte Farbe, obwohl Liste und Treemap bereits korrekt aktualisiert
  waren – wirkte wie ein weiterer Beleg für „Profil wird nicht angewendet“, obwohl nur diese
  Detailansicht veraltet war. Die Auswahl wird nach dem Neuaufbau der Liste jetzt erneut gesetzt,
  wodurch sich der Farbwähler korrekt aktualisiert.
- Bugfix: „Alle zurücksetzen“ im Farbschema-Dialog leerte bisher nur die interne
  Änderungs-Zwischenablage, ohne `ColorScheme` selbst zurückzusetzen oder den Treemap neu zu
  rendern – der Button hatte sichtbar keine Wirkung. Setzt jetzt auch die aktive Farbtabelle
  zurück und aktualisiert die Anzeige sofort.
- Release-Version auf 2.0.33 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.33-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.33-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.33-win-x64.zip`, `dist/AtlayaView-2.0.33-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.33-win-x64/atlaya_renderer.dll`

## 2.0.32 - 2026-07-19

- Wichtiger Bugfix: Ein angewendetes Farbprofil landete bisher nur in einer Zwischenablage
  (`_pending`) des Farbschema-Dialogs, die erst mit einem ZUSÄTZLICHEN, nicht offensichtlichen
  „OK“-Klick auf diesem (durch den Farbprofil-Dialog teilweise verdeckten) Fenster tatsächlich
  in die Anzeige übernommen wurde. Wer nach „Auf Liste anwenden“ nur den Farbprofil-Dialog
  schloss, ohne zusätzlich auf dem dahinterliegenden Farbschema-Dialog „OK“ zu klicken, sah nie
  eine Änderung – wurde wiederholt als „Profil wird nicht angewendet“ gemeldet. „Speichern“ und
  „Auf Liste anwenden“ schreiben die Farben jetzt sofort und direkt in das aktive Farbschema und
  stoßen unmittelbar ein Neu-Rendern des Treemaps an, auch während die Dialoge noch offen sind
  – kein zweiter Bestätigungsschritt mehr nötig.
- Release-Version auf 2.0.32 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.32-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.32-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.32-win-x64.zip`, `dist/AtlayaView-2.0.32-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.32-win-x64/atlaya_renderer.dll`

## 2.0.31 - 2026-07-19

- Bugfix: „Speichern" im Farbprofil-Dialog schrieb die Farben bisher nur in die wiederverwendbare
  Profil-Vorlage auf der Festplatte, spielte sie aber nirgends in die aktuelle Ansicht ein –
  sichtbar wurde eine Profilfarbe erst, wenn man zusätzlich (und nicht offensichtlich) auch noch
  „Auf Liste anwenden" klickte. Beide Knöpfe übergeben die Farben jetzt sofort an den
  Farbschema-Dialog; nach dessen „OK" erscheinen sie ohne erneuten Scan auf dem Treemap.
- Release-Version auf 2.0.31 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.31-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.31-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.31-win-x64.zip`, `dist/AtlayaView-2.0.31-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.31-win-x64/atlaya_renderer.dll`

## 2.0.30 - 2026-07-19

- Wichtiger Bugfix: Farbprofile wurden korrekt in `ColorScheme` übernommen (per Testfall
  verifiziert – Rendering zeigt nach `SetColor` sofort die richtige Farbe), erschienen aber auf
  dem Treemap trotzdem nicht, wenn zuvor eine Legenden-Kategorie angeklickt worden war (Einzel-
  oder Strg-Klick auf eine Kategorie-Kachel unten). In diesem Zustand übermalte der
  Kategoriefilter JEDE Erweiterung außerhalb der aktiven Kategorie mit Dunkelgrau – auch
  Erweiterungen mit einer gerade frisch zugewiesenen Profilfarbe. Fix: Eine explizit gesetzte
  Farb-Override (u. a. aus einem Farbprofil) ist jetzt vom Kategoriefilter ausgenommen und bleibt
  immer sichtbar, in beiden Renderern (nativ und verwalteter Fallback). Mit einem synthetischen
  Testfall verifiziert: derselbe Pixel, der bei aktivem Fremd-Kategorie-Filter vorher grau wurde,
  zeigt jetzt korrekt die zugewiesene Profilfarbe.
- Release-Version auf 2.0.30 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.30-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.30-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.30-win-x64.zip`, `dist/AtlayaView-2.0.30-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.30-win-x64/atlaya_renderer.dll`

## 2.0.29 - 2026-07-19

- Bugfix: Der Farbprofil-Dialog war mit 620×520px zu klein für den neuen Inhalt (Such-/
  Ankreuzliste, „Farbe aus Grundliste übernehmen"-Knopf) – der Farbwähler (Palette + Hex-Feld)
  war dadurch unsichtbar und erst nach manuellem Vergrößern des Fensters erreichbar. Fenster
  ist jetzt standardmäßig größer (680px hoch) und der Editor-Bereich zusätzlich in einen
  ScrollViewer gepackt, damit künftige Ergänzungen nicht wieder unerreichbar werden.
- Bugfix: `ColorSchemeDialog` startete seine Arbeitskopie der Farb-Overrides (`_pending`) leer
  statt mit den bereits gesetzten Farben vorbefüllt. Da „OK" intern `ColorScheme.ResetAll()`
  aufruft und danach nur `_pending` zurückspielt, gingen dadurch alle in der aktuellen Sitzung
  nicht direkt angefassten, bereits gesetzten Farben verloren (z. B. aus einer früheren Sitzung
  oder einem zuvor angewendeten Farbprofil) – jetzt wird `_pending` beim Öffnen des Dialogs mit
  den aktuellen Overrides vorbefüllt.
- Release-Version auf 2.0.29 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.29-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.29-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.29-win-x64.zip`, `dist/AtlayaView-2.0.29-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.29-win-x64/atlaya_renderer.dll`

## 2.0.28 - 2026-07-19

- Neu: Der Farbprofil-Editor (Farbschema-Dialog → „🎨 Farbprofile …") wurde umgebaut. Statt
  Erweiterungen einzeln über ein Kombinationsfeld hinzuzufügen, zeigt eine durchsuchbare Liste
  jetzt alle bekannten Erweiterungen mit Ankreuzfeld – angehakt = Teil des Profils. Der
  Farbwähler (Palette-Kacheln und Hex-Feld) weist die gewählte Farbe direkt allen angehakten
  Erweiterungen gemeinsam zu; ein neuer Knopf „Farbe aus Grundliste übernehmen" setzt für die
  angehakten Erweiterungen stattdessen ihre bereits im Hauptfarbschema hinterlegte Farbe.
  Farben bleiben beim zwischenzeitlichen Abhaken einer Erweiterung innerhalb derselben
  Bearbeitung erhalten (erneutes Ankreuzen verliert die zuvor gesetzte Farbe nicht). Neue,
  dem Farbschema noch unbekannte Erweiterungen weiterhin über ein Textfeld + „+" anlegbar.
- Release-Version auf 2.0.28 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.28-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.28-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.28-win-x64.zip`, `dist/AtlayaView-2.0.28-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.28-win-x64/atlaya_renderer.dll`

## 2.0.27 - 2026-07-19

- Bugfix: In der Multi-Laufwerk-Ansicht bekam ein deutlich kleineres Laufwerk neben einem
  großen nur eine 4px breite Spalte (`CalculateDriveRegions`s alter `Math.Max(4, ...)`-Klammer)
  – praktisch nicht mehr anklickbar (Bugreport: "das lange Kissen ganz rechts war nicht
  aktivierbar"). `AllocateWithMinimum` reserviert jetzt für jede Laufwerksspalte zuerst eine
  klickbare Mindestbreite (bis zu 60px) und verteilt erst danach den restlichen Platz
  proportional zur belegten Größe – verifiziert mit einem Testfall im Verhältnis 6 Mrd. : 1
  (60px statt 4px für das kleine Laufwerk, weiterhin korrekt sichtbar gerendert).
- Bugfix: Beim Wechsel von einer Einzellaufwerk-Ansicht (mit Navigationspfad, z. B. "G:\ >") in
  die Multi-Laufwerk-Ansicht blieb der alte Navigations-Breadcrumb stehen, obwohl er dort nicht
  mehr zur Navigation gehörte – wird beim Start eines Multi-Laufwerk-Scans jetzt mit geleert.
- Release-Version auf 2.0.27 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.27-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.27-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.27-win-x64.zip`, `dist/AtlayaView-2.0.27-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.27-win-x64/atlaya_renderer.dll`

## 2.0.26 - 2026-07-19

- Bugfix (Nachbesserung zu 2.0.23): Der bisherige Fix für das WPF/PerMonitorV2-Problem
  „Titelleiste auf einem sekundären Monitor mit abweichender DPI-Skalierung erst nach
  manuellem Ziehen am Fensterrand verschiebbar" hat live nicht funktioniert – von Chris
  bestätigt (Fenster startete unten auf dem sekundären Monitor, ließ sich nicht nach oben auf
  den anderen Monitor verschieben, bis der weiße Rand am rechten Fensterrand angefasst wurde).
  Ursache der Nachbesserung: `SetWindowPos` mit `SWP_FRAMECHANGED` allein (nur Neuzeichnen des
  Fensterrahmens, keine echte Größenänderung) reicht nicht aus, um WPFs intern zwischengespeicherten
  DPI-Transform (`HwndTarget`) für den aktuellen Monitor neu zu berechnen – nur ein echter
  `WM_SIZE`-Zyklus tut das, exakt das, was manuelles Ziehen am Rand auslöst. Neuer, gemeinsamer
  Fix (`Core/WindowFrameFix.cs`) simuliert das automatisiert: 1 Pixel breiter, sofort wieder
  zurück, unsichtbar für den Nutzer. Jetzt auf das Hauptfenster UND alle Dialogfenster
  (Einstellungen, Farbschema, Farbprofile, Filter, Update, Über AtlayaView) angewendet, da alle
  denselben zugrunde liegenden WPF-Bug teilen können.
- Release-Version auf 2.0.26 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.26-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.26-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.26-win-x64.zip`, `dist/AtlayaView-2.0.26-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.26-win-x64/atlaya_renderer.dll`

## 2.0.25 - 2026-07-19

- Bugfix: Bei Auswahl von genau 2 Laufwerken (z. B. E: und G:) blieben beide Häkchen gesetzt,
  aber es wurde nur der Inhalt eines Laufwerks angezeigt. Ursache: Schlug der Hintergrundscan
  für eines der ausgewählten Laufwerke fehl (z. B. kein Zugriff mehr, Wechseldatenträger
  entfernt, Netzlaufwerk getrennt), wurde `FileSystemScanner.ScanAsync` intern zwar sauber
  abgefangen (kein Absturz), das betroffene Laufwerk aber nur still aus dem Render-Cache
  entfernt – die Checkbox blieb angehakt, obwohl für dieses Laufwerk nie wieder etwas gezeichnet
  werden konnte. `RefreshMultiDriveAsync` wählt ein fehlgeschlagenes Laufwerk jetzt automatisch
  ab (Haken verschwindet) und meldet es in der Statuszeile („nicht lesbar, abgewählt: …“), statt
  einen inkonsistenten Auswahlzustand stehen zu lassen. Bleibt nach dem Abwählen nur noch ein
  gültiges Laufwerk übrig, wechselt die Ansicht automatisch zurück in den normalen
  Einzellaufwerk-Modus. Kernrendering (Regionsaufteilung + Cushion-Treemap-Layout) selbst wurde
  mit zwei real unterschiedlich großen Testordnern (Verhältnis 30:1) verifiziert und zeigt beide
  Bereiche korrekt an – das eigentliche Problem lag im stillen Scan-Fehlerpfad, nicht im
  Zeichnen.
- Release-Version auf 2.0.25 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.25-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.25-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.25-win-x64.zip`, `dist/AtlayaView-2.0.25-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.25-win-x64/atlaya_renderer.dll`

## 2.0.24 - 2026-07-19

- Neu: Farbprofile (Dialog „🎨 Farbprofile …") unterstützen jetzt Pro-Erweiterung-Farben
  statt einer einzigen gemeinsamen Farbe für alle Erweiterungen im Profil. Beim Bearbeiten
  eines Profils lassen sich Erweiterungen aus der vorhandenen Farbschema-Liste auswählen
  oder neu eingeben (werden dann beim Speichern auch dauerhaft in die globale Liste
  übernommen), und jede Erweiterung im Profil kann eine eigene, vom Listen-Standard
  abweichende Farbe bekommen. Alte, vor 2.0.24 gespeicherte Profile (eine Farbe für alle
  Erweiterungen) werden beim ersten Laden automatisch und ohne sichtbaren Unterschied in
  das neue Format migriert.
- Neu: die kurze Versionsnummer (z. B. „v2.0.24") steht jetzt fett, ganz rechts in der
  Menüzeile neben „Hilfe" statt in der Werkzeugleiste.
- Bugfix: Wählt man alle Laufwerke/Filter ab, wurde bisher weiterhin die zuletzt gerenderte
  (aber inhaltsleere) Ansicht angezeigt statt des Startbildschirms. `HasVisibleContent`
  prüft jetzt nach jeder Filter-/Kategorie-Änderung, ob nach Anwendung von `AppFilter` und
  aktiven Legenden-Kategorien überhaupt noch etwas sichtbar bleibt, und blendet sonst den
  Startbildschirm ein.
- Bugfix: Die Versionsanzeige (Programmfenster, Startbildschirm, Webseite) zeigte teils den
  vollen Git-Commit-Hash an (z. B. „v2.0.23+f880120724fa2…") statt der kurzen Version. Ursache:
  MSBuilds `IncludeSourceRevisionInInformationalVersion` hängt standardmäßig automatisch den
  Hash an `InformationalVersion` an, sobald ein `.git`-Ordner erkannt wird. Jetzt im .csproj
  explizit deaktiviert. Die Webseite war nicht betroffen, da sie die Version separat aus
  `<Version>` bzw. dem Update-Feed liest.
- Release-Version auf 2.0.24 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.24-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.24-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.24-win-x64.zip`, `dist/AtlayaView-2.0.24-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.24-win-x64/atlaya_renderer.dll`

## 2.0.23 - 2026-07-19

- Neu: kurze Versionsnummer (z. B. „v2.0.23") wird jetzt dauerhaft oben rechts in der
  Toolbar angezeigt (`LocalizationManager.HeaderVersionText`), nicht mehr nur im Leer-Zustand.
- Bugfix: Startet AtlayaView auf einem nicht-primären Monitor (v. a. mit abweichender
  DPI-Skalierung zum Hauptbildschirm), blieb die Titelleiste zunächst nicht interaktiv – das
  Fenster ließ sich erst auf einen anderen Bildschirm verschieben, nachdem man einmal am Rand
  gezogen hatte. Bekannter WPF/PerMonitorV2-Effekt (vgl. `dotnet/wpf#6103`): der
  Fensterrahmen wird auf einem sekundären Monitor beim ersten Anzeigen nicht korrekt
  neu berechnet. Fix: `Window_Loaded` stößt jetzt automatisch über `SetWindowPos` mit
  `SWP_FRAMECHANGED` genau die Neuberechnung an, die bisher nur das manuelle Ziehen am
  Fensterrand auslöste – ohne Position oder Größe sichtbar zu verändern.
- Release-Version auf 2.0.23 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.23-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.23-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.23-win-x64.zip`, `dist/AtlayaView-2.0.23-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.23-win-x64/atlaya_renderer.dll`

## 2.0.22 - 2026-07-19

- Wichtiger Bugfix: Der Resize-Fix aus 2.0.20 hat die eigentliche Ursache nicht vollständig
  behoben – `ImgTreemap_SizeChanged` löste `DoLayoutAndRender()` weiterhin sofort und
  ungebremst aus, parallel zum bereits vorhandenen 150-ms-Debounce-Timer
  (`OnRenderSizeChanged`). Bei schnellem Ziehen am Fensterrand liefen dadurch mehrere
  `_layout.Layout(...)`-Aufrufe GLEICHZEITIG auf demselben `_layout`-Objekt und denselben
  `node.Bounds` – ein Datenrace, der zu „Layout fehlgeschlagen"-Fehlerdialogen führte (teils
  mehrfach gestapelt, da jeder überlappende Aufruf seinen eigenen Dialog zeigte). Fix: echte
  Wiedereintritts-Sperre (`_isRelayouting`/`_pendingRelayout`) – läuft bereits eine
  Neuberechnung, wird ein weiterer Render-Wunsch nur vorgemerkt und nach Abschluss der
  aktuellen Berechnung einmal mit der dann aktuellen Fenstergröße nachgeholt, statt parallel
  zu rechnen. Mit 200 sehr schnellen, automatisierten Resize-Schritten auf einer bereits
  gerenderten Ansicht verifiziert (vorher reproduzierbar, jetzt nicht mehr).
- Release-Version auf 2.0.22 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.22-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.22-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.22-win-x64.zip`, `dist/AtlayaView-2.0.22-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.22-win-x64/atlaya_renderer.dll`

## 2.0.21 - 2026-07-19

- Neu: Prüfabstand für automatische Updates um „Bei jedem Start" erweitert (Einstellungen →
  Updates → Prüfabstand), zusätzlich zu Täglich/Wöchentlich/Monatlich/Jährlich. Nur wirksam,
  wenn „Automatisch prüfen" oder „Automatisch prüfen und updaten" aktiv ist.
  `Core/UpdateScheduler.cs`: neuer Intervall-Wert `"startup"`, fällig einmal pro
  Programmstart (Vergleich `last-check` gegen den Zeitpunkt des Prozessstarts statt gegen ein
  Tage-Intervall). Beim Implementieren einen echten .NET-Stolperstein gefunden und per
  eigenem Test aufgedeckt: ohne expliziten statischen Konstruktor darf der Compiler
  („beforefieldinit") das Referenz-Zeitfeld verzögert erst beim ersten tatsächlichen
  Lesezugriff initialisieren – durch eine Short-Circuit-Auswertung passierte dieser erste
  Zugriff hier immer erst beim ZWEITEN Aufruf, mit „jetzt" statt dem echten Prozessstart, was
  „Bei jedem Start" wirkungslos gemacht hätte. Fix: expliziter (leerer) statischer
  Konstruktor erzwingt die Initialisierung vor dem ersten Aufruf.
- Release-Version auf 2.0.21 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.21-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.21-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.21-win-x64.zip`, `dist/AtlayaView-2.0.21-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.21-win-x64/atlaya_renderer.dll`

## 2.0.20 - 2026-07-19

- Wichtiger Bugfix: Absturz „'-∞' ist kein gültiger Wert für die Eigenschaft 'Width'" beim
  Ändern der Fenstergröße, während die Maus über dem Treemap steht (oder ein Scan läuft).
  Ursache: `DoLayoutAndRender`/`DoMultiDriveLayoutAndRender` berechnen `node.Bounds` im
  gesamten Baum auf einem Hintergrund-Thread (`_layout.Layout(...)` in `Task.Run`) – wenn in
  genau diesem Moment eine Mausbewegung `TreemapGrid_MouseMove` → `UpdateOverlay` auslöste,
  las das den UI-Thread dieselben `Bounds`-Objekte mitten in der Neuberechnung, teils mit
  inkonsistenten Zwischenwerten bis hin zu ±Infinity. Fix: neues `_isRelayouting`-Flag sperrt
  alle Hit-Test-Aufrufe (Maus-Move/-Klick/-Rechtsklick/-Rad) für die Dauer der
  Hintergrund-Neuberechnung, zusätzlich prüft `UpdateOverlay` die Bounds jetzt explizit auf
  endliche, nicht-negative Werte, bevor sie an WPF übergeben werden.
- Bugfix: Das „Scan läuft …"-Fenster wuchs und schrumpfte abhängig von der Länge des gerade
  gescannten Pfads. Card hat jetzt eine feste Breite/Höhe; lange Pfade werden zweizeilig
  umgebrochen (mit Auslassungspunkten, falls auch das nicht reicht) statt die Box zu dehnen.
- Release-Version auf 2.0.20 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.20-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.20-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.20-win-x64.zip`, `dist/AtlayaView-2.0.20-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.20-win-x64/atlaya_renderer.dll`

## 2.0.19 - 2026-07-19

- Neu (Opt-in, standardmäßig AUS): „Schneller Scan" unter Einstellungen → Scan-Geschwindigkeit.
  Liest NTFS-Laufwerke über den USN-Änderungsjournal-Mechanismus (`FSCTL_ENUM_USN_DATA` für
  den ersten Scan – ein sequenzieller Durchlauf durchs ganze Volume statt N einzelner
  Verzeichnis-Abfragen; `FSCTL_QUERY_USN_JOURNAL`/`FSCTL_READ_USN_JOURNAL` für spätere
  erneute Scans desselben Laufwerks – dabei werden nur die tatsächlich geänderten Ordner neu
  aufgelistet, nicht das ganze Laufwerk). Braucht erhöhte Rechte (Windows verlangt das für
  jeden Volume-Handle-Zugriff, unabhängig vom Zweck); die Einstellung bietet dafür einen
  Neustart mit `Verb=runas` an. Ohne Adminrechte oder für Scans in einen bestimmten
  Unterordner hinein (das Verfahren arbeitet immer volumenweit) läuft unverändert der normale
  Scanner. **Wichtiger Hinweis zum Rollout:** Der eigentliche NTFS-Volume-Zugriff (neue Datei
  `Core/NtfsFastScanner.cs`) konnte in dieser Entwicklungsumgebung mangels Administratorrechten
  nicht live gegen einen echten elevated-Handle getestet werden – nur die Sicherheitsschranken
  (Fallback bei fehlenden Rechten, bei Unterordner-Scans, bei jedem Fehler) wurden verifiziert.
  Bitte den Schnellscan nach der Installation einmal bewusst ausprobieren und Auffälligkeiten
  (falsche Größen, fehlende Dateien) melden, bevor er als zuverlässig gilt.
- Release-Version auf 2.0.19 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.19-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.19-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.19-win-x64.zip`, `dist/AtlayaView-2.0.19-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.19-win-x64/atlaya_renderer.dll`

## 2.0.18 - 2026-07-19

- Performance: Der Scan-Vorlauf („Dateien und Ordner werden eingelesen …", Phase 1 – zählt
  die Verzeichnisstruktur für die Fortschrittsanzeige vor) lief bisher komplett
  einzelsträngig, obwohl der eigentliche Scan direkt danach (Phase 2) längst parallelisiert
  war – dadurch wurde das Laufwerk effektiv einmal einzelsträngig und einmal parallel
  abgelaufen. Phase 1 nutzt jetzt denselben parallelen Task-Ansatz (gleiche Tiefenbegrenzung,
  gleiches Parallelitäts-Limit) wie Phase 2. Größter Effekt auf SSD/NVMe; auf klassischen
  Festplatten begrenzt die Kopfmechanik weiterhin, wie viel parallele I/O tatsächlich bringt.
  Erster Schritt einer mehrteiligen Scan-Geschwindigkeits-Initiative (Cache mit
  Änderungs-Nachverfolgung und optionaler Admin-Schnellscan folgen in künftigen Versionen).
- Release-Version auf 2.0.18 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.18-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.18-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.18-win-x64.zip`, `dist/AtlayaView-2.0.18-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.18-win-x64/atlaya_renderer.dll`

## 2.0.17 - 2026-07-19

- Bugfix: Zoomrichtung des Mausrads im Treemap war invertiert – Rad von sich weg drehen
  zoomte aus der Ansicht heraus statt hinein, Rad zu sich hin drehen zoomte hinein statt
  heraus. Jetzt zoomt „Rad von sich weg" in das Objekt unter dem Mauszeiger hinein (bei
  fortgesetztem Drehen immer eine Ebene tiefer), „Rad zu sich hin" zoomt schrittweise wieder
  heraus bis zur Gesamtübersicht.
- Release-Version auf 2.0.17 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.17-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.17-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.17-win-x64.zip`, `dist/AtlayaView-2.0.17-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.17-win-x64/atlaya_renderer.dll`

## 2.0.16 - 2026-07-18

- Öffentlicher Release-Kanal auf GitHub umgestellt: Das Repository ist jetzt öffentlich, die Download-ZIPs liegen als GitHub Releases vor, und die AtlayaView-Webseite verweist für die aktuelle Version direkt auf diese Release-Artefakte.
- Die In-App-Update-Prüfung bleibt über den maschinenlesbaren Feed auf atlaya.capecter.com angebunden, lädt die eigentlichen Update-Dateien ab dieser Version aber aus den GitHub-Releases. Webseite, GitHub-Release und Update-Feed zeigen jetzt konsistent auf dieselben Full-/FX-Pakete.
- Release-Version auf 2.0.16 angehoben in .NET-Projekt und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.16-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.16-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.16-win-x64.zip`, `dist/AtlayaView-2.0.16-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.16-win-x64/atlaya_renderer.dll`

## 2.0.13 - 2026-07-15

- Wichtiger Bugfix: Nach Abbruch eines Scans („Scan abbrechen") oder einem Scan-Fehler blieb
  das „Scan läuft …"-Overlay dauerhaft sichtbar, obwohl der Scan intern bereits beendet war –
  die App wirkte komplett eingefroren und ließ sich nur über den Task-Manager beenden. Per
  Live-Speicherabzug des hängenden Prozesses (`dotnet-dump`) bestätigt: `IsScanning` wurde
  intern korrekt auf `false` gesetzt, aber `PropertyChanged` feuerte wegen einer fehlenden
  expliziten `[CallerMemberName]`-Angabe in `SetScanningState()` mit dem falschen
  Property-Namen („SetScanningState" statt „IsScanning") – das Overlay reagierte dadurch
  nie auf Scan-Abbruch oder -Fehler, nur ein zufälliges gleichzeitiges Aktualisieren von
  `DisplayRoot` (nur bei erfolgreichem Scan-Abschluss) verdeckte den Bug bisher. Betraf JEDEN
  abgebrochenen oder fehlgeschlagenen Scan, nicht nur bestimmte Ordner.
- Release-Version auf 2.0.13 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.13-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.13-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.13-win-x64.zip`, `dist/AtlayaView-2.0.13-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.13-win-x64/atlaya_renderer.dll`

## 2.0.12 - 2026-07-15

- Programm-Icon ergänzt: Bisher hatte AtlayaView weder ein Windows-Programm-Icon
  (Taskleiste/Desktop/Alt-Tab/Explorer zeigten das generische .NET-Symbol) noch ein Icon im
  Leer-Zustand der App selbst (nur ein 🌲-Platzhalter-Emoji). Aus dem offiziellen Atlaya-Logo
  (`ui/static/atlaya.png`, 96×96) wurde ein Mehrfachauflösungs-`.ico`
  (`Resources/atlaya.ico`, 16/32/48/64/96 px) erzeugt und per `<ApplicationIcon>` im .csproj
  als Executable-Icon eingebunden; im Leer-Zustand ersetzt das gleiche Logo (`Resources/atlaya.png`)
  das Emoji.
- Aktive Version wird jetzt neben dem Programmnamen im Leer-Zustand angezeigt
  (`LocalizationManager.EmptyStateVersion`, alle 5 Sprachen).
- Release-Version auf 2.0.12 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.12-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.12-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.12-win-x64.zip`, `dist/AtlayaView-2.0.12-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.12-win-x64/atlaya_renderer.dll`

## 2.0.11 - 2026-07-15

- Echter Fund nach Chris' Rückmeldung „Update von 2.0.9 auf 2.0.10 geht nicht, Version
  bleibt 2.0.9": Bei Installation in einem geschützten Ordner (bei ihm
  `C:\Program Files (x86)\AtlayaView`) scheiterte `Copy-Item` im Austausch-Skript lautlos an
  „Access Denied", weil das Skript ohne Administratorrechte lief – AtlayaView startete danach
  einfach unverändert neu, ohne jede Fehlermeldung. Fix: `SelfUpdater.LaunchSwapAndExit()`
  prüft jetzt per Schreibprobe, ob der Installationsordner beschreibbar ist; falls nicht,
  startet das Austausch-Skript mit `Verb=runas` (UAC-Sicherheitsabfrage). Fehler landen
  zusätzlich in `%TEMP%\AtlayaView_apply.log`, und die App startet auch bei einem Fehler
  wieder (statt spurlos zu verschwinden). Bricht der Nutzer die UAC-Abfrage ab, zeigt der
  Update-Dialog jetzt eine klare Meldung statt eines generischen Download-Fehlers.
- Release-Version auf 2.0.11 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.11-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.11-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.11-win-x64.zip`, `dist/AtlayaView-2.0.11-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.11-win-x64/atlaya_renderer.dll`

## 2.0.10 - 2026-07-15

- Italienische Sprachauswahl + Übersetzung ergänzt: `AppLanguage`-Enum hatte bisher nur
  Deutsch/Englisch/Französisch/Spanisch, obwohl die Webseite und alle anderen Atlaya-
  Produkte seit Langem 5-sprachig (DE/EN/FR/IT/ES) sind. `Core/LocalizationManager.cs`
  übersetzt jetzt alle ~90 UI-Texte (Menüs, Dialoge, Statusmeldungen, Update-Bereich)
  zusätzlich ins Italienische, neuer Menüpunkt „Italiano" unter Ansicht → Sprache.
- Release-Version auf 2.0.10 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.10-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.10-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.10-win-x64.zip`, `dist/AtlayaView-2.0.10-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.10-win-x64/atlaya_renderer.dll`

## 2.0.9 - 2026-07-15

- Echter Fund nach Chris' Rückmeldung, dass sich in den Options-Dialoge weder
  „Update-Prüfung" noch „Prüfabstand" öffnen/ändern ließen (2.0.8 hatte nur die
  Sichtbarkeit gefixt, nicht die eigentliche Ursache): das globale `ComboBox`-Template in
  `Resources/Styles.xaml` hatte **keinen `ToggleButton`** – `IsDropDownOpen` wurde nirgends
  gesetzt, das Dropdown öffnete sich bei Mausklick nie. Betraf damit potenziell JEDE
  ComboBox der App (`cmbDrives`, `cmbOpener`, `cmbSizeUnit`, nicht nur die neuen
  Update-Felder), nur bisher nicht aufgefallen. Fix: Template um einen echten
  `ToggleButton` (an `IsDropDownOpen` zweiseitig gebunden) ergänzt, exakt gleiches
  Aussehen wie zuvor.
- Release-Version auf 2.0.9 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.9-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.9-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.9-win-x64.zip`, `dist/AtlayaView-2.0.9-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.9-win-x64/atlaya_renderer.dll`

## 2.0.8 - 2026-07-15

- Bugfix Options-Dialog: Die neue „Updates"-Sektion (seit 2.0.7) war am unteren Rand des
  Dialogs nicht erreichbar, weil der Inhalt bei fester Fensterhöhe (620px, kein Scrollen)
  nicht mehr vollständig hineinpasste – die automatischen Prüfmodi + Intervall-Auswahl waren
  dadurch faktisch nicht auswählbar. Fix: Einstellungsinhalt jetzt in einem `ScrollViewer`,
  Fenster zusätzlich per Ziehgriff vergrößerbar (`ResizeMode="CanResizeWithGrip"`).
- Release-Version auf 2.0.8 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.8-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.8-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.8-win-x64.zip`, `dist/AtlayaView-2.0.8-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.8-win-x64/atlaya_renderer.dll`

## 2.0.7 - 2026-07-14

- Neue Update-Prüfung eingebaut (erste Netzwerkfunktion neben den Impressum/Datenschutz-
  Links): `Core/UpdateChecker.cs` fragt `atlayaview/updates/latest.json` auf
  atlaya.capecter.com ab und vergleicht die Version.
- Echter selbstersetzender Updater (`Core/SelfUpdater.cs`): lädt die passende Variante
  (mit/ohne .NET, automatisch anhand der installierten Variante erkannt) in einen
  Temp-Ordner, erzeugt ein PowerShell-Hilfsskript, das nach Prozessende die Dateien
  austauscht und AtlayaView neu startet.
- Hilfe-Menü um "Auf Updates prüfen …" erweitert (`Dialogs/UpdateDialog`), zeigt
  installierte/neue Version + Hinweistext, Button "Jetzt aktualisieren" nur bei
  gefundener passender Variante.
- Options-Dialog um zwei neue Auswahlfelder erweitert: Prüfmodus (Manuell / Automatisch
  prüfen / Automatisch prüfen und updaten) und Intervall (Täglich/Wöchentlich/Monatlich/
  Jährlich), persistiert in `AtlayaView.settings.json`.
- `Core/UpdateScheduler.cs` prüft im Hintergrund (30-Minuten-Poll, eigener Zeitstempel in
  `update_check_state.json`) und wendet je nach Modus automatisch an oder benachrichtigt nur.
- Release-Version auf 2.0.7 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.7-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.7-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.7-win-x64.zip`, `dist/AtlayaView-2.0.7-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.7-win-x64/atlaya_renderer.dll`

## 2.0.6 - 2026-07-14

- Hilfe-Menü um "Impressum (Web)" und "Datenschutz (Web)" erweitert – öffnet die
  entsprechenden Seiten auf atlaya.capecter.com im Standardbrowser (einziger Netzwerkzugriff
  der Anwendung, nur auf ausdrücklichen Klick).
- Info-Dialog ("Über AtlayaView") ergänzt um einen kurzen Datenschutz-Hinweis: die
  Anwendung verarbeitet keine personenbezogenen Daten und stellt sonst keine
  Netzwerkverbindung her.
- Release-Version auf 2.0.6 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.6-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.6-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.6-win-x64.zip`, `dist/AtlayaView-2.0.6-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.6-win-x64/atlaya_renderer.dll`

## 2.0.5 - 2026-05-14

- Scan-Overlay oeffnet jetzt unmittelbar beim Scanstart im Hauptfenster.
- Der erste Scan-Schritt zeigt das Einlesen mit sichtbarer Balkenbewegung statt mit einem statischen Nullstand.
- Direkt danach schaltet der Ablauf ohne Zusatzfenster in die Verarbeitungsphase mit echtem Fortschritt um.
- Der untere Button "Scan abbrechen" ist direkt an den aktiven Scan gekoppelt.
- Release-Version auf 2.0.5 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.5-win-x64/AtlayaView.exe`
- ZIP: `dist/AtlayaView-2.0.5-win-x64.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.5-win-x64/atlaya_renderer.dll`

## 2.0.4 - 2026-05-14

- Toolbar erweitert um einen direkten Laufwerks-Picker fuer Einzel- und Mehrfachauswahl.
- Laufwerksmenues bleiben bei Mehrfachauswahl offen, damit mehrere Haken ohne staendiges Neuoeffnen gesetzt werden koennen.
- Auswahl im Toolbar-Picker und im Ansicht-Menue bleibt synchron.
- Release-Version auf 2.0.4 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.4-win-x64/AtlayaView.exe`
- ZIP: `dist/AtlayaView-2.0.4-win-x64.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.4-win-x64/atlaya_renderer.dll`

## 2.0.3 - 2026-05-14

- Laufwerksauswahl aus dem Ansicht-Menü startet Scans jetzt automatisch ohne zusätzlichen F5-Refresh.
- Einzelne Menü-Auswahl startet nach kurzer Verzögerung direkt den Scan des gewählten Laufwerks.
- Mehrfachauswahl startet parallele Laufwerks-Scans automatisch und verwirft überholte Zwischenläufe über eine Generation-Schranke.
- Release-Version auf 2.0.3 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.3-win-x64/AtlayaView.exe`
- ZIP: `dist/AtlayaView-2.0.3-win-x64.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.3-win-x64/atlaya_renderer.dll`

## 2.0.2 - 2026-05-14

- App-Version auf 2.0.2 angehoben in .NET-Projekt, Manifest und Rust-Renderer.
- Über-Dialog erweitert: zeigt jetzt die echte App-Version sowie Informationen zur Rust-Integration und zum Fallback auf den verwalteten C#-Renderer.
- Nativer Renderer exportiert seine eigene Versionsnummer über `atlaya_renderer_version`, damit die UI den Rust-Stand direkt anzeigen kann.
- Release-Build und Packaging über `build-hybrid.ps1` stabilisiert.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.2-win-x64/AtlayaView.exe`
- ZIP: `dist/AtlayaView-2.0.2-win-x64.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.2-win-x64/atlaya_renderer.dll`

## Build und Verifikation

- Rust-Validierung: `cargo check` in `native/atlaya_renderer` erfolgreich.
- Release-Publish: `pwsh -File .\build-hybrid.ps1 -Configuration Release` erfolgreich.
- Das Publish-Skript baut zuerst den Rust-Renderer mit `cargo build --release` und veröffentlicht danach die WPF-Anwendung nach `dist/publish/AtlayaView-<Version>-win-x64`.
- Im Anschluss wird automatisch ein versionsiertes ZIP unter `dist/` erzeugt.

## Technische Hinweise

- `AtlayaView.csproj` schließt generierte Inhalte unter `obj`, `bin`, `dist/publish` und `.artifacts` explizit aus, um WPF-Doppeldefinitionen aus `wpftmp`-Zwischenständen zu verhindern.
- `build-hybrid.ps1` nutzt für Publish-Zwischenartefakte einen Temp-Pfad außerhalb des Repos, um Konflikte mit Security-Scannern auf `obj/.../refint/*.dll` zu vermeiden.