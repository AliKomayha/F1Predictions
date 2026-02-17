using System.Data;
using F1Predictions.Models;
using F1Predictions.Models.DTOs;
using F1Predictions.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace F1Predictions.Services
{
    public class PredictionsService : IPredictionsService
    {
        private readonly string _connectionString;

        public PredictionsService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<ServiceResult<List<RacePredictionDto>>> GetRacePredictions(int raceId, int leagueId, int userId)
        {
            try
            {
                var predictions = new List<RacePredictionDto>();
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Verify user is a member of this league
                if (!await IsLeagueMember(connection, userId, leagueId))
                {
                    return ServiceResult<List<RacePredictionDto>>.Fail("You are not a member of this league.");
                }

                var sql = @"
                    SELECT 
                        wp.Id AS WeeklyPredictionId,
                        wp.PredictionType,
                        wp.AdminDefinedText,
                        wp.AllowedTargetTypes,
                        up.Id AS UserPredictionId,
                        up.TargetType AS UserTargetType,
                        up.DriverId,
                        up.TeamId,
                        up.Text AS UserText,
                        up.IsLocked,
                        d.first_name AS DriverFirstName,
                        d.last_name AS DriverLastName,
                        t.displayName AS TeamName
                    FROM WeeklyPredictions wp
                    LEFT JOIN UserPredictions up 
                        ON wp.Id = up.WeeklyPredictionId 
                        AND up.UserId = @UserId 
                        AND up.LeagueId = @LeagueId
                    LEFT JOIN Drivers d ON up.DriverId = d.Id
                    LEFT JOIN Teams t ON up.TeamId = t.Id
                    WHERE wp.RaceId = @RaceId AND wp.IsActive = 1
                    ORDER BY 
                        CASE wp.PredictionType
                            WHEN 'Pole' THEN 1
                            WHEN 'P1' THEN 2
                            WHEN 'P2' THEN 3
                            WHEN 'P3' THEN 4
                            WHEN 'SprintPole' THEN 5
                            WHEN 'SprintWinner' THEN 6
                            WHEN 'Surprise' THEN 7
                            WHEN 'Flop' THEN 8
                            WHEN 'Crazy' THEN 9
                            WHEN 'Custom' THEN 10
                            ELSE 99
                        END";

                await using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@RaceId", raceId);
                cmd.Parameters.AddWithValue("@LeagueId", leagueId);
                cmd.Parameters.AddWithValue("@UserId", userId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var dto = new RacePredictionDto
                    {
                        WeeklyPredictionId = reader.GetInt32(reader.GetOrdinal("WeeklyPredictionId")),
                        PredictionType = reader.GetString(reader.GetOrdinal("PredictionType")),
                        AdminDefinedText = reader.IsDBNull(reader.GetOrdinal("AdminDefinedText"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("AdminDefinedText")),
                        AllowedTargetTypes = reader.GetString(reader.GetOrdinal("AllowedTargetTypes"))
                    };

                    // If user has a pick for this prediction
                    if (!reader.IsDBNull(reader.GetOrdinal("UserPredictionId")))
                    {
                        dto.UserPick = new UserPredictionDto
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("UserPredictionId")),
                            TargetType = reader.GetString(reader.GetOrdinal("UserTargetType")),
                            DriverId = reader.IsDBNull(reader.GetOrdinal("DriverId"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("DriverId")),
                            DriverName = reader.IsDBNull(reader.GetOrdinal("DriverFirstName"))
                                ? null
                                : $"{reader.GetString(reader.GetOrdinal("DriverFirstName"))} {reader.GetString(reader.GetOrdinal("DriverLastName"))}",
                            TeamId = reader.IsDBNull(reader.GetOrdinal("TeamId"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("TeamId")),
                            TeamName = reader.IsDBNull(reader.GetOrdinal("TeamName"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("TeamName")),
                            Text = reader.IsDBNull(reader.GetOrdinal("UserText"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("UserText")),
                            IsLocked = reader.IsDBNull(reader.GetOrdinal("IsLocked"))
                                ? false
                                : reader.GetBoolean(reader.GetOrdinal("IsLocked"))
                        };
                    }

                    predictions.Add(dto);
                }

                return ServiceResult<List<RacePredictionDto>>.Succeed(predictions);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<RacePredictionDto>>.Fail($"Error retrieving predictions: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<DriverOptionDto>>> GetDriversForRace(int raceId)
        {
            try
            {
                var drivers = new List<DriverOptionDto>();
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        d.Id, d.first_name, d.last_name, d.championship_number, t.displayName AS TeamName
                    FROM DriverTeams dt
                    INNER JOIN Drivers d ON dt.driver_id = d.Id
                    INNER JOIN Teams t ON dt.team_id = t.Id
                    WHERE dt.championship_id = (SELECT championship_id FROM Races WHERE Id = @RaceId)
                    ORDER BY t.displayName, d.last_name";

                await using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@RaceId", raceId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    drivers.Add(new DriverOptionDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                        LastName = reader.GetString(reader.GetOrdinal("last_name")),
                        ChampionshipNumber = reader.GetInt32(reader.GetOrdinal("championship_number")),
                        TeamName = reader.GetString(reader.GetOrdinal("TeamName"))
                    });
                }

                return ServiceResult<List<DriverOptionDto>>.Succeed(drivers);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<DriverOptionDto>>.Fail($"Error retrieving drivers: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<TeamOptionDto>>> GetTeamsForRace(int raceId)
        {
            try
            {
                var teams = new List<TeamOptionDto>();
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT DISTINCT t.Id, t.Name, t.displayName
                    FROM DriverTeams dt
                    INNER JOIN Teams t ON dt.team_id = t.Id
                    WHERE dt.championship_id = (SELECT championship_id FROM Races WHERE Id = @RaceId)
                    ORDER BY t.displayName";

                await using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@RaceId", raceId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    teams.Add(new TeamOptionDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        DisplayName = reader.GetString(reader.GetOrdinal("displayName"))
                    });
                }

                return ServiceResult<List<TeamOptionDto>>.Succeed(teams);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<TeamOptionDto>>.Fail($"Error retrieving teams: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<RaceOptionDto>>> GetRacesForLeague(int leagueId)
        {
            try
            {
                var races = new List<RaceOptionDto>();
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT r.Id, r.race_name, r.round_number, r.race_date, 
                           r.PredictionsLockedAt, tr.Name AS TrackName
                    FROM Races r
                    INNER JOIN Tracks tr ON r.track_id = tr.Id
                    WHERE r.championship_id = (SELECT ChampionshipId FROM Leagues WHERE Id = @LeagueId)
                    ORDER BY r.round_number";

                await using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@LeagueId", leagueId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    races.Add(new RaceOptionDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        RaceName = reader.GetString(reader.GetOrdinal("race_name")),
                        RoundNumber = reader.GetInt32(reader.GetOrdinal("round_number")),
                        RaceDate = reader.GetDateTime(reader.GetOrdinal("race_date")),
                        TrackName = reader.GetString(reader.GetOrdinal("TrackName")),
                        PredictionsLockedAt = reader.GetDateTimeOffset(reader.GetOrdinal("PredictionsLockedAt"))
                    });
                }

                return ServiceResult<List<RaceOptionDto>>.Succeed(races);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<RaceOptionDto>>.Fail($"Error retrieving races: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> SubmitPrediction(SubmitPredictionRequest dto, int userId)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 1. Check if predictions are locked for this race
                var checkLockSql = @"
                    SELECT r.PredictionsLockedAt 
                    FROM Races r
                    INNER JOIN WeeklyPredictions wp ON r.Id = wp.RaceId
                    WHERE wp.Id = @WpId";

                DateTimeOffset? lockTime = null;
                await using (var lockCheckCmd = new SqlCommand(checkLockSql, connection))
                {
                    lockCheckCmd.Parameters.AddWithValue("@WpId", dto.WeeklyPredictionId);
                    var result = await lockCheckCmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        lockTime = (DateTimeOffset)result;
                    }
                }

                if (lockTime.HasValue && DateTimeOffset.UtcNow >= lockTime.Value)
                {
                    return ServiceResult<bool>.Fail("Predictions are locked. The qualifying session has started.");
                }

                // 2. Verify league membership
                if (!await IsLeagueMember(connection, userId, dto.LeagueId))
                {
                    return ServiceResult<bool>.Fail("You are not a member of this league.");
                }

                // 3. Get the weekly prediction to validate AllowedTargetTypes
                string? allowedTargetTypes = null;
                bool isActive = false;
                var getWpSql = "SELECT AllowedTargetTypes, IsActive FROM WeeklyPredictions WHERE Id = @WpId";
                await using (var getCmd = new SqlCommand(getWpSql, connection))
                {
                    getCmd.Parameters.AddWithValue("@WpId", dto.WeeklyPredictionId);
                    await using var reader = await getCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        allowedTargetTypes = reader.GetString(0);
                        isActive = reader.GetBoolean(1);
                    }
                }

                if (allowedTargetTypes == null)
                {
                    return ServiceResult<bool>.Fail("Prediction not found.");
                }

                if (!isActive)
                {
                    return ServiceResult<bool>.Fail("This prediction is no longer active.");
                }

                // 4. Validate target type against allowed types
                var allowed = allowedTargetTypes.Split(',').Select(s => s.Trim()).ToList();
                if (!allowed.Contains(dto.TargetType))
                {
                    return ServiceResult<bool>.Fail($"Target type '{dto.TargetType}' is not allowed for this prediction. Allowed: {allowedTargetTypes}");
                }

                // 5. Validate required fields based on target type
                switch (dto.TargetType)
                {
                    case "Driver":
                        if (!dto.DriverId.HasValue)
                            return ServiceResult<bool>.Fail("Please select a driver.");
                        break;
                    case "Team":
                        if (!dto.TeamId.HasValue)
                            return ServiceResult<bool>.Fail("Please select a team.");
                        break;
                    case "Text":
                        if (string.IsNullOrWhiteSpace(dto.Text))
                            return ServiceResult<bool>.Fail("Please enter your prediction text.");
                        break;
                    default:
                        return ServiceResult<bool>.Fail($"Invalid target type: {dto.TargetType}");
                }

                // 6. Check if prediction is locked
                var checkLockedSql = @"
                    SELECT IsLocked FROM UserPredictions 
                    WHERE WeeklyPredictionId = @WpId AND LeagueId = @LeagueId AND UserId = @UserId";
                await using (var lockCmd = new SqlCommand(checkLockedSql, connection))
                {
                    lockCmd.Parameters.AddWithValue("@WpId", dto.WeeklyPredictionId);
                    lockCmd.Parameters.AddWithValue("@LeagueId", dto.LeagueId);
                    lockCmd.Parameters.AddWithValue("@UserId", userId);
                    var lockResult = await lockCmd.ExecuteScalarAsync();
                    if (lockResult != null && (bool)lockResult)
                    {
                        return ServiceResult<bool>.Fail("This prediction is locked and cannot be changed.");
                    }
                }

                // 7. Upsert: check if exists, then update or insert
                var checkExistsSql = @"
                    SELECT Id FROM UserPredictions 
                    WHERE WeeklyPredictionId = @WpId AND LeagueId = @LeagueId AND UserId = @UserId";
                int? existingId = null;
                await using (var checkCmd = new SqlCommand(checkExistsSql, connection))
                {
                    checkCmd.Parameters.AddWithValue("@WpId", dto.WeeklyPredictionId);
                    checkCmd.Parameters.AddWithValue("@LeagueId", dto.LeagueId);
                    checkCmd.Parameters.AddWithValue("@UserId", userId);
                    var result = await checkCmd.ExecuteScalarAsync();
                    if (result != null)
                    {
                        existingId = Convert.ToInt32(result);
                    }
                }

                if (existingId.HasValue)
                {
                    // Update existing prediction
                    var updateSql = @"
                        UPDATE UserPredictions SET
                            TargetType = @TargetType,
                            DriverId = @DriverId,
                            TeamId = @TeamId,
                            Text = @Text
                        WHERE Id = @Id";
                    await using var updateCmd = new SqlCommand(updateSql, connection);
                    updateCmd.Parameters.AddWithValue("@Id", existingId.Value);
                    updateCmd.Parameters.AddWithValue("@TargetType", dto.TargetType);
                    updateCmd.Parameters.AddWithValue("@DriverId", (object?)dto.DriverId ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@TeamId", (object?)dto.TeamId ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@Text", (object?)dto.Text ?? DBNull.Value);
                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // Insert new prediction
                    var insertSql = @"
                        INSERT INTO UserPredictions (UserId, LeagueId, WeeklyPredictionId, TargetType, DriverId, TeamId, Text, IsLocked, CreatedAt)
                        VALUES (@UserId, @LeagueId, @WpId, @TargetType, @DriverId, @TeamId, @Text, 0, GETUTCDATE())";
                    await using var insertCmd = new SqlCommand(insertSql, connection);
                    insertCmd.Parameters.AddWithValue("@UserId", userId);
                    insertCmd.Parameters.AddWithValue("@LeagueId", dto.LeagueId);
                    insertCmd.Parameters.AddWithValue("@WpId", dto.WeeklyPredictionId);
                    insertCmd.Parameters.AddWithValue("@TargetType", dto.TargetType);
                    insertCmd.Parameters.AddWithValue("@DriverId", (object?)dto.DriverId ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@TeamId", (object?)dto.TeamId ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Text", (object?)dto.Text ?? DBNull.Value);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                return ServiceResult<bool>.Succeed(true, "Prediction saved successfully!");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"Error saving prediction: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<RacePredictionDto>>> GetMemberPredictions(int raceId, int leagueId, int targetUserId, int requestingUserId)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Verify requesting user is a member of this league
                if (!await IsLeagueMember(connection, requestingUserId, leagueId))
                {
                    return ServiceResult<List<RacePredictionDto>>.Fail("You are not a member of this league.");
                }

                // Verify target user is also a member
                if (!await IsLeagueMember(connection, targetUserId, leagueId))
                {
                    return ServiceResult<List<RacePredictionDto>>.Fail("This user is not a member of this league.");
                }

                var predictions = new List<RacePredictionDto>();
                var sql = @"
                    SELECT 
                        wp.Id AS WeeklyPredictionId,
                        wp.PredictionType,
                        wp.AdminDefinedText,
                        wp.AllowedTargetTypes,
                        up.Id AS UserPredictionId,
                        up.TargetType AS UserTargetType,
                        up.DriverId,
                        up.TeamId,
                        up.Text AS UserText,
                        up.IsLocked,
                        d.first_name AS DriverFirstName,
                        d.last_name AS DriverLastName,
                        t.displayName AS TeamName
                    FROM WeeklyPredictions wp
                    LEFT JOIN UserPredictions up 
                        ON wp.Id = up.WeeklyPredictionId 
                        AND up.UserId = @TargetUserId 
                        AND up.LeagueId = @LeagueId
                    LEFT JOIN Drivers d ON up.DriverId = d.Id
                    LEFT JOIN Teams t ON up.TeamId = t.Id
                    WHERE wp.RaceId = @RaceId AND wp.IsActive = 1
                    ORDER BY 
                        CASE wp.PredictionType
                            WHEN 'Pole' THEN 1
                            WHEN 'P1' THEN 2
                            WHEN 'P2' THEN 3
                            WHEN 'P3' THEN 4
                            WHEN 'SprintPole' THEN 5
                            WHEN 'SprintWinner' THEN 6
                            WHEN 'Surprise' THEN 7
                            WHEN 'Flop' THEN 8
                            WHEN 'Crazy' THEN 9
                            WHEN 'Custom' THEN 10
                            ELSE 99
                        END";

                await using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@RaceId", raceId);
                cmd.Parameters.AddWithValue("@LeagueId", leagueId);
                cmd.Parameters.AddWithValue("@TargetUserId", targetUserId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var dto = new RacePredictionDto
                    {
                        WeeklyPredictionId = reader.GetInt32(reader.GetOrdinal("WeeklyPredictionId")),
                        PredictionType = reader.GetString(reader.GetOrdinal("PredictionType")),
                        AdminDefinedText = reader.IsDBNull(reader.GetOrdinal("AdminDefinedText"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("AdminDefinedText")),
                        AllowedTargetTypes = reader.GetString(reader.GetOrdinal("AllowedTargetTypes"))
                    };

                    if (!reader.IsDBNull(reader.GetOrdinal("UserPredictionId")))
                    {
                        dto.UserPick = new UserPredictionDto
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("UserPredictionId")),
                            TargetType = reader.GetString(reader.GetOrdinal("UserTargetType")),
                            DriverId = reader.IsDBNull(reader.GetOrdinal("DriverId"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("DriverId")),
                            DriverName = reader.IsDBNull(reader.GetOrdinal("DriverFirstName"))
                                ? null
                                : $"{reader.GetString(reader.GetOrdinal("DriverFirstName"))} {reader.GetString(reader.GetOrdinal("DriverLastName"))}",
                            TeamId = reader.IsDBNull(reader.GetOrdinal("TeamId"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("TeamId")),
                            TeamName = reader.IsDBNull(reader.GetOrdinal("TeamName"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("TeamName")),
                            Text = reader.IsDBNull(reader.GetOrdinal("UserText"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("UserText")),
                            IsLocked = reader.IsDBNull(reader.GetOrdinal("IsLocked"))
                                ? false
                                : reader.GetBoolean(reader.GetOrdinal("IsLocked"))
                        };
                    }

                    predictions.Add(dto);
                }

                return ServiceResult<List<RacePredictionDto>>.Succeed(predictions);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<RacePredictionDto>>.Fail($"Error retrieving member predictions: {ex.Message}");
            }
        }

        #region League Hub Methods

        public async Task<ServiceResult<CurrentRaceDto>> GetCurrentRace(int leagueId, int? raceId = null)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Get championship ID for this league
                var champSql = "SELECT ChampionshipId FROM Leagues WHERE Id = @LeagueId";
                await using var champCmd = new SqlCommand(champSql, connection);
                champCmd.Parameters.AddWithValue("@LeagueId", leagueId);
                var champId = await champCmd.ExecuteScalarAsync();
                if (champId == null)
                    return ServiceResult<CurrentRaceDto>.Fail("League not found.");

                // Get total rounds
                var totalSql = "SELECT COUNT(*) FROM Races WHERE championship_id = @ChampId";
                await using var totalCmd = new SqlCommand(totalSql, connection);
                totalCmd.Parameters.AddWithValue("@ChampId", champId);
                var totalRounds = Convert.ToInt32(await totalCmd.ExecuteScalarAsync());

                string raceSql;
                SqlCommand raceCmd;

                if (raceId.HasValue)
                {
                    // Navigating to a specific race
                    raceSql = @"
                        SELECT r.Id, r.race_name, r.round_number, r.race_date, 
                               r.PredictionsLockedAt, tr.Name AS TrackName
                        FROM Races r
                        INNER JOIN Tracks tr ON r.track_id = tr.Id
                        WHERE r.Id = @RaceId AND r.championship_id = @ChampId";
                    raceCmd = new SqlCommand(raceSql, connection);
                    raceCmd.Parameters.AddWithValue("@RaceId", raceId.Value);
                    raceCmd.Parameters.AddWithValue("@ChampId", champId);
                }
                else
                {
                    // Auto-detect: find the earliest race where predictions aren't locked yet, or the latest race
                    raceSql = @"
                        SELECT TOP 1 r.Id, r.race_name, r.round_number, r.race_date, 
                               r.PredictionsLockedAt, tr.Name AS TrackName
                        FROM Races r
                        INNER JOIN Tracks tr ON r.track_id = tr.Id
                        WHERE r.championship_id = @ChampId
                          AND r.PredictionsLockedAt > SYSDATETIMEOFFSET()
                        ORDER BY r.round_number ASC";
                    raceCmd = new SqlCommand(raceSql, connection);
                    raceCmd.Parameters.AddWithValue("@ChampId", champId);
                }

                await using (raceCmd)
                {
                    await using var reader = await raceCmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        // All races locked — return the most recent one
                        reader.Close();
                        var fallbackSql = @"
                            SELECT TOP 1 r.Id, r.race_name, r.round_number, r.race_date, 
                                   r.PredictionsLockedAt, tr.Name AS TrackName
                            FROM Races r
                            INNER JOIN Tracks tr ON r.track_id = tr.Id
                            WHERE r.championship_id = @ChampId
                            ORDER BY r.round_number DESC";
                        await using var fallbackCmd = new SqlCommand(fallbackSql, connection);
                        fallbackCmd.Parameters.AddWithValue("@ChampId", champId);
                        await using var fallbackReader = await fallbackCmd.ExecuteReaderAsync();
                        if (!await fallbackReader.ReadAsync())
                            return ServiceResult<CurrentRaceDto>.Fail("No races found for this league.");

                        return ServiceResult<CurrentRaceDto>.Succeed(
                            await BuildCurrentRaceDto(connection, fallbackReader, totalRounds, leagueId));
                    }

                    return ServiceResult<CurrentRaceDto>.Succeed(
                        await BuildCurrentRaceDto(connection, reader, totalRounds, leagueId));
                }
            }
            catch (Exception ex)
            {
                return ServiceResult<CurrentRaceDto>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<LeagueSummaryDto>> GetLeagueSummary(int leagueId, int raceId, int userId)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                if (!await IsLeagueMember(connection, userId, leagueId))
                    return ServiceResult<LeagueSummaryDto>.Fail("You are not a member of this league.");

                // Get league name
                var leagueSql = "SELECT Name FROM Leagues WHERE Id = @LeagueId";
                await using var leagueCmd = new SqlCommand(leagueSql, connection);
                leagueCmd.Parameters.AddWithValue("@LeagueId", leagueId);
                var leagueName = (string?)await leagueCmd.ExecuteScalarAsync() ?? "Unknown";

                // Get user's total points (overall across all races)
                var totalPtsSql = @"SELECT ISNULL(SUM(PointsTotal), 0) 
                                    FROM WeeklyPoints 
                                    WHERE UserId = @UserId AND LeagueId = @LeagueId";
                await using var totalPtsCmd = new SqlCommand(totalPtsSql, connection);
                totalPtsCmd.Parameters.AddWithValue("@UserId", userId);
                totalPtsCmd.Parameters.AddWithValue("@LeagueId", leagueId);
                var totalPts = Convert.ToInt32(await totalPtsCmd.ExecuteScalarAsync());

                // Get user's race points
                var racePtsSql = @"SELECT ISNULL(PointsTotal, 0) 
                                   FROM WeeklyPoints 
                                   WHERE UserId = @UserId AND LeagueId = @LeagueId AND RaceId = @RaceId";
                await using var racePtsCmd = new SqlCommand(racePtsSql, connection);
                racePtsCmd.Parameters.AddWithValue("@UserId", userId);
                racePtsCmd.Parameters.AddWithValue("@LeagueId", leagueId);
                racePtsCmd.Parameters.AddWithValue("@RaceId", raceId);
                var racePts = Convert.ToInt32(await racePtsCmd.ExecuteScalarAsync() ?? 0);

                // Get member standings
                var membersSql = @"
                    SELECT lm.UserId, u.FirstName, u.LastName, lm.Role,
                           ISNULL(SUM(wp.PointsTotal), 0) AS TotalPoints
                    FROM LeagueMembers lm
                    INNER JOIN Users u ON lm.UserId = u.Id
                    LEFT JOIN WeeklyPoints wp ON wp.UserId = lm.UserId AND wp.LeagueId = lm.LeagueId
                    WHERE lm.LeagueId = @LeagueId AND lm.IsActive = 1
                    GROUP BY lm.UserId, u.FirstName, u.LastName, lm.Role
                    ORDER BY ISNULL(SUM(wp.PointsTotal), 0) DESC";
                await using var membersCmd = new SqlCommand(membersSql, connection);
                membersCmd.Parameters.AddWithValue("@LeagueId", leagueId);

                var members = new List<MemberStandingDto>();
                await using var membersReader = await membersCmd.ExecuteReaderAsync();
                int rank = 0;
                while (await membersReader.ReadAsync())
                {
                    rank++;
                    var memberId = membersReader.GetInt32(membersReader.GetOrdinal("UserId"));
                    members.Add(new MemberStandingDto
                    {
                        UserId = memberId,
                        FirstName = membersReader.GetString(membersReader.GetOrdinal("FirstName")),
                        LastName = membersReader.GetString(membersReader.GetOrdinal("LastName")),
                        Role = membersReader.GetString(membersReader.GetOrdinal("Role")),
                        TotalPoints = membersReader.GetInt32(membersReader.GetOrdinal("TotalPoints")),
                        Rank = rank,
                        HasUndoneVotes = false // Will be set below
                    });
                }
                membersReader.Close();

                // Check undone votes: for each member, check if they have votable predictions 
                // that the current user hasn't voted on yet (during voting window)
                var raceState = await GetRaceStateValue(connection, raceId);
                if (raceState == "VotingOpen")
                {
                    var undoneVotesSql = @"
                        SELECT up.UserId, COUNT(*) AS UndoneCount
                        FROM UserPredictions up
                        INNER JOIN WeeklyPredictions wp ON up.WeeklyPredictionId = wp.Id
                        WHERE wp.RaceId = @RaceId AND up.LeagueId = @LeagueId
                          AND wp.PredictionType IN ('Surprise', 'Flop', 'Crazy', 'Custom')
                          AND NOT EXISTS (
                            SELECT 1 FROM PredictionVotes pv 
                            WHERE pv.UserPredictionId = up.Id AND pv.VoterId = @VoterId
                          )
                          AND NOT EXISTS (
                            SELECT 1 FROM UserPredictionPoints upp 
                            WHERE upp.UserPredictionId = up.Id
                          )
                        GROUP BY up.UserId";
                    await using var undoneCmd = new SqlCommand(undoneVotesSql, connection);
                    undoneCmd.Parameters.AddWithValue("@RaceId", raceId);
                    undoneCmd.Parameters.AddWithValue("@LeagueId", leagueId);
                    undoneCmd.Parameters.AddWithValue("@VoterId", userId);

                    await using var undoneReader = await undoneCmd.ExecuteReaderAsync();
                    while (await undoneReader.ReadAsync())
                    {
                        var uid = undoneReader.GetInt32(0);
                        var m = members.FirstOrDefault(m => m.UserId == uid);
                        if (m != null) m.HasUndoneVotes = true;
                    }
                }

                // Get current race DTO
                var currentRaceResult = await GetCurrentRace(leagueId, raceId);

                return ServiceResult<LeagueSummaryDto>.Succeed(new LeagueSummaryDto
                {
                    LeagueId = leagueId,
                    LeagueName = leagueName,
                    CurrentRace = currentRaceResult.Data!,
                    UserTotalPoints = totalPts,
                    UserRacePoints = racePts,
                    Members = members
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<LeagueSummaryDto>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<MemberPredictionDto>>> GetMemberPredictionsEnhanced(
            int raceId, int leagueId, int targetUserId, int requestingUserId)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                if (!await IsLeagueMember(connection, requestingUserId, leagueId))
                    return ServiceResult<List<MemberPredictionDto>>.Fail("You are not a member of this league.");
                if (!await IsLeagueMember(connection, targetUserId, leagueId))
                    return ServiceResult<List<MemberPredictionDto>>.Fail("This user is not a member of this league.");

                // Check if predictions are locked — if not, hide other members' picks
                var lockSql = @"SELECT r.PredictionsLockedAt 
                                FROM Races r 
                                WHERE r.Id = @RaceId";
                await using var lockCmd = new SqlCommand(lockSql, connection);
                lockCmd.Parameters.AddWithValue("@RaceId", raceId);
                var lockAt = await lockCmd.ExecuteScalarAsync();
                var now = DateTimeOffset.UtcNow;
                bool isLocked = lockAt != null && now >= (DateTimeOffset)lockAt;
                bool viewingOwnPredictions = targetUserId == requestingUserId;

                // If not locked and viewing someone else, return blank predictions
                if (!isLocked && !viewingOwnPredictions)
                {
                    return ServiceResult<List<MemberPredictionDto>>.Fail("Predictions aren't visible until they lock.");
                }

                var sql = @"
                    SELECT 
                        wp.Id AS WeeklyPredictionId,
                        wp.PredictionType,
                        wp.AdminDefinedText,
                        wp.AllowedTargetTypes,
                        up.Id AS UserPredictionId,
                        up.TargetType AS UserTargetType,
                        up.DriverId,
                        up.TeamId,
                        up.Text AS UserText,
                        up.IsLocked,
                        d.first_name AS DriverFirstName,
                        d.last_name AS DriverLastName,
                        t.displayName AS TeamName,
                        upp.PointsAwarded,
                        pv_mine.Vote AS MyVote,
                        (SELECT COUNT(*) FROM PredictionVotes WHERE UserPredictionId = up.Id AND Vote = 1) AS YesVotes,
                        (SELECT COUNT(*) FROM PredictionVotes WHERE UserPredictionId = up.Id AND Vote = 0) AS NoVotes
                    FROM WeeklyPredictions wp
                    LEFT JOIN UserPredictions up 
                        ON wp.Id = up.WeeklyPredictionId 
                        AND up.UserId = @TargetUserId 
                        AND up.LeagueId = @LeagueId
                    LEFT JOIN Drivers d ON up.DriverId = d.Id
                    LEFT JOIN Teams t ON up.TeamId = t.Id
                    LEFT JOIN UserPredictionPoints upp ON up.Id = upp.UserPredictionId
                    LEFT JOIN PredictionVotes pv_mine ON up.Id = pv_mine.UserPredictionId AND pv_mine.VoterId = @RequestingUserId
                    WHERE wp.RaceId = @RaceId AND wp.IsActive = 1
                    ORDER BY 
                        CASE wp.PredictionType
                            WHEN 'Pole' THEN 1
                            WHEN 'P1' THEN 2
                            WHEN 'P2' THEN 3
                            WHEN 'P3' THEN 4
                            WHEN 'SprintPole' THEN 5
                            WHEN 'SprintWinner' THEN 6
                            WHEN 'Surprise' THEN 7
                            WHEN 'Flop' THEN 8
                            WHEN 'Crazy' THEN 9
                            WHEN 'Custom' THEN 10
                            ELSE 99
                        END";

                await using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@RaceId", raceId);
                cmd.Parameters.AddWithValue("@LeagueId", leagueId);
                cmd.Parameters.AddWithValue("@TargetUserId", targetUserId);
                cmd.Parameters.AddWithValue("@RequestingUserId", requestingUserId);

                var votableTypes = new[] { "Surprise", "Flop", "Crazy", "Custom" };
                var raceState = await GetRaceStateValue(connection, raceId);
                var results = new List<MemberPredictionDto>();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var predType = reader.GetString(reader.GetOrdinal("PredictionType"));
                    bool isVotable = votableTypes.Contains(predType);

                    var dto = new MemberPredictionDto
                    {
                        WeeklyPredictionId = reader.GetInt32(reader.GetOrdinal("WeeklyPredictionId")),
                        PredictionType = predType,
                        AdminDefinedText = reader.IsDBNull(reader.GetOrdinal("AdminDefinedText"))
                            ? null : reader.GetString(reader.GetOrdinal("AdminDefinedText")),
                        AllowedTargetTypes = reader.GetString(reader.GetOrdinal("AllowedTargetTypes")),
                        IsVotable = isVotable
                    };

                    if (!reader.IsDBNull(reader.GetOrdinal("UserPredictionId")))
                    {
                        dto.UserPick = new UserPredictionDto
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("UserPredictionId")),
                            TargetType = reader.GetString(reader.GetOrdinal("UserTargetType")),
                            DriverId = reader.IsDBNull(reader.GetOrdinal("DriverId"))
                                ? null : reader.GetInt32(reader.GetOrdinal("DriverId")),
                            DriverName = reader.IsDBNull(reader.GetOrdinal("DriverFirstName"))
                                ? null : $"{reader.GetString(reader.GetOrdinal("DriverFirstName"))} {reader.GetString(reader.GetOrdinal("DriverLastName"))}",
                            TeamId = reader.IsDBNull(reader.GetOrdinal("TeamId"))
                                ? null : reader.GetInt32(reader.GetOrdinal("TeamId")),
                            TeamName = reader.IsDBNull(reader.GetOrdinal("TeamName"))
                                ? null : reader.GetString(reader.GetOrdinal("TeamName")),
                            Text = reader.IsDBNull(reader.GetOrdinal("UserText"))
                                ? null : reader.GetString(reader.GetOrdinal("UserText")),
                            IsLocked = !reader.IsDBNull(reader.GetOrdinal("IsLocked")) && reader.GetBoolean(reader.GetOrdinal("IsLocked"))
                        };

                        // Points info
                        if (!reader.IsDBNull(reader.GetOrdinal("PointsAwarded")))
                        {
                            dto.PointsAwarded = reader.GetInt32(reader.GetOrdinal("PointsAwarded"));
                            dto.PointsStatus = dto.PointsAwarded > 0 ? "Correct" : "Wrong";
                        }
                        else if (isVotable && raceState == "VotingOpen")
                        {
                            dto.PointsStatus = "VotingInProgress";
                        }
                        else
                        {
                            dto.PointsStatus = "Pending";
                        }

                        // Vote info (only for votable types)
                        if (isVotable)
                        {
                            dto.YesVotes = reader.GetInt32(reader.GetOrdinal("YesVotes"));
                            dto.NoVotes = reader.GetInt32(reader.GetOrdinal("NoVotes"));
                            dto.MyVote = reader.IsDBNull(reader.GetOrdinal("MyVote"))
                                ? null : reader.GetBoolean(reader.GetOrdinal("MyVote"));
                            dto.IsVoteResolved = dto.PointsAwarded.HasValue;
                        }
                    }

                    results.Add(dto);
                }

                return ServiceResult<List<MemberPredictionDto>>.Succeed(results);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<MemberPredictionDto>>.Fail($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Private Helpers

        private async Task<CurrentRaceDto> BuildCurrentRaceDto(SqlConnection connection, SqlDataReader reader, int totalRounds, int leagueId)
        {
            var id = reader.GetInt32(reader.GetOrdinal("Id"));
            var lockedAt = reader.GetDateTimeOffset(reader.GetOrdinal("PredictionsLockedAt"));
            var now = DateTimeOffset.UtcNow;

            var dto = new CurrentRaceDto
            {
                Id = id,
                RaceName = reader.GetString(reader.GetOrdinal("race_name")),
                RoundNumber = reader.GetInt32(reader.GetOrdinal("round_number")),
                TotalRounds = totalRounds,
                RaceDate = reader.GetDateTime(reader.GetOrdinal("race_date")),
                TrackName = reader.GetString(reader.GetOrdinal("TrackName")),
                PredictionsLockedAt = lockedAt,
                ArePredictionsLocked = now >= lockedAt
            };

            reader.Close();

            // Get race state
            dto.RaceState = await GetRaceStateValue(connection, id) ?? "PredictionsOpen";

            // Get voting window
            var vwSql = @"SELECT OpensAt, ClosesAt FROM PredictionVoteWindows 
                          WHERE RaceId = @RaceId AND LeagueId = @LeagueId";
            await using var vwCmd = new SqlCommand(vwSql, connection);
            vwCmd.Parameters.AddWithValue("@RaceId", id);
            vwCmd.Parameters.AddWithValue("@LeagueId", leagueId);
            await using var vwReader = await vwCmd.ExecuteReaderAsync();
            if (await vwReader.ReadAsync())
            {
                var closes = vwReader.GetDateTime(vwReader.GetOrdinal("ClosesAt"));
                var opens = vwReader.GetDateTime(vwReader.GetOrdinal("OpensAt"));
                var nowGmt2 = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(2)).DateTime;
                dto.IsVotingOpen = nowGmt2 >= opens && nowGmt2 <= closes;
                dto.VotingClosesAt = closes;
            }

            return dto;
        }

        private async Task<string?> GetRaceStateValue(SqlConnection connection, int raceId)
        {
            var sql = "SELECT State FROM RaceStates WHERE RaceId = @RaceId";
            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@RaceId", raceId);
            return (string?)await cmd.ExecuteScalarAsync();
        }

        private async Task<bool> IsLeagueMember(SqlConnection connection, int userId, int leagueId)
        {
            var sql = "SELECT COUNT(*) FROM LeagueMembers WHERE LeagueId = @LeagueId AND UserId = @UserId AND IsActive = 1";
            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@LeagueId", leagueId);
            cmd.Parameters.AddWithValue("@UserId", userId);
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        #endregion
    }
}
