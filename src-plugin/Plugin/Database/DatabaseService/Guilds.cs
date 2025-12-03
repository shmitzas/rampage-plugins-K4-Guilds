using System.Data;
using Dommel;
using K4_Guilds.Database.Models;

namespace K4_Guilds.Database;

public partial class DatabaseService
{
	public async Task<Guild?> GetGuildAsync(int guildId)
	{
		using var conn = GetConnection();
		conn.Open();

		var guild = await conn.GetAsync<Guild>(guildId);
		if (guild != null)
			await LoadGuildRelationsAsync(conn, guild);

		return guild;
	}

	public async Task<Guild?> GetGuildByNameAsync(string name)
	{
		using var conn = GetConnection();
		conn.Open();

		var guild = (await conn.SelectAsync<Guild>(g => g.Name == name)).FirstOrDefault();
		if (guild != null)
			await LoadGuildRelationsAsync(conn, guild);

		return guild;
	}

	public async Task<Guild?> GetPlayerGuildAsync(ulong steamId)
	{
		using var conn = GetConnection();
		conn.Open();

		var member = (await conn.SelectAsync<GuildMember>(m => m.SteamId == steamId)).FirstOrDefault();
		if (member == null) return null;

		var guild = await conn.GetAsync<Guild>(member.GuildId);
		if (guild != null)
			await LoadGuildRelationsAsync(conn, guild);

		return guild;
	}

	public async Task<Guild> CreateGuildAsync(ulong leaderSteamId, string leaderName, string name, string tag, int leaderRankPriority)
	{
		using var conn = GetConnection();
		conn.Open();
		using var transaction = conn.BeginTransaction();

		try
		{
			var guild = new Guild
			{
				Name = name,
				Tag = tag,
				LeaderSteamId = leaderSteamId,
				BankBalance = 0,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};
			guild.Id = Convert.ToInt32(await conn.InsertAsync(guild, transaction));

			var member = new GuildMember
			{
				GuildId = guild.Id,
				SteamId = leaderSteamId,
				PlayerName = leaderName,
				RankPriority = leaderRankPriority,
				JoinedAt = DateTime.UtcNow,
				LastSeen = DateTime.UtcNow
			};
			await conn.InsertAsync(member, transaction);

			transaction.Commit();

			guild.Members = [member];
			guild.Upgrades = [];
			guild.EnabledPerks = [];
			return guild;
		}
		catch
		{
			transaction.Rollback();
			throw;
		}
	}

	public async Task<bool> DisbandGuildAsync(int guildId)
	{
		using var conn = GetConnection();
		conn.Open();

		var guild = await conn.GetAsync<Guild>(guildId);
		if (guild == null) return false;

		return await conn.DeleteAsync(guild);
	}

	public async Task<IReadOnlyList<Guild>> GetAllGuildsAsync(int page, int pageSize)
	{
		using var conn = GetConnection();
		conn.Open();

		var allGuilds = (await conn.SelectAsync<Guild>(g => true))
			.OrderByDescending(g => g.CreatedAt)
			.Skip(page * pageSize)
			.Take(pageSize)
			.ToList();

		if (allGuilds.Count == 0) return allGuilds;

		await LoadGuildsRelationsBatchAsync(conn, allGuilds);
		return allGuilds;
	}

	public async Task UpdateGuildNameAsync(int guildId, string newName)
	{
		using var conn = GetConnection();
		conn.Open();

		var guild = await conn.GetAsync<Guild>(guildId);
		if (guild != null)
		{
			guild.Name = newName;
			guild.UpdatedAt = DateTime.UtcNow;
			await conn.UpdateAsync(guild);
		}
	}

	private static async Task LoadGuildRelationsAsync(IDbConnection conn, Guild guild)
	{
		guild.Members = [.. await conn.SelectAsync<GuildMember>(m => m.GuildId == guild.Id)];
		guild.Upgrades = [.. await conn.SelectAsync<GuildUpgrade>(u => u.GuildId == guild.Id)];
		guild.EnabledPerks = [.. await conn.SelectAsync<GuildPerk>(p => p.GuildId == guild.Id)];
	}

	private static async Task LoadGuildsRelationsBatchAsync(IDbConnection conn, List<Guild> guilds)
	{
		var guildIds = guilds.Select(g => g.Id).ToHashSet();

		var allMembers = (await conn.SelectAsync<GuildMember>(m => guildIds.Contains(m.GuildId))).ToLookup(m => m.GuildId);
		var allUpgrades = (await conn.SelectAsync<GuildUpgrade>(u => guildIds.Contains(u.GuildId))).ToLookup(u => u.GuildId);
		var allPerks = (await conn.SelectAsync<GuildPerk>(p => guildIds.Contains(p.GuildId))).ToLookup(p => p.GuildId);

		foreach (var guild in guilds)
		{
			guild.Members = [.. allMembers[guild.Id]];
			guild.Upgrades = [.. allUpgrades[guild.Id]];
			guild.EnabledPerks = [.. allPerks[guild.Id]];
		}
	}
}
