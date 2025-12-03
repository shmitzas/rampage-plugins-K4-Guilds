using Dommel;
using K4_Guilds.Database.Models;

namespace K4_Guilds.Database;

public partial class DatabaseService
{
	public async Task<IEnumerable<GuildMember>> GetGuildMembersAsync(int guildId)
	{
		using var conn = GetConnection();
		conn.Open();
		return await conn.SelectAsync<GuildMember>(m => m.GuildId == guildId);
	}

	public async Task<GuildMember?> GetGuildMemberAsync(ulong steamId)
	{
		using var conn = GetConnection();
		conn.Open();
		return (await conn.SelectAsync<GuildMember>(m => m.SteamId == steamId)).FirstOrDefault();
	}

	public async Task<GuildMember> AddMemberAsync(int guildId, ulong steamId, string playerName, int rankPriority)
	{
		using var conn = GetConnection();
		conn.Open();

		var member = new GuildMember
		{
			GuildId = guildId,
			SteamId = steamId,
			PlayerName = playerName,
			RankPriority = rankPriority,
			JoinedAt = DateTime.UtcNow,
			LastSeen = DateTime.UtcNow
		};
		await conn.InsertAsync(member);

		return (await GetGuildMemberAsync(steamId))!;
	}

	public async Task<bool> RemoveMemberAsync(ulong steamId)
	{
		using var conn = GetConnection();
		conn.Open();

		var member = (await conn.SelectAsync<GuildMember>(m => m.SteamId == steamId)).FirstOrDefault();
		if (member == null) return false;

		return await conn.DeleteAsync(member);
	}

	public async Task<bool> UpdateMemberRankAsync(ulong steamId, int rankPriority)
	{
		using var conn = GetConnection();
		conn.Open();

		var member = (await conn.SelectAsync<GuildMember>(m => m.SteamId == steamId)).FirstOrDefault();
		if (member == null) return false;

		member.RankPriority = rankPriority;
		return await conn.UpdateAsync(member);
	}
}
