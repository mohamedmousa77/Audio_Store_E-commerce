# 🛒 Audio Store - Backend API

> Enterprise e-commerce API built with **ASP.NET Core**, **Clean Architecture**, and **Domain-Driven Design**.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**Live Demo**: [Coming Soon]  
**Frontend**: [Audio Store SPA](https://github.com/mohamedmousa77/Audio-Store)

---

## 🎯 What is this?

A production-ready e-commerce backend demonstrating **enterprise-level architecture patterns** for scalable, maintainable systems. Built as a portfolio project showcasing Clean Architecture, DDD, and CQRS implementation.

**Perfect for**: Learning enterprise patterns, interview preparation, reference architecture

---

## ✨ Key Features

- 🏗️ **Clean Architecture** (4-layer: Domain, Application, Infrastructure, API)
- 🎯 **Domain-Driven Design** with 6 bounded contexts
- ⚡ **CQRS** pattern for read/write optimization
- 🔐 **JWT Authentication** + Role-based authorization (Admin/Customer)
- 📦 **50+ RESTful endpoints** (Products, Orders, Cart, Admin)
- 🧪 **70%+ test coverage** (xUnit, Moq)
- 🐳 **Docker ready** with docker-compose
- 🚀 **CI/CD** with GitHub Actions

---

## 🛠️ Tech Stack

**Backend**: ASP.NET Core 8, C# 12, Entity Framework Core  
**Database**: SQL Server
**Caching**: Redis, InMemory  
**Testing**: xUnit, Moq, FluentAssertions  
**DevOps**: Docker, GitHub Actions, Terraform (IaC)

---

## 🚀 Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (easiest way)

### Run with Docker

```bash
# Clone repo
git clone https://github.com/mohamedmousa77/Audio_Store_E-commerce.git
cd Audio_Store_E-commerce

# Start all services (API + SQL Server + Redis)
docker-compose up -d

# API runs at http://localhost:7071
# Swagger docs at http://localhost:7071/swagger
```

## 📁 Project Structure
```bash
AudioStore.Solution/
│
├── AudioStore.Domain/          # Entities, Value Objects, Domain Logic
├── AudioStore.Application/     # Use Cases, DTOs, Services, Validators
├── AudioStore.Infrastructure/  # DbContext, Repositories, External Services
├── AudioStore.API/            # Controllers, Middleware, Swagger
└── AudioStore.Tests/          # Unit & Integration Tests
```
## 📈 Performance

- API Response Time: <200ms average

- Concurrent Users: Tested up to 1000 simultaneous

- Database: Optimized with indexes and query batching

- Caching: Redis for frequent reads (~85% hit rate)

## 📝 License
MIT © [Mohamed Mousa](https://github.com/mohamedmousa77)


## 📞 Contact

**Mohamed Mousa** - Senior Full-Stack .NET Developer

📧 [mohamed.mousa.contact@gmail.com](mohamed.mousa.contact@gmail.com)

💼 [LinkedIn](https://www.linkedin.com/in/mohamedmousa-/)

🌐 [Portfolio](mohamedmousa.it)

💻 [GitHub](https://github.com/mohamedmousa77)

## 🌟 Related Projects

**Frontend:** [Audio Store Frontend (Angular 17)](https://github.com/mohamedmousa77/Audio-Store)

    ⭐ If you find this project helpful for learning, please star the repo!

        Built with ❤️ using Clean Architecture & Domain-Driven Design

