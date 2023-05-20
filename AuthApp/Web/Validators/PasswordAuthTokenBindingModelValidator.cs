using FluentValidation;
using Web.Models;

namespace Web.Validators
{
    public class PasswordAuthTokenBindingModelValidator : AbstractValidator<PasswordAuthTokenBindingModel>
    {
        public PasswordAuthTokenBindingModelValidator()
        {
            RuleFor(model => model.Email)
                .NotEmpty()
                .WithMessage("Provide your email.");

            RuleFor(model => model.Password)
                .NotEmpty()
                .WithMessage("Provide your password.");

            RuleFor(model => model.grant_type)
                .Equal("password")
                .WithMessage("Unsupported grant_type value.");
        }
    }
}
