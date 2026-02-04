using System.Data;
using System.Security.Cryptography;
using F1Predictions.Models;
using F1Predictions.Models.DTOs;
using F1Predictions.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace F1Predictions.Services
{
    public class LeaguesService : ILeaguesService
    {
        private readonly string _connectionString;

        public LeaguesService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<ServiceResult<LeagueDto>> CreateLeague(CreateLeagueDto dto, int ownerUserId)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                await using var transaction = connection.BeginTransaction();

                try
                {
                    // Get active championship ID
                    int? championshipId = await GetActiveChampionshipIdAsync(connection, transaction);
                    if (!championshipId.HasValue)
                    {
                        return ServiceResult<LeagueDto>.Fail("No active championship found.");
                    }

                    // Insert the league with default IsActive = 1, IsPublic = 0
                    var insertLeagueSql = @"
                        INSERT INTO Leagues (Name, Description, OwnerId, ChampionshipId, CreatedAt, IsPublic, IsActive)
                        OUTPUT INSERTED.Id
                        VALUES (@Name, @Description, @OwnerId, @ChampionshipId, GETUTCDATE(), 0, 1)";

                    int leagueId;
                    await using (var cmd = new SqlCommand(insertLeagueSql, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Name", dto.Name);
                        cmd.Parameters.AddWithValue("@Description", (object?)dto.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@OwnerId", ownerUserId);
                        cmd.Parameters.AddWithValue("@ChampionshipId", championshipId.Value);

                        var result = await cmd.ExecuteScalarAsync();
                        leagueId = Convert.ToInt32(result);
                    }

                    // Generate invite code and update the league
                    string inviteCode = GenerateInviteCode();
                    var updateInviteCodeSql = "UPDATE Leagues SET InviteCode = @InviteCode WHERE Id = @LeagueId";
                    await using (var cmd = new SqlCommand(updateInviteCodeSql, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@InviteCode", inviteCode);
                        cmd.Parameters.AddWithValue("@LeagueId", leagueId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Add owner as a member in LeagueMembers table with Role = 'Owner'
                    var insertMemberSql = @"
                        INSERT INTO LeagueMembers (LeagueId, UserId, Role, JoinedAt, IsActive)
                        VALUES (@LeagueId, @UserId, 'Owner', GETUTCDATE(), 1)";
                    await using (var cmd = new SqlCommand(insertMemberSql, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@LeagueId", leagueId);
                        cmd.Parameters.AddWithValue("@UserId", ownerUserId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Commit transaction
                    await transaction.CommitAsync();

                    // Fetch the created league with additional info
                    var leagueDto = await GetLeagueByIdAsync(connection, leagueId);
                    if (leagueDto == null)
                    {
                        return ServiceResult<LeagueDto>.Fail("League created but failed to retrieve details.");
                    }

                    return ServiceResult<LeagueDto>.Succeed(leagueDto, "League created successfully.");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return ServiceResult<LeagueDto>.Fail($"An error occurred while creating the league: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<LeagueDto>>> GetUserLeagues(int userId)
        {
            try
            {
                var leagues = new List<LeagueDto>();
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        l.Id, l.Name, l.Description, l.OwnerId, l.ChampionshipId, 
                        l.IsPublic, l.InviteCode, l.CreatedAt, l.IsActive,
                        u.FirstName + ' ' + u.LastName AS OwnerName,
                        c.Year AS ChampionshipName,
                        (SELECT COUNT(*) FROM LeagueMembers WHERE LeagueId = l.Id AND IsActive = 1) AS MemberCount
                    FROM Leagues l
                    INNER JOIN Users u ON l.OwnerId = u.Id
                    INNER JOIN Championships c ON l.ChampionshipId = c.Id
                    INNER JOIN LeagueMembers lm ON l.Id = lm.LeagueId
                    WHERE lm.UserId = @UserId AND lm.IsActive = 1 AND l.IsActive = 1
                    ORDER BY l.CreatedAt DESC";

                await using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    leagues.Add(MapToLeagueDto(reader));
                }

                return ServiceResult<List<LeagueDto>>.Succeed(leagues);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<LeagueDto>>.Fail($"An error occurred while retrieving leagues: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> JoinLeagueByCodeAsync(string inviteCode, int userId)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Find league by invite code
                int? leagueId = null;
                var findLeagueSql = "SELECT Id FROM Leagues WHERE InviteCode = @InviteCode AND IsActive = 1";
                await using (var findCmd = new SqlCommand(findLeagueSql, connection))
                {
                    findCmd.Parameters.AddWithValue("@InviteCode", inviteCode.Trim().ToUpper());
                    var result = await findCmd.ExecuteScalarAsync();
                    if (result != null)
                    {
                        leagueId = Convert.ToInt32(result);
                    }
                }

                if (!leagueId.HasValue)
                {
                    return ServiceResult<bool>.Fail("Invalid invite code. Please check and try again.");
                }

                // Check if user is already a member
                var checkSql = "SELECT COUNT(*) FROM LeagueMembers WHERE LeagueId = @LeagueId AND UserId = @UserId";
                await using (var checkCmd = new SqlCommand(checkSql, connection))
                {
                    checkCmd.Parameters.AddWithValue("@LeagueId", leagueId.Value);
                    checkCmd.Parameters.AddWithValue("@UserId", userId);
                    var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (count > 0)
                    {
                        return ServiceResult<bool>.Fail("You are already a member of this league.");
                    }
                }

                // Add user as member
                var insertSql = @"
                    INSERT INTO LeagueMembers (LeagueId, UserId, Role, JoinedAt, IsActive)
                    VALUES (@LeagueId, @UserId, 'Member', GETUTCDATE(), 1)";
                await using (var cmd = new SqlCommand(insertSql, connection))
                {
                    cmd.Parameters.AddWithValue("@LeagueId", leagueId.Value);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                return ServiceResult<bool>.Succeed(true, "Successfully joined the league!");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"An error occurred while joining the league: {ex.Message}");
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Gets the active championship ID (where IsActive = 1).
        /// </summary>
        private async Task<int?> GetActiveChampionshipIdAsync(SqlConnection connection, SqlTransaction transaction)
        {
            var sql = "SELECT TOP 1 Id FROM Championships WHERE IsActive = 1";
            await using var cmd = new SqlCommand(sql, connection, transaction);
            var result = await cmd.ExecuteScalarAsync();
            return result == null ? null : Convert.ToInt32(result);
        }

        /// <summary>
        /// Generates a unique invite code for a league.
        /// </summary>
        private static string GenerateInviteCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var bytes = new byte[8];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            var result = new char[8];
            for (int i = 0; i < 8; i++)
            {
                result[i] = chars[bytes[i] % chars.Length];
            }
            return new string(result);
        }

        /// <summary>
        /// Gets a league by ID with owner and championship details.
        /// </summary>
        private async Task<LeagueDto?> GetLeagueByIdAsync(SqlConnection connection, int leagueId)
        {
            var sql = @"
                SELECT 
                    l.Id, l.Name, l.Description, l.OwnerId, l.ChampionshipId, 
                    l.IsPublic, l.InviteCode, l.CreatedAt, l.IsActive,
                    u.FirstName + ' ' + u.LastName AS OwnerName,
                    c.Year AS ChampionshipName,
                    (SELECT COUNT(*) FROM LeagueMembers WHERE LeagueId = l.Id AND IsActive = 1) AS MemberCount
                FROM Leagues l
                INNER JOIN Users u ON l.OwnerId = u.Id
                INNER JOIN Championships c ON l.ChampionshipId = c.Id
                WHERE l.Id = @LeagueId";

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@LeagueId", leagueId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToLeagueDto(reader);
            }
            return null;
        }

        /// <summary>
        /// Maps a data reader row to a LeagueDto.
        /// </summary>
        private static LeagueDto MapToLeagueDto(SqlDataReader reader)
        {
            return new LeagueDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) 
                    ? null 
                    : reader.GetString(reader.GetOrdinal("Description")),
                OwnerId = reader.GetInt32(reader.GetOrdinal("OwnerId")),
                OwnerName = reader.GetString(reader.GetOrdinal("OwnerName")),
                ChampionshipId = reader.GetInt32(reader.GetOrdinal("ChampionshipId")),
                ChampionshipName = reader.GetInt32(reader.GetOrdinal("ChampionshipName")).ToString(),
                IsPublic = reader.GetBoolean(reader.GetOrdinal("IsPublic")),
                InviteCode = reader.IsDBNull(reader.GetOrdinal("InviteCode")) 
                    ? null 
                    : reader.GetString(reader.GetOrdinal("InviteCode")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                MemberCount = reader.GetInt32(reader.GetOrdinal("MemberCount"))
            };
        }

        #endregion
    }
}
