# SmartStashAI — Inteligentny System Zarządzania Domowym Magazynem

SmartStashAI to nowoczesny system klasy Smart Home przeznaczony do ewidencjonowania, lokalizowania i inteligentnego katalogowania przedmiotów domowych, narzędzi, okablowania oraz komponentów elektronicznych i automatyki. 

System opiera się na architekturze współdzielonej (multi-tenant), umożliwiając wspólne zarządzanie zasobami w obrębie jednego gospodarstwa domowego (rodziny) przy jednoczesnym wsparciu lokalnej sztucznej inteligencji (Vision AI) uruchamianej bezpośrednio na infrastrukturze użytkownika.

---

## 🏗️ Architektura Systemu

System został zaprojektowany w architekturze klient-serwer z podziałem na warstwy:



1. **SmartStashAI.Api (Backend):** Serce systemu oparte na **.NET 8 (ASP.NET Core Web API)**. Odpowiada za bezpieczną logikę biznesową, autoryzację, zarządzanie bazą danych, generowanie kodów QR oraz bezpośrednią orkiestrację zabawek AI (Ollama).
2. **SmartStashAI.Client (Frontend — w przygotowaniu):** Mobilna aplikacja kliencka realizowana w technologii **Blazor WebAssembly (PWA — Progressive Web App)**. Pozwala na natywne uruchamianie systemu na smartfonach, integrację z aparatem fotograficznym (skanowanie przedmiotów i kodów QR) oraz pracę w trybie offline.
3. **Lokalny Węzeł AI:** Instancja serwera **Ollama** uruchomiona lokalnie w sieci domowej, wykorzystująca moc obliczeniową dedykowanego GPU (np. AMD Radeon za pośrednictwem ROCm) do bezchmurnego przetwarzania obrazu i tekstu za pomocą modelu `llama3.2-vision:11b`.

---

## 🔄 Kluczowe Przepływy Danych (Workflows)

### 1. Przepływ Inteligentnego Katalogowania (Vision AI)
Aplikacja eliminuje uciążliwe, ręczne wpisywanie danych podczas chowania przedmiotów:
* Użytkownik robi zdjęcie przedmiotu (np. nowo zakupionego mikrokontrolera lub śrubokręta) telefonem.
* Klient konwertuje obraz do formatu Base64 i przesyła go do endpointu `/api/Recognition/recognize`.
* Backend za pomocą abstrakcji `Microsoft.Extensions.AI` i biblioteki `OllamaSharp` wysyła strumień binarny do lokalnego modelu **Llama 3.2 Vision**.
* Model analizuje cechy wizualne i zwraca ustrukturyzowany, deterministyczny obiekt JSON zawierający sugerowaną nazwę, kategorię oraz przeznaczenie w języku polskim.

### 2. Identyfikacja Schowków przez Kody QR
Każdy schowek, szafka, szuflada czy pojemnik narzędziowy otrzymuje w systemie swoje odwzorowanie.
* System generuje unikalny token (np. `STASH_C8E00BDDDA1B`) i zamienia go na fizyczny kod QR.
* Po wydrukowaniu i naklejeniu kodu na fizyczny pojemnik, wystarczy zbliżyć aparat telefonu.
* Aplikacja natychmiast dekoduje token i za pomocą endpointu `/api/StorageLocations/qr/{token}` wyświetla na ekranie pełną zawartość danej szuflady, oszczędzając czas spędzony na fizycznym przeszukiwaniu szaf.

---

## 🗄️ Model Danych i Struktura Bazy (SQLite)

System korzysta z lekkiej, transakcyjnej bazy danych SQLite zarządzanej przez **Entity Framework Core 8**. Struktura encji odwzorowuje relacje świata rzeczywistego z zachowaniem ścisłej izolacji danych pomiędzy różnymi rodzinami.



* **Households (Gospodarstwa domowe):** Nadrzędny kontener logiki biznesowej. Grupuje użytkowników oraz przypisane do domu lokalizacje.
* **Users (Użytkownicy):** Członkowie danego gospodarstwa domowego. Autoryzacja realizowana jest poprzez bezpieczne tokeny **JWT**, w których zaszyte jest `HouseholdId`. Uniemożliwia to dostęp do danych osobom spoza przypisanej rodziny.
* **StorageLocations (Miejsca przechowywania):** Klasy reprezentujące szafki zaimplementowane w formie **struktury drzewiastej (samoodwołanie)**. Pozwala to na mapowanie rzeczywistych zależności (np. `Garaż` -> `Szafa warsztatowa` -> `Szuflada 3`). Usunięcie szafy nadrzędnej skutkuje kaskadowym usunięciem informacji o strukturze podrzędnej (`DeleteBehavior.Cascade`).
* **Items (Przedmioty):** Fizyczne przedmioty przypisane do konkretnego schowka. Posiadają unikalne cechy (nazwa, kategoria, zastosowanie, ścieżka do zdjęcia) oraz stanową flagę **`IsLost`**. Jeżeli użytkownik nie znajdzie przedmiotu w szafce, oznacza go jako zgubiony, co pozwala na filtrowanie globalnej listy zgub w domu.

---

## 📁 Struktura Solucji

```text
SmartStashAI/
│
├── SmartStashAI.sln               # Główny plik solucji Visual Studio
│
├── SmartStashAI.Api/              # PROJEKT BACKENDOWY (ASP.NET Core Web API)
│   ├── Controllers/               # Endpointy (Auth, Recognition, StorageLocations, Items)
│   ├── Data/                      # Kontekst EF Core (AppDbContext) i plik smartstash.db
│   ├── Dtos/                      # Obiekty transferu danych (Data Transfer Objects)
│   ├── Migrations/                # Migracje bazy danych Code-First
│   ├── Models/                    # Definicje encji bazodanowych (Item, Household, itp.)
│   ├── Services/                  # Logika biznesowa (Serwis autoryzacji, haszowanie haseł)
│   ├── Program.cs                 # Konfiguracja potoku i kontenera IoC
│   └── appsettings.json           # Konfiguracja środowiskowa (Klucze JWT, Connection Strings)
│
└── SmartStashAI.Client/           # PROJEKT FRONTENDOWY (Blazor WebAssembly PWA - w przygotowaniu)

---

## Wdrożenie i uruchomienie deweloperskie
1. Środowisko lokalne AI
Przed uruchomieniem backendu upewnij się, że lokalny demon Ollama jest aktywny i posiada pobrany model multimodalny:

Bash
ollama run llama3.2-vision:11b
2. Inicjalizacja bazy danych i start systemu
Otwórz solucję w programie Visual Studio 2022.

Uruchom Package Manager Console dla projektu .Api i utwórz bazę danych:

PowerShell
Update-Database
Wybierz profil uruchamiania https lub http i wciśnij F5.

Dokumentacja interfejsów API otworzy się automatycznie w formacie Swagger UI pod adresem: https://localhost:7047/swagger/index.html.