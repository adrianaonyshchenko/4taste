using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;
using Serilog;

namespace FourTaste.ApplicationCore.Services
{
    public class RoomService : IRoomService
    {
        private const string NotAuthenticatedError = "Увійдіть в акаунт.";
        private const string RoomNotFoundError = "Кімнату не знайдено.";

        private readonly IRoomRepository _roomRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger _logger;

        public RoomService(IRoomRepository roomRepository, ICurrentUserService currentUser, ILogger logger)
        {
            _roomRepository = roomRepository;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<RoomDto>> CreateRoomAsync(CreateRoomRequest request)
        {
            if (!_currentUser.IsAuthenticated)
            {
                _logger.Warning("Спроба створення кімнати неавторизованим користувачем.");
                return Result<RoomDto>.Failure("Увійдіть в акаунт, щоб створити кімнату.");
            }

            var validation = CreateRoomRequestValidator.Validate(request);
            if (validation.IsFailure)
            {
                _logger.Warning("Помилка валідації кімнати: {Error}", validation.Error);
                return Result<RoomDto>.Failure(validation.Error!);
            }

            int userId = _currentUser.UserId;
            var room = new Room
            {
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                OwnerId = userId,
                InviteCode = await GenerateUniqueInviteCodeAsync(),
                CreatedAt = DateTime.UtcNow,
                Members = new List<RoomMember>
                {
                    new RoomMember { UserId = userId, Role = RoomRole.Owner, JoinedAt = DateTime.UtcNow },
                },
            };

            var createdRoom = await _roomRepository.CreateRoomAsync(room);

            _logger.Information(
                "Користувач {UserId} успішно створив кімнату {RoomId} з кодом {InviteCode}.",
                userId,
                createdRoom.Id,
                createdRoom.InviteCode);

            return Result<RoomDto>.Success(ToDto(createdRoom));
        }

        public async Task<Result<List<RoomDto>>> GetUserRoomsAsync()
        {
            if (!_currentUser.IsAuthenticated)
            {
                return Result<List<RoomDto>>.Success(new List<RoomDto>());
            }

            var rooms = await _roomRepository.GetUserRoomsAsync(_currentUser.UserId);
            return Result<List<RoomDto>>.Success(rooms.Select(ToDto).ToList());
        }

        public async Task<Result<RoomDetailsDto>> GetRoomDetailsAsync(int roomId)
        {
            var room = await _roomRepository.GetByIdAsync(roomId);
            if (room is null)
            {
                return Result<RoomDetailsDto>.Failure(RoomNotFoundError);
            }

            var memberUserIds = room.Members.Select(m => m.UserId).Append(room.OwnerId).Distinct();
            var userNames = await _roomRepository.GetUserNamesAsync(memberUserIds);

            var members = room.Members.Select(m => new RoomMemberDto
            {
                UserId = m.UserId,
                Name = userNames.TryGetValue(m.UserId, out var name) ? name : $"Користувач #{m.UserId}",
                Role = m.Role == RoomRole.Owner ? "Власник" : "Учасник",
            }).ToList();

            return Result<RoomDetailsDto>.Success(new RoomDetailsDto
            {
                Id = room.Id,
                Name = room.Name,
                Description = room.Description,
                OwnerId = room.OwnerId,
                InviteCode = room.InviteCode,
                IsOwner = room.OwnerId == _currentUser.UserId,
                Members = members,
            });
        }

        public async Task<Result> DeleteRoomAsync(int roomId)
        {
            var check = await GetRoomForCurrentUserAsync(roomId);
            if (check.IsFailure)
            {
                return check;
            }

            if (check.Value!.OwnerId != _currentUser.UserId)
            {
                return Result.Failure("Тільки власник може видалити кімнату.");
            }

            await _roomRepository.DeleteRoomAsync(roomId);
            return Result.Success();
        }

        public async Task<Result> LeaveRoomAsync(int roomId)
        {
            var check = await GetRoomForCurrentUserAsync(roomId);
            if (check.IsFailure)
            {
                return check;
            }

            if (check.Value!.OwnerId == _currentUser.UserId)
            {
                return Result.Failure("Власник не може покинути кімнату. Її можна тільки видалити.");
            }

            await _roomRepository.RemoveMemberAsync(roomId, _currentUser.UserId);
            return Result.Success();
        }

        public async Task<Result<RoomDto>> JoinRoomByCodeAsync(string inviteCode)
        {
            if (!_currentUser.IsAuthenticated)
            {
                _logger.Warning("Спроба приєднання до кімнати неавторизованим користувачем.");
                return Result<RoomDto>.Failure("Увійдіть в акаунт, щоб приєднатися до кімнати.");
            }

            string code = inviteCode?.Trim().ToUpperInvariant() ?? string.Empty;
            if (code.Length == 0)
            {
                _logger.Warning("Помилка валідації: код запрошення порожній.");
                return Result<RoomDto>.Failure("Введіть код запрошення.");
            }

            var room = await _roomRepository.GetByInviteCodeAsync(code);
            if (room is null)
            {
                _logger.Warning("Кімнату з кодом {InviteCode} не знайдено.", code);
                return Result<RoomDto>.Failure("Кімнату з таким кодом не знайдено.");
            }

            int userId = _currentUser.UserId;
            if (room.OwnerId == userId || room.Members.Any(m => m.UserId == userId))
            {
                return Result<RoomDto>.Failure("Ви вже є учасником цієї кімнати.");
            }

            await _roomRepository.AddMemberAsync(new RoomMember
            {
                RoomId = room.Id,
                UserId = userId,
                Role = RoomRole.Member,
                JoinedAt = DateTime.UtcNow,
            });

            _logger.Information("Користувач {UserId} успішно приєднався до кімнати {RoomId} за кодом.", userId, room.Id);

            return Result<RoomDto>.Success(ToDto(room));
        }

        private static RoomDto ToDto(Room room) => new RoomDto
        {
            Id = room.Id,
            Name = room.Name,
            Description = room.Description,
            OwnerId = room.OwnerId,
            InviteCode = room.InviteCode,
            CreatedAt = room.CreatedAt,
        };

        private static string GenerateRandomCode(int length)
        {
            var chars = RoomOptions.InviteCodeCharacters;
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            var sb = new StringBuilder(length);
            foreach (byte b in bytes)
            {
                sb.Append(chars[b % chars.Length]);
            }

            return sb.ToString();
        }

        private async Task<Result<Room>> GetRoomForCurrentUserAsync(int roomId)
        {
            if (!_currentUser.IsAuthenticated)
            {
                return Result<Room>.Failure(NotAuthenticatedError);
            }

            var room = await _roomRepository.GetByIdAsync(roomId);
            return room is null ? Result<Room>.Failure(RoomNotFoundError) : Result<Room>.Success(room);
        }

        private async Task<string> GenerateUniqueInviteCodeAsync()
        {
            const int maxRetries = 10;
            for (int i = 0; i < maxRetries; i++)
            {
                string code = GenerateRandomCode(RoomOptions.InviteCodeLength);
                if (!await _roomRepository.InviteCodeExistsAsync(code))
                {
                    return code;
                }
            }

            throw new InvalidOperationException("Не вдалося згенерувати унікальний код кімнати.");
        }
    }
}
