# SmartTrip API - Backend

## Environment Setup

This project uses environment variables for sensitive configuration. Follow these steps:

### 1. Create .env file

Copy the example file and fill in your values:

```bash
cp .env.example .env
```

### 2. Configure your .env file

Edit `.env` and add your actual values:

```env
# Database Configuration
DB_HOST=localhost
DB_NAME=travelassistant
DB_USERNAME=postgres
DB_PASSWORD=your_actual_password

# JWT Configuration
JWT_SECRET=generate-at-least-32-random-bytes

# Cache and external services
REDIS_CONNECTION=localhost:6379
GEMINI_API_KEY=your_actual_gemini_api_key
GOOGLE_API_KEY=your_actual_google_places_api_key
WEATHERAPI_KEY=your_actual_weatherapi_key
TICKETMASTER_API_KEY=your_actual_ticketmaster_api_key
EVENTBRITE_API_TOKEN=your_actual_eventbrite_token

# Comma-separated web origins; required in production
CORS_ALLOWED_ORIGINS=http://localhost:8081,http://localhost:19006

# Application Settings
ASPNETCORE_ENVIRONMENT=Development
```

### 3. Run the application

```bash
dotnet restore
dotnet build
dotnet run
```

## Important Notes

- **Never commit `.env` file to git** - it's already in `.gitignore`
- `.env.example` is a template - commit this to show others what variables are needed
- All sensitive data (passwords, API keys) should only be in `.env`
- Production secrets must be supplied by the deployment platform's secret store
- If a credential has ever been committed, revoke/rotate it; deleting it from the latest file is not sufficient

## Database Migration

After setting up your `.env` file:

```bash
dotnet ef database update
```

## API Documentation

Once running, visit: `http://localhost:5191/swagger`

