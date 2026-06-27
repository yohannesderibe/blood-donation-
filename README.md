# St. Amanuel Church Blood Donation System

A bilingual (English / Amharic) blood donation management platform for **St. Amanuel Church**, built with React, .NET, and PostgreSQL.

## Tech Stack

| Layer    | Technology                          |
|----------|-------------------------------------|
| Frontend | React 19 + TypeScript + Vite        |
| Backend  | ASP.NET Core 10 Web API             |
| Database | PostgreSQL                          |
| SMS      | Afro Messaging API (WHALE identifier)|

## Features (Admin Role)

1. **Dashboard** – donor stats, blood type pie chart, recent donors, system notifications
2. **Donor Dictionary** – bilingual registration form, searchable/filterable table, pagination
3. **Send SMS** – bulk SMS to all/selected/custom groups via Afro Messaging
4. **Reports** – CSV & PDF exports (donor directory, donation history, SMS campaigns)
5. **Hospital Partners** – CRUD for partner hospitals linked to donation verification

Additional: JWT authentication, BCrypt password hashing, audit logs, eligibility rules (90-day interval).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [PostgreSQL 14+](https://www.postgresql.org/download/)

## Setup

### 1. Database

Create the PostgreSQL database:

```sql
CREATE DATABASE st_amanuel_blood;
```

Optionally run the full schema manually:

```bash
psql -U postgres -d st_amanuel_blood -f database/schema.sql
```

Or let the backend auto-create tables on first run via Entity Framework (`EnsureCreated`).

Update the connection string in `backend/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=st_amanuel_blood;Username=postgres;Password=YOUR_PASSWORD"
}
```

### 2. Backend

```bash
cd backend
dotnet run
```

API runs at **http://localhost:5000**

Default admin credentials:
- **Username:** `admin`
- **Password:** `Admin@123`

### 3. Frontend

```bash
cd frontend
npm install
npm run dev
```

App runs at **http://localhost:5173**

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Admin login |
| GET | `/api/dashboard/summary` | Dashboard stats |
| GET | `/api/dashboard/blood-types` | Blood type distribution |
| GET | `/api/donors` | List donors (paginated, filterable) |
| POST | `/api/donors` | Register new donor |
| DELETE | `/api/donors/{id}` | Delete donor |
| POST | `/api/donors/{id}/donate-today` | Mark donated today |
| POST | `/api/sms/send` | Send bulk SMS |
| GET | `/api/sms/balance` | SMS balance info |
| POST | `/api/reports/generate` | Generate CSV/PDF report |
| GET/POST/PUT/DELETE | `/api/hospitals` | Hospital partner CRUD |

## Afro Messaging Configuration

Configured in `backend/appsettings.json`:

- **Identifier:** WHALE (`e80ad9d8-adf3-463f-80f4-7c4b39f7f164`)
- **API Key:** stored in `AfroMessaging:ApiKey`

## Project Structure

```
├── backend/           # .NET Web API
│   ├── Controllers/
│   ├── Data/
│   ├── Models/
│   └── Services/
├── frontend/          # React SPA
│   └── src/
│       ├── api/
│       ├── components/
│       ├── i18n/
│       └── pages/
└── database/
    └── schema.sql     # PostgreSQL schema
```

## Language Toggle

Use the **English / አማርኛ** button in the sidebar (or login page) to switch languages across all admin pages.

## Security Notes

- Change the default admin password after first login
- Replace the JWT secret key in production
- Store Afro Messaging API keys in environment variables or Azure Key Vault for production deployments
