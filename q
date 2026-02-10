[1mdiff --git a/F1Predictions/ApiControllers/LeaguesApiController.cs b/F1Predictions/ApiControllers/LeaguesApiController.cs[m
[1mindex a237336..3436635 100644[m
[1m--- a/F1Predictions/ApiControllers/LeaguesApiController.cs[m
[1m+++ b/F1Predictions/ApiControllers/LeaguesApiController.cs[m
[36m@@ -68,5 +68,19 @@[m [mnamespace F1Predictions.ApiControllers[m
 [m
             return Ok(new { message = result.Message });[m
         }[m
[32m+[m
[32m+[m[32m        [HttpGet("league-members/{leagueId}")][m
[32m+[m[32m        public async Task<ActionResult<List<LeagueMemberDto>>> GetLeagueMembers(int leagueId)[m
[32m+[m[32m        {[m
[32m+[m[32m            int userId = User.GetUserId();[m
[32m+[m[32m            var result = await _leaguesService.GetLeagueMembers(userId, leagueId);[m
[32m+[m
[32m+[m[32m            if (!result.Success)[m
[32m+[m[32m                return BadRequest(result.Message);[m
[32m+[m
[32m+[m[32m            return Ok(result.Data);[m
[32m+[m[32m        }[m
[32m+[m
[32m+[m
     }[m
 }[m
\ No newline at end of file[m
[1mdiff --git a/F1Predictions/Services/Interfaces/ILeaguesService.cs b/F1Predictions/Services/Interfaces/ILeaguesService.cs[m
[1mindex 473d19f..a53cfdc 100644[m
[1m--- a/F1Predictions/Services/Interfaces/ILeaguesService.cs[m
[1m+++ b/F1Predictions/Services/Interfaces/ILeaguesService.cs[m
[36m@@ -19,5 +19,7 @@[m [mnamespace F1Predictions.Services.Interfaces[m
         /// Joins a user to a league using an invite code.[m
         /// </summary>[m
         Task<ServiceResult<bool>> JoinLeagueByCodeAsync(string inviteCode, int userId);[m
[32m+[m
[32m+[m[32m        Task<ServiceResult<List<LeagueMemberDto>>> GetLeagueMembers(int userId, int leagueId);[m
     }[m
 }[m
[1mdiff --git a/F1Predictions/Services/LeaguesService.cs b/F1Predictions/Services/LeaguesService.cs[m
[1mindex 84a4e12..baa33de 100644[m
[1m--- a/F1Predictions/Services/LeaguesService.cs[m
[1m+++ b/F1Predictions/Services/LeaguesService.cs[m
[36m@@ -193,6 +193,50 @@[m [mnamespace F1Predictions.Services[m
             }[m
         }[m
 [m
[32m+[m[41m        [m
[32m+[m[32m        public async Task<ServiceResult<List<LeagueMemberDto>>> GetLeagueMembers(int userId, int leagueId)[m
[32m+[m[32m        {[m
[32m+[m[32m            try[m
[32m+[m[32m            {[m
[32m+[m[32m                var leagues = new List<LeagueMemberDto>();[m
[32m+[m[32m                await using var connection = new SqlConnection(_connectionString);[m
[32m+[m[32m                await connection.OpenAsync();[m
[32m+[m
[32m+[m[32m                var sql = @"[m
[32m+[m[32m                            SELECT[m
[32m+[m[32m                                lm.UserId, u.FirstName, u.LastName, lm.Role, lm.JoinedAt[m
[32m+[m[32m                            FROM LeagueMembers lm[m
[32m+[m[32m                            INNER JOIN Users u ON u.Id = lm.UserId[m
[32m+[m[32m                            WHERE lm.LeagueId = @LeagueId[m
[32m+[m[32m                              AND lm.IsActive = 1[m
[32m+[m[32m                              AND EXISTS ([m
[32m+[m[32m                                  SELECT 1[m
[32m+[m[32m                                  FROM LeagueMembers lm2[m
[32m+[m[32m                                  WHERE lm2.LeagueId = @LeagueId[m
[32m+[m[32m                                    AND lm2.UserId = @UserId[m
[32m+[m[32m                                    AND lm2.IsActive = 1[m
[32m+[m[32m                              )[m
[32m+[m[32m                            ORDER BY lm.JoinedAt";[m
[32m+[m
[32m+[m[32m                await using var cmd = new SqlCommand(sql, connection);[m
[32m+[m[32m                cmd.Parameters.AddWithValue("@UserId", userId);[m
[32m+[m[32m                cmd.Parameters.AddWithValue("@LeagueId", leagueId);[m
[32m+[m
[32m+[m[32m                await using var reader = await cmd.ExecuteReaderAsync();[m
[32m+[m[32m                while (await reader.ReadAsync())[m
[32m+[m[32m                {[m
[32m+[m[32m                    leagues.Add(MapToLeagueMemberDto(reader));[m
[32m+[m[32m                }[m
[32m+[m
[32m+[m[32m                return ServiceResult<List<LeagueMemberDto>>.Succeed(leagues);[m
[32m+[m[32m            }[m
[32m+[m[32m            catch (Exception ex)[m
[32m+[m[32m            {[m
[32m+[m[32m                return ServiceResult<List<LeagueMemberDto>>.Fail($"An error occurred while retrieving leagues: {ex.Message}");[m
[32m+[m[32m            }[m
[32m+[m[32m        }[m
[32m+[m[41m            [m
[32m+[m
         #region Private Helper Methods[m
 [m
         /// <summary>[m
[36m@@ -278,6 +322,19 @@[m [mnamespace F1Predictions.Services[m
             };[m
         }[m
 [m
[32m+[m[32m        private static LeagueMemberDto MapToLeagueMemberDto(SqlDataReader reader)[m
[32m+[m[32m        {[m
[32m+[m[32m            return new LeagueMemberDto[m
[32m+[m[32m            {[m
[32m+[m[32m                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),[m
[32m+[m[32m                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),[m
[32m+[m[32m                LastName = reader.GetString(reader.GetOrdinal("LastName")),[m
[32m+[m[32m                Role = reader.GetString(reader.GetOrdinal("Role")),[m
[32m+[m[32m                JoinedAt = reader.GetDateTime(reader.GetOrdinal("JoinedAt"))[m
[32m+[m[32m            };[m
[32m+[m[32m        }[m
[32m+[m
[32m+[m
         #endregion[m
     }[m
 }[m
