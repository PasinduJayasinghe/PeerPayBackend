# PeerPay Backend - Comprehensive Technical Report

## 1. PROJECT OVERVIEW

**PeerPay** is a student job marketplace platform backend built with .NET 8.0, following clean architecture principles and CQRS pattern. The system connects students seeking part-time work with employers offering opportunities, handling job postings, applications, payments, messaging, and ratings.

**Technology Stack:**
- .NET 8.0 Web API
- Entity Framework Core 8.0
- SQL Server
- MediatR (CQRS)
- Serilog (Logging)
- JWT Authentication
- Stripe Payment Integration

---

## 2. SOLUTION ARCHITECTURE

### 2.1 Project Structure (Clean Architecture)

The solution follows **Clean Architecture** with clear separation of concerns across 4 layers:

```
PeerPayBackend/
├── Domain/              # Core business entities & enums
├── Application/         # Business logic, commands, queries
├── Infrastructure/      # Data access, external services
└── PeerPayBackend/      # API layer, controllers, middleware
```

**Dependency Flow:** 
```
API → Application → Domain
       ↓
   Infrastructure → Domain
```

### 2.2 Domain Layer (Core)

**Purpose:** Contains enterprise business rules, entities, enums, and events. No external dependencies.

**Key Components:**

**Entities (CoreClasses.cs):**
```csharp
public class User
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Name { get; set; }
    public string PasswordHash { get; set; }
    public UserType UserType { get; set; }
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public Profile Profile { get; set; }
}

public class Job
{
    public string JobId { get; set; }
    public string EmployerId { get; set; }
    public string? CategoryId { get; set; }
    public string Title { get; set; }
    public decimal PayAmount { get; set; }
    public PayType PayType { get; set; }
    public JobType JobType { get; set; }
    public JobStatus Status { get; set; }
    public DateTime PostedDate { get; set; }
}
```

**Enums (CoreEnums.cs):**
```csharp
public enum UserType { Student = 1, Employer = 2, Admin = 3 }
public enum UserStatus { Active = 1, Inactive = 2, Suspended = 3 }
public enum JobStatus { Active = 1, Closed = 2, Cancelled = 3 }
public enum PayType { Hourly = 1, Daily = 2, Weekly = 3, Monthly = 4, Fixed = 5 }
public enum JobType { FullTime = 1, PartTime = 2, ProjectBased = 3, Freelance = 4 }
public enum ApplicationStatus { Pending = 1, Accepted = 2, Rejected = 3 }
```

**Events (CoreEvents.cs):**
```csharp
public class UserRegisteredEvent
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public UserType UserType { get; set; }
    public DateTime RegisteredAt { get; set; }
}
```

---

## 3. APPLICATION LAYER (Business Logic)

### 3.1 CQRS Pattern with MediatR

**CQRS (Command Query Responsibility Segregation)** separates read and write operations for better scalability and maintainability.

**Configuration in Program.cs:**
```csharp
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
    typeof(Application.Commands.StudentCommand.RegisterStudentCommand).Assembly));
```

### 3.2 Commands (Write Operations)

**Command Example - CreateJobCommand:**
```csharp
public class CreateJobCommand : IRequest<JobDto>
{
    public string EmployerId { get; set; }
    public string? CategoryId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal PayAmount { get; set; }
    public PayType PayType { get; set; }
    public int DurationDays { get; set; }
    public JobType JobType { get; set; }
    public int MaxApplicants { get; set; }
}
```

**Command Handler with Logging:**
```csharp
public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, JobDto>
{
    private readonly IJobRepository _jobRepository;
    private readonly IEmployerRepository _employerRepository;
    private readonly ILogger<CreateJobCommandHandler> _logger;

    public async Task<JobDto> Handle(CreateJobCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Creating job: {Title} for employer: {EmployerId}", 
            request.Title, request.EmployerId);
        
        var employer = await _employerRepository.GetByUserIdAsync(request.EmployerId);
        if (employer == null)
        {
            _logger.LogWarning("Job creation failed: Employer {EmployerId} not found", 
                request.EmployerId);
            throw new Exception("Employer not found");
        }

        var job = new Job
        {
            JobId = Guid.NewGuid().ToString(),
            EmployerId = employer.EmployerId,
            Title = request.Title,
            PayAmount = request.PayAmount,
            Status = JobStatus.Active,
            PostedDate = DateTime.UtcNow
        };

        await _jobRepository.AddAsync(job);
        
        _logger.LogInformation("Job created successfully: {JobId}", job.JobId);
        return MapToDto(job);
    }
}
```

### 3.3 FluentValidation Integration

**Validation Example:**
```csharp
public class CreateJobCommandValidator : AbstractValidator<CreateJobCommand>
{
    public CreateJobCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Job title is required")
            .MaximumLength(300).WithMessage("Title cannot exceed 300 characters");
            
        RuleFor(x => x.PayAmount)
            .GreaterThan(0).WithMessage("Pay amount must be greater than 0");
            
        RuleFor(x => x.DurationDays)
            .GreaterThan(0).WithMessage("Duration must be at least 1 day")
            .LessThanOrEqualTo(365).WithMessage("Duration cannot exceed 365 days");
    }
}
```

### 3.4 Queries (Read Operations)

**Query Example:**
```csharp
public class GetJobByIdQuery : IRequest<JobDto>
{
    public string JobId { get; set; }
}

public class GetJobByIdQueryHandler : IRequestHandler<GetJobByIdQuery, JobDto>
{
    private readonly IJobRepository _jobRepository;

    public async Task<JobDto> Handle(GetJobByIdQuery request, CancellationToken ct)
    {
        var job = await _jobRepository.GetByIdAsync(request.JobId);
        if (job == null) throw new KeyNotFoundException("Job not found");
        return MapToDto(job);
    }
}
```

### 3.5 Repository Pattern (Interfaces)

**Application Layer Interfaces:**
```csharp
public interface IUserRepository
{
    Task<User> GetByIdAsync(string userId);
    Task<User> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task<User> AddAsync(User user);
    Task UpdateAsync(User user);
}

public interface IJobRepository
{
    Task<Job> AddAsync(Job job);
    Task<Job> GetByIdAsync(string jobId);
    Task<List<Job>> GetActiveJobsAsync();
    Task<List<Job>> GetJobsByEmployerAsync(string employerId);
    Task UpdateAsync(Job job);
}
```

---

## 4. INFRASTRUCTURE LAYER (Data & External Services)

### 4.1 Entity Framework Core Configuration

**DbContext (PeerPayDbContext.cs):**
```csharp
public class PeerPayDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Employer> Employers { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.HasOne(e => e.Profile)
                .WithOne(p => p.User)
                .HasForeignKey<Profile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Job Configuration with Optional Category
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.JobId);
            entity.Property(e => e.CategoryId).HasMaxLength(50);
            entity.Property(e => e.PayAmount).HasColumnType("decimal(18,2)");
            
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Jobs)
                .HasForeignKey(e => e.CategoryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasMany(e => e.Applications)
                .WithOne(a => a.Job)
                .HasForeignKey(a => a.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

**Connection String (appsettings.json):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=DESKTOP-MJL437C\\SQLEXPRESS01;Database=peerPay;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 4.2 Repository Implementation

**Example - UserRepository:**
```csharp
public class UserRepository : IUserRepository
{
    private readonly PeerPayDbContext _context;

    public UserRepository(PeerPayDbContext context)
    {
        _context = context;
    }

    public async Task<User> GetByIdAsync(string userId)
    {
        return await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<User> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }
}
```

### 4.3 External Service Integrations

**Password Hashing Service:**
```csharp
public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

**JWT Token Service:**
```csharp
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.UserType.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

**Stripe Payment Service:**
```csharp
public class StripeService : IStripeService
{
    public StripeService(IConfiguration configuration)
    {
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
    }

    public async Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string currency)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100),
            Currency = currency,
            PaymentMethodTypes = new List<string> { "card" }
        };

        var service = new PaymentIntentService();
        return await service.CreateAsync(options);
    }
}
```

---

## 5. API LAYER (Presentation)

### 5.1 Controller Design

**RESTful API Controller Example:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class JobController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<JobController> _logger;

    public JobController(IMediator mediator, ILogger<JobController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new job posting
    /// POST /api/job
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<JobDto>> CreateJob([FromBody] CreateJobCommand command)
    {
        try
        {
            _logger.LogInformation("Creating job: {Title}", command.Title);
            var result = await _mediator.Send(command);
            _logger.LogInformation("Job created successfully: {JobId}", result.JobId);
            return CreatedAtAction(nameof(GetJobById), new { id = result.JobId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job: {Title}", command.Title);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get job by ID
    /// GET /api/job/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<JobDto>> GetJobById(string id)
    {
        var query = new GetJobByIdQuery { JobId = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
```

### 5.2 Authentication Flow

**Login Process:**
```csharp
[HttpPost("login")]
public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginUserCommand command)
{
    try
    {
        _logger.LogInformation("Login attempt for user: {EmailOrPhone}", command.EmailOrPhone);
        var result = await _mediator.Send(command);
        _logger.LogInformation("User logged in successfully: {Email}", result.User.Email);
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Login failed for user: {EmailOrPhone}", command.EmailOrPhone);
        return BadRequest(new { error = ex.Message });
    }
}
```

**LoginResponseDto:**
```csharp
public class LoginResponseDto
{
    public string Token { get; set; }
    public UserDto User { get; set; }
    public UserType UserType { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

---

## 6. DESIGN PATTERNS IMPLEMENTED

### 6.1 CQRS (Command Query Responsibility Segregation)
- **Commands:** Modify state (RegisterUser, CreateJob, ApplyToJob)
- **Queries:** Read data (GetJobById, GetUserProfile)
- **Benefits:** Scalability, optimized read/write models, clear separation

### 6.2 Repository Pattern
- Abstracts data access logic
- Easy to mock for testing
- Database-agnostic application layer

### 6.3 Mediator Pattern (via MediatR)
- Decouples controllers from business logic
- Centralized request/response pipeline
- Easy to add cross-cutting concerns (validation, logging)

### 6.4 Dependency Injection
```csharp
// Program.cs - Service Registration
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
```

### 6.5 Factory Pattern (Implicit)
- Entity creation through constructors
- GUID generation for IDs

### 6.6 Strategy Pattern
- Different payment types (Hourly, Fixed, Monthly)
- Different job types (FullTime, PartTime, ProjectBased)

---

## 7. LOGGING IMPLEMENTATION (Serilog)

### 7.1 Configuration

**appsettings.json:**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "Logs/peerpay-.txt",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

### 7.2 Startup Configuration

**Program.cs:**
```csharp
// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting PeerPay Backend API");
    
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    
    var app = builder.Build();
    app.UseSerilogRequestLogging();
    
    Log.Information("PeerPay Backend API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}
```

### 7.3 Global Exception Handling

**ExceptionHandlingMiddleware:**
```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = exception switch
        {
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException => HttpStatusCode.NotFound,
            ArgumentException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        var result = JsonSerializer.Serialize(new
        {
            error = exception.Message,
            statusCode = (int)code
        });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        return context.Response.WriteAsync(result);
    }
}
```

---

## 8. KEY LIBRARIES & PACKAGES

### 8.1 Core Frameworks
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
```

### 8.2 CQRS & Validation
```xml
<PackageReference Include="MediatR" Version="12.5.0" />
<PackageReference Include="FluentValidation" Version="8.0.0" />
```

### 8.3 Logging
```xml
<PackageReference Include="Serilog" Version="4.3.0" />
<PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
<PackageReference Include="Serilog.Settings.Configuration" Version="9.0.0" />
```

### 8.4 Authentication
```xml
<PackageReference Include="Microsoft.IdentityModel.Tokens" Version="8.14.0" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.14.0" />
```

### 8.5 API Documentation
```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
```

---

## 9. API ENDPOINTS STRUCTURE

### 9.1 User Management
```
POST   /api/user/login                  # User login
GET    /api/user/{id}                   # Get user by ID
GET    /api/user/email/{email}          # Get user by email
PUT    /api/user/{id}                   # Update user profile
PUT    /api/user/{id}/password          # Change password
```

### 9.2 Student Operations
```
POST   /api/student/register            # Register student
GET    /api/student/{id}                # Get student profile
PUT    /api/student/{id}                # Update student profile
GET    /api/student/{id}/applications   # Get student applications
```

### 9.3 Employer Operations
```
POST   /api/employer/register           # Register employer
GET    /api/employer/{id}/jobs          # Get employer's jobs
```

### 9.4 Job Management
```
POST   /api/job                         # Create job
GET    /api/job/{id}                    # Get job details
PUT    /api/job/{id}                    # Update job
DELETE /api/job/{id}                    # Delete job
GET    /api/job/active                  # Get active jobs
GET    /api/job/employer/{id}           # Get jobs by employer
```

### 9.5 Job Applications
```
POST   /api/jobapplication              # Apply to job
PUT    /api/jobapplication/{id}/status  # Update application status
GET    /api/jobapplication/job/{id}     # Get job applications
```

---

## 10. DATA TRANSFER OBJECTS (DTOs)

### 10.1 Response DTOs
```csharp
public class UserResponseDto
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public UserType UserType { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class JobDto
{
    public string JobId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal PayAmount { get; set; }
    public PayType PayType { get; set; }
    public JobType JobType { get; set; }
    public JobStatus Status { get; set; }
    public DateTime PostedDate { get; set; }
    public DateTime? Deadline { get; set; }
}
```

---

## 11. SECURITY FEATURES

### 11.1 Password Security
- BCrypt hashing algorithm
- Salt automatically generated
- One-way encryption

### 11.2 JWT Authentication
- Token-based authentication
- 24-hour expiration
- Role-based claims
- Secure HMAC-SHA256 signing

### 11.3 CORS Configuration
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5176")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

### 11.4 Data Validation
- FluentValidation for input validation
- Email uniqueness constraints
- Phone number uniqueness
- Required field validation

---

## 12. DATABASE DESIGN HIGHLIGHTS

### 12.1 Key Relationships
- **User ↔ Profile:** One-to-One (Cascade delete)
- **User → Student/Employer:** One-to-One inheritance (Cascade delete)
- **Employer → Jobs:** One-to-Many (Restrict delete)
- **Job → Applications:** One-to-Many (Cascade delete)
- **Job ↔ Category:** Many-to-One (Optional, SetNull on delete)

### 12.2 Unique Constraints
- User.Email (Unique index)
- User.Phone (Unique index)
- JobCategory.Name (Unique)
- Rating: JobId + RaterId + RatedUserId (Composite unique)

### 12.3 Decimal Precision
```csharp
entity.Property(e => e.PayAmount).HasColumnType("decimal(18,2)");
entity.Property(e => e.Rating).HasColumnType("decimal(3,2)");
```

---

## 13. ERROR HANDLING STRATEGY

### 13.1 Layered Error Handling
1. **Validation Layer:** FluentValidation catches input errors
2. **Business Logic Layer:** Commands throw domain exceptions
3. **Middleware Layer:** Global exception handler catches unhandled exceptions
4. **Controller Layer:** Try-catch for graceful error responses

### 13.2 Error Response Format
```json
{
  "error": "Job not found",
  "statusCode": 404
}
```

---

## 14. SCALABILITY CONSIDERATIONS

### 14.1 CQRS Benefits
- Separate read/write databases possible
- Independent scaling of read vs write operations
- Optimized query models

### 14.2 Repository Pattern
- Easy to switch data sources
- Can add caching layer
- Unit of Work pattern ready

### 14.3 Async/Await Throughout
- All database operations async
- Non-blocking I/O
- Better resource utilization

---

## 15. TESTING CONSIDERATIONS

### 15.1 Testable Architecture
- Dependency injection enables mocking
- Repository pattern abstracts data layer
- CQRS separates concerns
- Interface-based design

### 15.2 Mock Example
```csharp
var mockRepo = new Mock<IJobRepository>();
mockRepo.Setup(r => r.GetByIdAsync("job1"))
    .ReturnsAsync(new Job { JobId = "job1", Title = "Test Job" });

var handler = new GetJobByIdQueryHandler(mockRepo.Object);
```

---

## 16. CONCLUSION

PeerPay Backend demonstrates professional enterprise-level architecture with:

✅ **Clean Architecture** - Clear separation of concerns  
✅ **CQRS Pattern** - Scalable read/write separation  
✅ **Repository Pattern** - Abstracted data access  
✅ **Dependency Injection** - Loose coupling, testability  
✅ **Comprehensive Logging** - Serilog with structured logging  
✅ **Security** - JWT authentication, password hashing  
✅ **Validation** - FluentValidation for input checking  
✅ **Error Handling** - Global exception middleware  
✅ **Payment Integration** - Stripe for transactions  
✅ **Modern .NET 8.0** - Latest framework features  

The solution is production-ready, maintainable, scalable, and follows industry best practices for enterprise web API development.

---

## Appendix A: Project Statistics

**Total Projects:** 4 (Domain, Application, Infrastructure, PeerPayBackend)  
**Target Framework:** .NET 8.0  
**Database:** SQL Server (peerPay)  
**Total Entities:** 20+ domain entities  
**Total Controllers:** 10+ API controllers  
**Total Commands:** 15+ CQRS commands  
**Total Queries:** 10+ CQRS queries  
**Total Repositories:** 10+ repository interfaces and implementations  

---

## Appendix B: Development Environment

**IDE:** Visual Studio Code / Visual Studio 2022  
**Database Management:** SQL Server Management Studio  
**API Testing:** Swagger UI, Postman  
**Version Control:** Git  
**Package Manager:** NuGet  

---

*Document Generated: October 26, 2025*  
*Project: PeerPay Backend*  
*Version: 1.0*
