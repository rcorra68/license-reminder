# 🛡️ License Reminder CLI

![.NET Version](https://img.shields.io/badge/.NET-8.0-blueviolet)
![Maintained](https://img.shields.io/badge/Maintained-Yes-green)
![License](https://img.shields.io/badge/License-MIT-yellow)

A professional .NET Console Application designed to automate the reconciliation of driver's license expirations and employee contact data. It manages automated email notifications and ensures data consistency across multiple CSV sources.

## 🚀 Architecture Overview

The project follows **Clean Architecture** principles and **SOLID** patterns, organized into three main layers:

* **AvvisoScadenzaPatenti.Core**: Domain Models, Repository Interfaces, and Business Logic (Orchestrator).
* **AvvisoScadenzaPatenti.Infrastructure**: Data access (CSV flat files), Email services (MailKit), and Security (Base64/Data Protection).
* **AvvisoScadenzaPatenti.Cli**: The entry point managing Dependency Injection (Microsoft.Extensions), Configuration, and Command Line parsing.

## 🛠 Tech Stack

- **Framework:** .NET 8.0 (LTS)
- **CSV Parsing:** [CsvHelper](https://joshclose.github.io/CsvHelper/)
- **Email:** [MailKit](http://www.mimekit.net/)
- **CLI Parsing:** [CommandLineParser](https://github.com/commandlineparser/commandlineparser)
- **DI & Hosting:** Microsoft.Extensions.Hosting

## 📋 Features

- **Data Reconciliation**: Cross-references License CSV data with Employee CSV records.
- **Smart Contact Lookup**: Handles uncompliant email addresses via a dedicated lookup table.
- **Auto-Provisioning**: Automatically creates new employee records if they are missing from the database.
- **Secure Storage**: Includes a utility to encrypt sensitive passwords directly into `appsettings.json`.

## ⚙️ Installation & Setup

1. **Clone the repository**:

```bash
git clone [https://github.com/rcorra68/license-reminder.git](https://github.com/rcorra68/license-reminder.git)
cd license-reminder
```

2. **Restore dependencies**:

```bash
dotnet restore
```

3. **Configure the application**:

Update `src/AvvisoScadenzaPatenti.Cli/appsettings.json` with your SMTP settings and CSV file paths.

## 🚀 Usage

### Run the application

```bash
dotnet run --project src/AvvisoScadenzaPatenti.Cli/AvvisoScadenzaPatenti.Cli.csproj
```

### Encrypt a password

```bash
dotnet run --project src/AvvisoScadenzaPatenti.Cli/AvvisoScadenzaPatenti.Cli.csproj -- --crypt "your_password"
```

### Show help

```bash
dotnet run --project src/AvvisoScadenzaPatenti.Cli/AvvisoScadenzaPatenti.Cli.csproj -- --help
```

## 🧪 Running Tests

To execute the unit test suite:

```bash
dotnet test
```

## 🔐 Security & Configuration

This project supports two ways to manage sensitive information (like SMTP passwords), depending on the environment.

### 1. Local Development (Windows / macOS)

We use the native **.NET Secret Manager** to keep credentials out of the source code. This is the preferred method for local development.

To set up your local secrets:

```bash
# Initialize secrets for the project
dotnet user-secrets init --project src/AvvisoScadenzaPatenti.Cli/

# Set your SMTP credentials
dotnet user-secrets set "Settings:Smtp:Username" "your_email@example.com"
dotnet user-secrets set "Settings:Smtp:Password" "your_secure_password"
dotnet user-secrets set "Settings:Smtp:Host" "your.domain.com"
dotnet user-secrets set "Settings:AdminEmail" "admin_email@example.com"
```
