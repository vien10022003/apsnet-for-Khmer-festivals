# Khmer Festivals - Conduit API

This is an ASP.NET Core Web API project implementing the RealWorld API specification for managing Khmer festivals, articles, comments, and user interactions.

## 🚀 Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- SQLite (included with the project)

### Installation & Setup

1. **Clone the repository**
   ```bash
   git clone <your-repository-url>
   cd Khmer-festivals
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Build the project**
   ```bash
   dotnet build
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

   The application will start and be available at:
   - HTTP: `http://localhost:5000`
   - HTTPS: `https://localhost:5001`

### 🗄️ Database

The project uses **SQLite** as the database provider. The database file (`festival.db`) is automatically created when you first run the application.

#### Database Features:
- **Auto-migration**: Database schema is created automatically on startup
- **Seed Data**: Sample data is populated when the database is first created
- **Default Admin Account**: 
  - Username: `admin`
  - Email: `admin@festival.com`
  - Password: `123456`
### 📚 API Documentation

Once the application is running, you can access the interactive API documentation:

- **Swagger UI**: `https://localhost:5001/swagger`
- **OpenAPI JSON**: `https://localhost:5001/swagger/v1/swagger.json`

### 🛠️ Development Commands

```bash
# Clean build artifacts
dotnet clean

# Run in development mode with hot reload
dotnet watch run

# Run tests (if available)
dotnet test

# Publish for production
dotnet publish -c Release -o ./publish
```

### 📁 Project Structure

```
├── Domain/                 # Domain entities
│   ├── Article.cs
│   ├── Comment.cs
│   ├── Person.cs
│   └── ...
├── Features/              # Feature-based organization
│   ├── Articles/
│   ├── Comments/
│   ├── Profiles/
│   └── ...
├── Infrastructure/        # Data access and external services
├── Program.cs            # Application entry point
├── ServicesExtensions.cs # Service configuration
└── festival.db          # SQLite database file
```

### 🔧 Configuration

The application uses standard ASP.NET Core configuration sources:
- `appsettings.json`
- `appsettings.Development.json`
- Environment variables
- Command line arguments

### 🌟 Key Features

- **Article Management**: Create, read, update, delete articles
- **Comment System**: Add comments to articles
- **User Authentication**: JWT-based authentication
- **User Profiles**: User profile management
- **Follow System**: Follow/unfollow other users
- **Favorites**: Favorite/unfavorite articles
- **Tag System**: Organize articles with tags

### 🚀 Production Deployment

1. **Build for production**:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Run the published application**:
   ```bash
   cd publish
   dotnet Conduit.dll
   ```

### 🤝 API Endpoints

The API follows RESTful conventions. Key endpoints include:

- `GET /api/articles` - Get articles
- `POST /api/articles` - Create article
- `GET /api/articles/{slug}` - Get article by slug
- `POST /api/articles/{slug}/comments` - Add comment
- `POST /api/users/login` - User login
- `POST /api/users` - User registration

See the Swagger documentation for complete API reference.

### 🔍 Troubleshooting

**Common Issues:**

1. **Port already in use**: Change the port in `launchSettings.json`
2. **Database issues**: Delete `festival.db` to recreate the database
3. **Package restore issues**: Run `dotnet clean` then `dotnet restore`

**Logs**: Check the console output for detailed error messages and logs.

### 📝 Notes

- The project uses Entity Framework Core with SQLite
- Authentication is implemented using JWT tokens
- The API follows the RealWorld specification
- CORS is configured for development

### 🛡️ Security

- JWT token authentication
- Password hashing
- Input validation
- CORS policy configuration

---

For more information about the RealWorld project, visit: https://realworld.io/