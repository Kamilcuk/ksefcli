import re

with open('README.md', 'r', encoding='utf-8') as f:
    readme = f.read()

# Extract "Opcje Globalne" from README
match_opcje_globalne = re.search(r'### Opcje Globalne\n.*?(?=\n\*   `Auth`:)', readme, re.DOTALL)
if match_opcje_globalne:
    opcje_globalne_text = match_opcje_globalne.group(0)
    # Remove it from README, replace with nothing or a link
    readme = readme.replace(opcje_globalne_text, "")
    
    # Let's clean up the bullet list of commands that was left behind
    match_bullet_list = re.search(r'\n\*   `Auth`:.*?(?=\n## Polecenia)', readme, re.DOTALL)
    if match_bullet_list:
        readme = readme.replace(match_bullet_list.group(0), "")

    # Also clean up "## Użycie" and "Ogólna składnia"
    uzycie_text = """## Użycie

Ogólna składnia poleceń `kcksefcli` jest następująca:

```bash
kcksefcli <polecenie> [opcje]
```
"""
    readme = readme.replace(uzycie_text, "")

    # Add back just Użycie with link
    uzycie_replacement = """## Użycie

Ogólna składnia poleceń `kcksefcli` jest następująca:

```bash
kcksefcli <polecenie> [opcje]
```

Szczegółowy opis konfiguracji profili, globalnych opcji i pamięci podręcznej znajdziesz w dokumencie: [**Konfiguracja**](docs/Configuration.md).
"""
    readme = readme.replace("## Konfiguracja\nSzczegóły konfiguracji opisano w pliku [Konfiguracja (docs/Configuration.md)](docs/Configuration.md).", 
                            "## Konfiguracja\nSzczegóły konfiguracji opisano w pliku [Konfiguracja](docs/Configuration.md).\n\n" + uzycie_replacement)


with open('README.md', 'w', encoding='utf-8') as f:
    f.write(readme)


with open('docs/Configuration.md', 'r', encoding='utf-8') as f:
    config_md = f.read()

# Replace Opcje Cache'owania z tabelą
cache_options = """### Opcje Cache'owania i konfiguracji

Podczas wywoływania komend korzystających z konfiguracji, dostępne są globalne opcje pozwalające na sterowanie zachowaniem pamięci podręcznej oraz środowiskiem logowania.

Dodatkowo zachowaniem tych opcji można sterować poprzez zmienne środowiskowe:
- `$KCKSEFCLI_CONFIG` - domyślna ścieżka do pliku `kcksefcli.yaml`.
- `$KCKSEFCLI_ACTIVE` - nazwa aktywnego profilu, używana gdy nie podano jawnie opcji `--active`.

**Wyłączające się metody konfiguracji:**
Możesz sterować konfiguracją używając opcji `--config`/`--active` LUB definiować ad-hoc poświadczenia opcjami takimi jak `--environment`, `--token`. Próba łączenia opcji ad-hoc z ładowaniem z pliku skutkuje błędem.

| Opcja | Zmienna środowiskowa | Opis | Domyślnie | Konfliktuje z |
| :--- | :--- | :--- | :--- | :--- |
| `-c`, `--config` | `$KCKSEFCLI_CONFIG` | Wskazuje plik `kcksefcli.yaml` zawierający definicje profili. | `./kcksefcli.yaml` lub `~/.config/kcksefcli/kcksefcli.yaml` | Ad-hoc opcje profilu |
| `-a`, `--active` | `$KCKSEFCLI_ACTIVE` | Wybiera z pliku wskazanego w `--config` profil o zadanej nazwie. | `active_profile` z pliku YAML lub pierwszy wylistowany profil | Ad-hoc opcje profilu |
| `--cache` | | Ścieżka do pliku do zapisu oraz odczytu tokenów sesyjnych (cache). | `~/.cache/kcksefcli/tokenstore.json` (Linux/Mac) | Brak |
| `--no-tokencache` | | Całkowicie wyłącza odczyt i zapis tokenów z/do pamięci podręcznej na czas trwania bieżącego wywołania komendy. | `false` | Brak |
| `--environment` | | Ustawia wybrane środowisko KSeF dla wywołania ad-hoc (np. `test`, `demo`). | | `--config`, `--active` |
| `--token` | | Używa wskazanego tokena autoryzacyjnego (numer NIP wyciągany jest z tokenu). Tworzy profil ad-hoc. | | `--config`, `--active` |
"""

match_cache_options = re.search(r'### Opcje Cache\'owania i konfiguracji.*?(?=\n### Współdziałanie)', config_md, re.DOTALL)
if match_cache_options:
    config_md = config_md.replace(match_cache_options.group(0), cache_options)

with open('docs/Configuration.md', 'w', encoding='utf-8') as f:
    f.write(config_md)

