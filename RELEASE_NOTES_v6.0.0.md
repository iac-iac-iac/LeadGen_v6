## LeadGen v6.0.0 — первый релиз

### Что нового

- **WPF-приложение** на .NET 10 с современным UI и анимациями
- **Обработка лидов** — JSON / TSV / CSV из Webbee AI → очистка → Bitrix24 CSV
- **Генератор ссылок** — Яндекс.Карты по 42+ городам, районам и UTC
- **Дашборд** — статистика, графики, история в SQLite
- **Менеджер городов** — редактирование `config.json` без правки файла вручную

### Установка

1. Скачайте `LeadGen-v6.0.0-win-x64.zip`
2. Распакуйте в любую папку
3. Запустите `LeadGen.exe` (нужен [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0))

### Требования

- Windows 10/11 x64
- .NET 10 Desktop Runtime

### Для разработчиков

```powershell
git clone https://github.com/iac-iac-iac/LeadGen_v6.git
dotnet build LeadGen.sln
dotnet test
```

Полный список изменений: [CHANGELOG.md](CHANGELOG.md)
