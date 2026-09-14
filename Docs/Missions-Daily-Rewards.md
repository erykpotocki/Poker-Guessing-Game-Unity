# Misje, odznaki i Daily Rewards

Wdrożone: 59 misji kariery + 7 progów avatarów reklamowych, istniejące misje startowe i dzienne, osobna kolekcja odznak, kalendarz siedmiu nagród i przycisk w górnym pasku. Grafiki zawierające liczby są przypisane tylko do zgodnych progów; pozostałe odznaki mają kolor kategorii i symbol.

## Zasady
- Dzień kalendarzowy: Europe/Warsaw; reset zawsze o 00:00 czasu polskiego (CET/CEST). Maksymalnie jeden odbiór dziennie; przerwa nie resetuje kolejności. Cofnięcie zegara blokuje odbiór do daty późniejszej niż ostatni odbiór.
- Bazowe dni: 500 gold, 750 gold, 5 diamonds, 1000 gold, 1 spin, 10 diamonds, 2000 gold.
- Wzrost walut: liniowo 5% za ukończony tydzień, maksymalnie 50%. Zaokrąglenie w dół; spin pozostaje jednym spinem.
- Bonusowe spiny mają osobny trwały licznik. Nie przepadają przy pełnych trzech zwykłych spinach ani przy zmianie dnia. Zużywane są przed zwykłymi ładunkami.
- Wygrane z botami: gra bez innych ludzi, z co najmniej jednym botem. Gra online: co najmniej dwóch ludzi; lobby mieszane zalicza serię online.
- Progi reklam: 1/5/10/25/50/100/250 ukończonych reklam; avatar odbierany w SPECIAL. Istniejący dostawca reklam pozostaje bez zmian.
- Beta Tester 2026 jest wyłączony ze sklepu i spina. Avatar odbierany ręcznie w zakładce SPECJALNE po ukończeniu 5 gier z botami, 5 gier z ludźmi i 5 gier offline na jednym telefonie. Wszystkie trzy warunki muszą być spełnione.

## Zapis i migracja
Nowe pola są częścią istniejącego PlayerSave: DailyRewards, ClaimedCareerMissions, OwnedBadges, BonusSpins oraz historyczne zarobki i liczniki bot/online. Dotychczasowe gry, zwycięstwa, spiny i kolekcje od razu zasilają progi. Historycznych przychodów i rodzaju dawnych rozgrywek nie da się odtworzyć ze starego portfela: te nowe liczniki rozpoczynają się od wdrożenia. Starsze automatyczne nagrody pozostają kompatybilne; nowe nagrody kariery mają osobne identyfikatory.

Zapis nagród pozostaje lokalny. Czas kalendarza pochodzi z HTTPS timeapi.io (UTC), przeliczanego na Europe/Warsaw. Po synchronizacji biegnie według monotonicznego licznika Unity, niezależnie od daty telefonu. Synchronizacja co 120 s, ważność 300 s; po wstrzymaniu aplikacji wymagane ponowne potwierdzenie. Bez ważnego czasu odbiór jest zablokowany; ponawianie automatyczne co 30 s i przyciskiem. Nie zabezpiecza to przed ręczną edycją lokalnego zapisu — do tego potrzebny byłby serwer profili. Starsze zapisy dat UTC migrowane zachowawczo, bez ponownego odbioru potencjalnie odebranej dziś nagrody.

Spiny z nagród wyświetlane łącznie (np. 4/3). Regeneracja uzupełnia tylko Wheel.Charges do 3, a BonusSpins przechowuje nadwyżkę i jest zużywane najpierw.

## Grafiki
17 nowych PNG zapisano w 512×512. Importery istniejących pojedynczych grafik ograniczono do 512, dużych teł do 1024; wieloelementowe arkusze pozostają do 2048. Oryginalne pliki źródłowe pozostają do późniejszego eksportu; Unity skaluje istniejące grafiki przy imporcie, zachowując GUID i wycięcia sprite'ów. Źródła w Download nie są częścią buildu. Szczegóły: reward-texture-optimization.json.

## Weryfikacja
`CareerRewardTests`: kolejność po przerwie, ponowny odbiór po serializacji, cofnięcie daty, cykl i limit bonusu, odbiór kilku progów, oddzielenie odznak od avatarów, historyczne zarobki po wydaniu waluty, deduplikacja meczu i poprawność zasobów PNG.

Wynik 2026-09-13: 34/34 testy CareerRewardTests + ProfileLevelProgressionTests zakończone powodzeniem (Logs/RewardRegressionTests.xml). Kompilacja Unity zakończona powodzeniem. Nowe PNG: 36 595 377 → 7 161 980 bajtów. Nie wykonano oceny wizualnej nowych paneli na fizycznym telefonie ani publikacji nowego buildu.


Aktualizacja 2026-09-14: zielone odebrane dni, złota następna nagroda, przycisk X; menu główne bez powiększonych hitboxów wchodzących na sąsiednie przyciski. Kompilacja C# bez błędów; 15 samodzielnych testów logiki czasu/nagród zakończonych powodzeniem. Zweryfikowano odpowiedź źródła czasu HTTP 200 oraz CORS (*). Testów interfejsu w działającym Unity nie uruchomiono (projekt otwarty w edytorze).

Aktualizacja: polskie zakładki DZIENNE/KARIERA/SPECJALNE i zielono-złoty styl. Beta korzysta z trwałych BotGames/OnlineGames/OfflineGames. Stare wygrane z botami stanowią minimum znanych ukończonych gier; dawnych przegranych z botami i gier offline nie można odtworzyć. Offline nie przyznaje waluty ani EXP; każdy identyfikator gry zaliczany raz, rewanż otrzymuje nowy. Testy logiki: 25/25.
