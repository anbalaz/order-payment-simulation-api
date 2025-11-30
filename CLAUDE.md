# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ASP.NET Core 8.0 Web API for simulating order payment workflows. Currently in early development with minimal boilerplate structure.

## Build and Run Commands

```bash
# Build the solution
dotnet build OrderPaymentSimulation.Api.sln

# Run the API (from project directory)
cd src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api
dotnet run

# Run with specific profile
dotnet run --launch-profile https  # Runs on https://localhost:7006
dotnet run --launch-profile http   # Runs on http://localhost:5267

# Restore dependencies
dotnet restore OrderPaymentSimulation.Api.sln

# Clean build artifacts
dotnet clean OrderPaymentSimulation.Api.sln
```

## Architecture

**Technology Stack:**
- .NET 8.0 (target framework: net8.0)
- ASP.NET Core Web API with Minimal API patterns
- Swagger/OpenAPI (Swashbuckle 6.6.2) for API documentation

**Project Structure:**
- Solution: `OrderPaymentSimulation.Api.sln` (root directory)
- Main project: `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/`
- Root namespace: `order_payment_simulation_api`

**Configuration:**
- Development runs on http://localhost:5267 and https://localhost:7006
- Swagger UI available at `/swagger` endpoint in Development environment
- HTTPS redirection enabled
- Authorization configured (UseAuthorization middleware)

**Current State:**
- Minimal boilerplate with default WeatherForecast controller
- Standard ASP.NET Core middleware pipeline configured in Program.cs
- Controllers use attribute routing pattern `[Route("[controller]")]`
