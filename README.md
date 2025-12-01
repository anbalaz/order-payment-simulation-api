# Order Payment Simulation API

ASP.NET Core 8.0 Web API for simulating order payment workflows with PostgreSQL database.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for PostgreSQL)
- Git
- (Optional) [dotnet-ef CLI tool](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) for database migrations

## Project Structure

```
order-payment-simulation-api/
├── src/
│   └── OrderPaymentSimulation.Api/
│       └── OrderPaymentSimulation.Api/          # Main API project
│           ├── Models/                          # Entity models
│           ├── Data/                            # DbContext and configurations
│           ├── Controllers/                     # API controllers
│           └── Program.cs                       # Application entry point
├── Postgres/                                    # Docker configuration
│   ├── docker-compose.yml                       # PostgreSQL container setup
│   └── init-scripts/                            # Database init scripts
├── PRPs/                                        # Project Requirements & Plans
├── CLAUDE.md                                    # AI assistant guidance
└── README.md                                    # This file
```

## Database Schema

The application uses the following tables:

- **users** - User accounts with hashed passwords
- **products** - Product catalog
- **orders** - Customer orders
- **order_items** - Order line items (many-to-many between orders and products)

See `CLAUDE.md` for detailed schema information.

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd order-payment-simulation-api
```

### 2. Start PostgreSQL Database

Navigate to the Postgres directory and start the database using Docker Compose:

```bash
cd Postgres
docker-compose up -d
```

Verify the database is running:

```bash
docker ps
```

You should see a container named `order-payment-db` running.

### 3. Build and Run the API

The application will automatically apply migrations and seed data on startup.

From the repository root:

```bash
dotnet build OrderPaymentSimulation.Api.sln
cd src/OrderPaymentSimulation.Api
dotnet run
```

Or run with specific launch profile:

```bash
dotnet run --launch-profile https  # Runs on https://localhost:7006
dotnet run --launch-profile http   # Runs on http://localhost:5267
```

### 4. Access Swagger UI

Open your browser and navigate to:

- **HTTPS:** https://localhost:7006/swagger
- **HTTP:** http://localhost:5267/swagger

The Swagger UI provides interactive API documentation and testing capabilities.

## API Endpoints

### Authentication

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Password123!"
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "name": "Test User",
    "email": "test@example.com",
    "createdAt": "2025-01-15T10:00:00Z",
    "updatedAt": "2025-01-15T10:00:00Z"
  }
}
```

### User Management

All endpoints except **Create User** require JWT Bearer token in the Authorization header:
```
Authorization: Bearer <your-jwt-token>
```

#### Create User
```http
PUT /api/user
Content-Type: application/json

{
  "name": "New User",
  "email": "newuser@example.com",
  "password": "SecurePassword123!"
}
```

**Response (201 Created):** UserDto with id, name, email, createdAt, updatedAt

#### Update User
```http
POST /api/user
Authorization: Bearer <token>
Content-Type: application/json

{
  "id": 3,
  "name": "Updated Name",
  "email": "updated@example.com",
  "password": "NewPassword123!"  // Optional
}
```

**Response (200 OK):** Updated UserDto

#### Get User
```http
GET /api/user/{id}
Authorization: Bearer <token>
```

**Response (200 OK):** UserDto

#### Delete User
```http
DELETE /api/user/{id}
Authorization: Bearer <token>
```

**Response (200 OK):** `{ "message": "User deleted successfully" }`

### Testing the API

1. **Using Swagger UI:**
   - Navigate to https://localhost:7006/swagger
   - Click "Authorize" button (top right)
   - Create a user using `PUT /api/user`
   - Login using `POST /api/auth/login` to get token
   - Enter token in format: `Bearer <your-token-here>`
   - Click "Authorize"
   - Now you can test protected endpoints

2. **Using curl:**
```bash
# Login
curl -X POST https://localhost:7006/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password123!"}'

# Get user (replace {token} and {id})
curl https://localhost:7006/api/user/1 \
  -H "Authorization: Bearer {token}"
```

## Database Information

**Default Development Configuration:**

- **Database Name:** order_payment_simulation
- **Host:** localhost
- **Port:** 5432
- **Username:** orderuser
- **Password:** dev_password (**WARNING:** For development only!)

**Connection String:**
```
Host=localhost;Port=5432;Database=order_payment_simulation;Username=orderuser;Password=dev_password
```

## Seeded Test Data

The database is automatically seeded with:

**Users:**
- admin@example.com (password: Password123!)
- test@example.com (password: Password123!)

**Products:**
- Laptop ($999.99)
- Mouse ($29.99)
- Keyboard ($79.99)
- Monitor ($399.99)
- Headphones ($199.99)

**Sample Orders:**
- 3 orders for test user with various statuses

## Available Commands

### Build the Solution

```bash
# From repository root
dotnet build OrderPaymentSimulation.Api.sln
```

### Run Tests

```bash
# From repository root (when tests are added)
dotnet test
```

### Database Management

**Note:** The application automatically applies migrations on startup. Manual migration commands are optional.

#### Create a New Migration (Optional)

```bash
cd src/OrderPaymentSimulation.Api
dotnet ef migrations add <MigrationName>
```

#### Apply Migrations Manually (Optional)

```bash
dotnet ef database update
```

#### Rollback Migration

```bash
dotnet ef database update <PreviousMigrationName>
```

#### Remove Last Migration (if not applied)

```bash
dotnet ef migrations remove
```

#### Drop Database

```bash
dotnet ef database drop
```

### Docker Commands

#### Stop the Database

```bash
cd Postgres
docker-compose down
```

#### Stop and Remove Volumes (deletes all data)

```bash
docker-compose down -v
```

#### View Logs

```bash
docker-compose logs -f postgres
```

#### Access PostgreSQL Shell

```bash
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation
```

Useful PostgreSQL commands:
```sql
\dt                          -- List all tables
\d users                     -- Describe users table
SELECT * FROM users;         -- Query users
\q                           -- Quit
```

## Troubleshooting

### Database Connection Fails

**Issue:** Cannot connect to PostgreSQL

**Solutions:**
- Ensure Docker Desktop is running
- Check if PostgreSQL container is running: `docker ps`
- Verify port 5432 is not in use by another application
- Check connection string in `appsettings.json`
- Restart Docker container: `cd Postgres && docker-compose restart`

### Migration Errors

**Issue:** Migration fails on application startup

**Solutions:**
- Ensure PostgreSQL database is running before starting the application
- Check connection string in `appsettings.json` and `appsettings.Development.json`
- Try dropping and recreating database: `cd Postgres && docker-compose down -v && docker-compose up -d`
- Check migration files in `Data/Migrations/` for syntax errors

### Port Already in Use

**Issue:** Port 5432 already in use

**Solutions:**
- Stop other PostgreSQL instances
- Change port in `docker-compose.yml`:
  ```yaml
  ports:
    - "5433:5432"  # Use port 5433 on host
  ```
- Update connection string to match new port

### Application Won't Start

**Issue:** `dotnet run` fails

**Solutions:**
- Check for compilation errors: `dotnet build`
- Ensure all NuGet packages are restored: `dotnet restore`
- Check port conflicts (5267, 7006)
- Review application logs for specific error messages
- Ensure database is accessible before starting the app

### Seed Data Not Applied

**Issue:** Database tables are empty

**Solutions:**
- Check if seeding ran: look for log messages during startup
- Verify database connection is successful
- Drop and recreate database: `cd Postgres && docker-compose down -v && docker-compose up -d`
- Restart application to trigger seeding again

## Development Workflow

1. **Make changes** to entity models or add new features
2. **Create migration**: `dotnet ef migrations add <DescriptiveName>`
3. **Review migration** code in `Data/Migrations/`
4. **Restart application** - migrations are applied automatically on startup
5. **Test changes** using Swagger UI or unit tests
6. **Commit** migration files to version control

## Security Notes

⚠️ **WARNING:** The default credentials are for development only!

**For Production:**
- Use strong passwords
- Store credentials in environment variables or secret management systems (Azure Key Vault, AWS Secrets Manager)
- Never commit production credentials to version control
- Use SSL/TLS for database connections
- Implement proper authentication and authorization

## Technology Stack

- **.NET 8.0** - Application framework
- **ASP.NET Core Web API** - REST API framework
- **Entity Framework Core 8.0** - ORM
- **Npgsql 8.0** - PostgreSQL data provider
- **PostgreSQL 16** - Database
- **Docker** - Containerization
- **Swagger/OpenAPI** - API documentation

## Additional Resources

- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Npgsql Documentation](https://www.npgsql.org/efcore/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Docker Documentation](https://docs.docker.com/)

## License

[Specify your license here]

## Contributing

[Add contribution guidelines if applicable]
