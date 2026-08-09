using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Auth.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using LawConnect.Shared.Options;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace LawConnect.Application.Features.Auth.Commands.RequestOtp;

/// <summary>
/// Handles OTP request for passwordless login.
/// </summary>
public sealed class RequestOtpCommandHandler : IRequestHandler<RequestOtpCommand, Result<RequestOtpResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IOtpGenerator _otpGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly ISmsService _smsService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IDistributedCache _cache;
    private readonly OtpOptions _otpOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestOtpCommandHandler"/> class.
    /// </summary>
    public RequestOtpCommandHandler(
        IApplicationDbContext dbContext,
        IOtpGenerator otpGenerator,
        ITokenHasher tokenHasher,
        ISmsService smsService,
        IDateTimeProvider dateTimeProvider,
        IDistributedCache cache,
        IOptions<OtpOptions> otpOptions)
    {
        _dbContext = dbContext;
        _otpGenerator = otpGenerator;
        _tokenHasher = tokenHasher;
        _smsService = smsService;
        _dateTimeProvider = dateTimeProvider;
        _cache = cache;
        _otpOptions = otpOptions.Value;
    }

    /// <inheritdoc />
    public async Task<Result<RequestOtpResponse>> Handle(
        RequestOtpCommand request,
        CancellationToken cancellationToken)
    {
        var phoneNumber = request.PhoneNumber.Trim();

        await EnsureWithinRateLimitAsync(phoneNumber, cancellationToken);

        var utcNow = _dateTimeProvider.UtcNow;
        var expiresAt = utcNow.AddMinutes(_otpOptions.ExpirationMinutes);
        var code = _otpGenerator.GenerateNumericCode();
        var codeHash = _tokenHasher.Hash(code);

        var existingOtps = await _dbContext.OtpVerifications
            .Where(o =>
                o.Destination == phoneNumber
                && o.Purpose == OtpPurpose.Login
                && !o.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var otp in existingOtps)
        {
            otp.IsUsed = true;
            otp.UsedAtUtc = utcNow;
        }

        _dbContext.OtpVerifications.Add(new OtpVerification
        {
            Destination = phoneNumber,
            CodeHash = codeHash,
            Purpose = OtpPurpose.Login,
            ExpiresAtUtc = expiresAt,
            MaxAttempts = _otpOptions.MaxAttempts,
            IsUsed = false,
            AttemptCount = 0
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _smsService.SendAsync(
            phoneNumber,
            $"Your LawConnect verification code is {code}. It expires in {_otpOptions.ExpirationMinutes} minutes.",
            cancellationToken);

        await IncrementRateLimitAsync(phoneNumber, cancellationToken);

        var response = new RequestOtpResponse(
            MaskPhone(phoneNumber),
            expiresAt,
            _otpOptions.ExpirationMinutes * 60);

        return Result.Success(response);
    }

    private async Task EnsureWithinRateLimitAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        var cacheKey = AuthConstants.OtpRateLimitCachePrefix + phoneNumber;
        var currentValue = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (int.TryParse(currentValue, out var count) && count >= _otpOptions.MaxRequestsPerWindow)
        {
            throw new BusinessRuleException(
                "Too many OTP requests. Please try again later.",
                "Auth.OtpRateLimited");
        }
    }

    private async Task IncrementRateLimitAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        var cacheKey = AuthConstants.OtpRateLimitCachePrefix + phoneNumber;
        var currentValue = await _cache.GetStringAsync(cacheKey, cancellationToken);
        var count = int.TryParse(currentValue, out var existing) ? existing + 1 : 1;

        await _cache.SetStringAsync(
            cacheKey,
            count.ToString(),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_otpOptions.RequestWindowMinutes)
            },
            cancellationToken);
    }

    private static string MaskPhone(string phoneNumber)
    {
        if (phoneNumber.Length <= 4)
        {
            return "****";
        }

        return string.Concat(new string('*', phoneNumber.Length - 4), phoneNumber[^4..]);
    }
}
