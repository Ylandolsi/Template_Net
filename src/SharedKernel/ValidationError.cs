namespace SharedKernel;

public sealed record ValidationError : Error
{
    public ValidationError(Error[] errors)
        : base(
            "Validation.General",
            "One or more validation errors occurred",
            ErrorType.Validation)
    {
        Errors = errors;
    }

    public Error[] Errors { get; }

    public static ValidationError FromResults(IEnumerable<Result> results) =>
        new(results.Where(r => r.IsFailure).Select(r => r.Error).ToArray());
}

/* 
// In your domain service
public Result<User> CreateUser(string email, string name)
{
    var errors = new List<ValidationError>();
    
    if (string.IsNullOrEmpty(email))
        errors.Add(new ValidationError("Email", "Email is required"));
    
    if (string.IsNullOrEmpty(name))
        errors.Add(new ValidationError("Name", "Name is required"));
    
    if (errors.Any())
    {
        return Result.Failure<User>(
            Error.Validation("User.Validation", "User validation failed", errors)
        );
    }
    
    // Create user logic...
    return Result.Success(user);
}

[HttpPost]
public IResult CreateUser(CreateUserRequest request)
{
    var result = _userService.CreateUser(request.Email, request.Name);
    
    return result.Match(
        onSuccess: user => Results.Created($"/users/{user.Id}", user),
        onFailure: CustomResults.Problem  // This is where the magic happens
    );
}

{
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "User.Validation",
    "status": 400,
    "detail": "User validation failed",
    "errors": [
        {
            "propertyName": "Email",
            "errorMessage": "Email is required"
        },
        {
            "propertyName": "Name", 
            "errorMessage": "Name is required"
        }
    ]
}
*/ 