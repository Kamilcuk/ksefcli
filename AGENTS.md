# Projekt ksefcli

Projekt `ksefcli` to aplikacja CLI na system Linux, napisana w C#, służąca do interakcji z API Krajowego Systemu e-Faktur (KSeF) w Polsce. Wykorzystuje ona bibliotekę kliencką `ksef-client-csharp` do komunikacji z usługami KSeF.

## Zasady pracy agenta

Jako agent odpowiedzialny za rozwój tego projektu, będę przestrzegał następujących zasad:

*   **Nie wykonuję poleceń automatycznie bez planu**: Zawsze przedstawiam plan działania przed rozpoczęciem implementacji.
*   **Generuję listę TODO**: Dla złożonych zadań, najpierw tworzę szczegółową, techniczną listę kroków (`TODO`) przy użyciu narzędzia `write_todos`.
*   **Realizuję punkty TODO jeden po jednym**: Pracuję metodycznie, wykonując zadania z listy `TODO` sekwencyjnie.
*   **Format listy TODO**: Lista zadań będzie zwięzła, wykorzystując tylko słowa kluczowe i nie będzie zawierać pełnych zdań. Wszystkie podzadania zostaną spłaszczone i przedstawione jako niezależne zadania.
*   **Minimalizm w komunikacji**: Bądź tak zwięzły, jak to tylko możliwe, i wypisuj minimalną ilość informacji, bez gramatyki.
*   **Commit often**: Commituj zmiany często, po każdej znaczącej zmianie.
*   **Brak pustych linii**: Pisząc kod, nie twórz linii bez zawartości.

W thirdparty/ksef-client-csharp jest zależność.


## Best Practices C# / .NET

Nie używaj var

## Parser Argumentów CLI: Spectre.Console.Cli

Struktura CLI została zaimplementowana w `Program.cs`
