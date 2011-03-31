Dies ist ein C#-Client für das Paranuss-Turnier April 2011 der Entwickler-Ecke.

Los geht's
=====

Zum Öffnen des Projekts brauchst du eine IDE, die VS2010-Projekte öffnen kann.
Trage zuerst am Anfang von Server.cs deine Daten ein, die du bei der Registrierung für das Turnier erhalten hast. In Bot.cs findest du eine Beispiel-Implementierung namens "Randomy". Kopiere oder überschreibe die Klasse, um deinen eigenen Bot zu implementieren.

Die UI
=====

Generell öffnet sich für jedes Spiel ein neues Fenster. Dieses schließt sich automatisch bei Spielende, das Ergebnis wird im Hauptfenster eingetragen.

Internet-Spiel
-----

Das Herz des Übungsturniers. Wähle in der ComboBox einen Bot deiner Wahl aus und verbinde dich. Du kannst von beliebigen anderen Clients herausgefordert werden, außerdem wird der Client alle 10 Sekunden von sich aus Clients herausfordern.

Lokales Spiel
----

Wähle zwei beliebige Bots aus und lass sie in einer lokalen Runde gegeneinander antreten.
