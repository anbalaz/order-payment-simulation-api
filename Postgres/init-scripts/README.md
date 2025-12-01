# Database Initialization Scripts

This directory is mounted to `/docker-entrypoint-initdb.d` in the PostgreSQL container.

SQL scripts (`.sql`) and shell scripts (`.sh`) placed here will be executed automatically when the container is first created, in alphabetical order.

## Current Setup

The application uses Entity Framework Core migrations for schema management and seeding, so this directory is currently empty.

## Alternative Seeding

If you prefer SQL-based seeding instead of EF Core:

1. Create `01-seed.sql` with INSERT statements
2. Restart the container: `docker-compose down -v && docker-compose up -d`

Note: Files here only run on initial database creation, not on every container start.
