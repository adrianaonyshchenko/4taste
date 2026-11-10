using System;
using System.Collections.Generic;
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
        private readonly IRoomRepository _roomRepository;
        private readonly ILogger _logger;

        public RoomService(IRoomRepository roomRepository, ILogger logger)
        {
            _roomRepository = roomRepository;
            _logger = logger;
        }

        public async Task<Result<RoomDto>> CreateRoomAsync(CreateRoomRequest request)
        {
            var currentUser = SessionContext.CurrentUser;
            if (currentUser is null || currentUser.ID <= 0)
            {
                _logger.Warning("Спроба створення кімнати неавторизованим користувачем.");
                return Result<RoomDto>.Failure("Увійдіть в акаунт, щоб створити кімнату.");
            }

            if (request is null)
            {
                _logger.Warning("Помилка валідації: запит на створення кімнати порожній.");
                return Result<RoomDto>.Failure("Дані кімнати не задано.");
            }

            string name = request.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.Warning("Помилка валідації: назва кімнати порожня.");
                return Result<RoomDto>.Failure("Назва кімнати не може бути порожньою.");
            }

            if (name.Length > RoomOptions.MaxNameLength)
            {
                _logger.Warning("Помилка валідації: назва кімнати занадто довга ({Length} символів).", name.Length);
                return Result<RoomDto>.Failure($"Назва кімнати не може перевищувати {RoomOptions.MaxNameLength} символів.");
            }

            string? description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            if (description != null && description.Length > RoomOptions.MaxDescriptionLength)
            {
                _logger.Warning("Помилка валідації: опис кімнати занадто довгий ({Length} символів).", description.Length);
                return Result<RoomDto>.Failure($"Опис кімнати не може перевищувати {RoomOptions.MaxDescriptionLength} символів.");
            }

            try
            {
                string inviteCode = await GenerateUniqueInviteCodeAsync();

                var room = new Room
                {
                    Name = name,
                    Description = description,
                    OwnerId = currentUser.ID,
                    InviteCode = inviteCode,
                    CreatedAt = DateTime.UtcNow,
                    Members = new List<RoomMember>
                    {
                        new RoomMember
                        {
                            UserId = currentUser.ID,
                            Role = RoomRole.Owner,
                            JoinedAt = DateTime.UtcNow
                        }
                    }
                };

                var createdRoom = await _roomRepository.CreateRoomAsync(room);

                _logger.Information(
                    "Користувач {UserId} успішно створив кімнату {RoomId} з кодом {InviteCode}.",
                    currentUser.ID,
                    createdRoom.Id,
                    createdRoom.InviteCode);

                var dto = new RoomDto
                {
                    Id = createdRoom.Id,
                    Name = createdRoom.Name,
                    Description = createdRoom.Description,
                    OwnerId = createdRoom.OwnerId,
                    InviteCode = createdRoom.InviteCode,
                    CreatedAt = createdRoom.CreatedAt
                };

                return Result<RoomDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Помилка при створенні кімнати користувачем {UserId}.", currentUser.ID);
                return Result<RoomDto>.Failure("Виникла технічна помилка при збереженні кімнати. Спробуйте пізніше.");
            }
        }

        public async Task<Result<List<RoomDto>>> GetUserRoomsAsync(int userId)
        {
            if (userId <= 0)
            {
                return Result<List<RoomDto>>.Success(new List<RoomDto>());
            }

            try
            {
                var rooms = await _roomRepository.GetUserRoomsAsync(userId);
                var dtos = rooms.Select(r => new RoomDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    OwnerId = r.OwnerId,
                    InviteCode = r.InviteCode,
                    CreatedAt = r.CreatedAt
                }).ToList();

                return Result<List<RoomDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Помилка при отриманні кімнат користувача {UserId}.", userId);
                return Result<List<RoomDto>>.Failure("Не вдалося завантажити список кімнат.");
            }
        }

        public async Task<Result<RoomDetailsDto>> GetRoomDetailsAsync(int roomId, int userId)
        {
            try
            {
                var room = await _roomRepository.GetByIdAsync(roomId);
                if (room is null)
                {
                    return Result<RoomDetailsDto>.Failure("Кімнату не знайдено.");
                }

                var memberUserIds = room.Members.Select(m => m.UserId).Concat(new[] { room.OwnerId }).Distinct();
                var userNames = await _roomRepository.GetUserNamesAsync(memberUserIds);

                var members = room.Members.Select(m => new RoomMemberDto
                {
                    UserId = m.UserId,
                    Name = userNames.TryGetValue(m.UserId, out var name) ? name : $"Користувач #{m.UserId}",
                    Role = m.Role == RoomRole.Owner ? "Власник" : "Учасник"
                }).ToList();

                var dto = new RoomDetailsDto
                {
                    Id = room.Id,
                    Name = room.Name,
                    Description = room.Description,
                    OwnerId = room.OwnerId,
                    InviteCode = room.InviteCode,
                    IsOwner = room.OwnerId == userId,
                    Members = members
                };

                return Result<RoomDetailsDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Помилка при отриманні деталей кімнати {RoomId}.", roomId);
                return Result<RoomDetailsDto>.Failure("Не вдалося завантажити інформацію про кімнату.");
            }
        }

        public async Task<Result> DeleteRoomAsync(int roomId, int userId)
        {
            if (userId <= 0)
            {
                return Result.Failure("Увійдіть в акаунт.");
            }

            try
            {
                var room = await _roomRepository.GetByIdAsync(roomId);
                if (room is null)
                {
                    return Result.Failure("Кімнату не знайдено.");
                }

                if (room.OwnerId != userId)
                {
                    return Result.Failure("Тільки власник може видалити кімнату.");
                }

                await _roomRepository.DeleteRoomAsync(roomId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Помилка при видаленні кімнати {RoomId}.", roomId);
                return Result.Failure("Не вдалося видалити кімнату.");
            }
        }

        public async Task<Result> LeaveRoomAsync(int roomId, int userId)
        {
            if (userId <= 0)
            {
                return Result.Failure("Увійдіть в акаунт.");
            }

            try
            {
                var room = await _roomRepository.GetByIdAsync(roomId);
                if (room is null)
                {
                    return Result.Failure("Кімнату не знайдено.");
                }

                if (room.OwnerId == userId)
                {
                    return Result.Failure("Власник не може покинути кімнату. Її можна тільки видалити.");
                }

                await _roomRepository.RemoveMemberAsync(roomId, userId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Помилка при виході з кімнати {RoomId}.", roomId);
                return Result.Failure("Не вдалося вийти з кімнати.");
            }
        }

        private async Task<string> GenerateUniqueInviteCodeAsync()
        {
            const int maxRetries = 10;
            for (int i = 0; i < maxRetries; i++)
            {
                string code = GenerateRandomCode(RoomOptions.InviteCodeLength);
                bool exists = await _roomRepository.InviteCodeExistsAsync(code);
                if (!exists)
                {
                    return code;
                }
            }

            throw new InvalidOperationException("Не вдалося згенерувати унікальний код кімнати.");
        }

        private string GenerateRandomCode(int length)
        {
            var chars = RoomOptions.InviteCodeCharacters;
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[bytes[i] % chars.Length]);
            }
            return sb.ToString();
        }
    }
}
