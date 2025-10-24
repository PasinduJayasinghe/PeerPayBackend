using Application.Dtos;
using Application.Interfaces;
using Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands.LoginCommand
{
    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, LoginResponseDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        public LoginUserCommandHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            ITokenService tokenService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<LoginResponseDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            // Try to find user by email or phone
            var user = await GetUserByEmailOrPhoneAsync(request.EmailOrPhone);

            if (user == null)
            {
                throw new Exception("Invalid credentials");
            }

            // Verify password
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new Exception("Invalid credentials");
            }

            // Check if user is active
            if (user.Status != UserStatus.Active && user.Status != UserStatus.PendingVerification)
            {
                throw new Exception($"Account is {user.Status.ToString().ToLower()}");
            }

            // Generate token
            var token = _tokenService.GenerateToken(user);
            var expiresAt = DateTime.UtcNow.AddHours(24);

            // Update last login
            await _userRepository.UpdateLastLoginAsync(user.UserId);

            return new LoginResponseDto
            {
                Token = token,
                User = new UserDto
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    Phone = user.Phone,
                    Name = user.Name,
                    UserType = user.UserType,
                    Status = user.Status,
                    IsVerified = user.IsVerified,
                    CreatedAt = user.CreatedAt
                },
                UserType = user.UserType,
                ExpiresAt = expiresAt
            };
        }

        private async Task<Domain.Classes.User> GetUserByEmailOrPhoneAsync(string emailOrPhone)
        {
            // Check if it's an email
            if (emailOrPhone.Contains("@"))
            {
                return await _userRepository.GetByEmailAsync(emailOrPhone.ToLower());
            }

            // Otherwise treat as phone
            var normalizedPhone = NormalizePhoneNumber(emailOrPhone);
            return await _userRepository.GetByPhoneAsync(normalizedPhone);
        }

        private string NormalizePhoneNumber(string phone)
        {
            if (phone.StartsWith("0"))
            {
                return "+94" + phone.Substring(1);
            }
            return phone;
        }
    }
}
