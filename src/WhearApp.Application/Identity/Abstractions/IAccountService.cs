using FluentResults;

namespace WhearApp.Application.Identity.Abstractions;

public interface IAccountService
{
    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request);
    Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request);
    Task<Result> ResendEmailConfirmationAsync(ResendEmailConfirmationRequest request);
    Task<Result<UserInfo>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
}