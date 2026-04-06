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

## 🗺️ Roadmap
- [ ] Implement xUnit & Moq test suite for Orchestrator logic.
- [ ] Upgrade Base64 encryption to .NET Data Protection API for enhanced security.
- [ ] Add support for Excel (.xlsx) data sources.