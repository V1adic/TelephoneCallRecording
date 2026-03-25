using Microsoft.Extensions.Options;
using TelephoneCallRecording.Models.Authorization;
using TelephoneCallRecording.Services.Authorization.Email;
using TelephoneCallRecording.Services.Authorization.Lockout.Options;

public interface IUserVerificationService
{
    Task TriggerEmailVerificationAsync(User user);
    bool RequiresVerification(User user);
}

public class UserVerificationService : IUserVerificationService
{
    private readonly IConfirmationCodeGenerator _codeGenerator;
    private readonly IEmailService _emailService;
    private readonly IOptions<LockoutOptions> _options;

    public UserVerificationService(IConfirmationCodeGenerator codeGenerator, IEmailService emailService, IOptions<LockoutOptions> options)
    {
        _codeGenerator = codeGenerator;
        _emailService = emailService;
        _options = options;
    }

    public async Task TriggerEmailVerificationAsync(User user)
    {
        (string codeHash, string code) = _codeGenerator.Generate();

        user.IsEmailConfirmed = false;
        user.EmailConfirmationCodeHash = codeHash;
        user.EmailConfirmationExpires = DateTime.UtcNow.AddMinutes(_options.Value.CodeExpirationMinutes);

        await _emailService.SendConfirmationCodeAsync(user.Email, code);
    }

    public bool RequiresVerification(User user)
    {
        // TODO: здесь будет сложная логика на основе логов/2FA-политики
        return true; // или false, если уже подтверждён и т.д.
    }
}