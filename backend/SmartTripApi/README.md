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
DB_PORT=5432
DB_NAME=TravelAssistant
DB_USER=postgres
DB_PASSWORD=your_actual_password

# JWT Configuration
JWT_SECRET=your-super-secret-key-with-at-least-32-characters-here

# AI Provider Configuration
GEMINI_API_KEY=your_actual_gemini_api_key
GOOGLE_PLACES_API_KEY=your_actual_google_places_api_key

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

## Database Migration

After setting up your `.env` file:

```bash
dotnet ef database update
```

## API Documentation

Once running, visit: `http://localhost:5191/swagger`

