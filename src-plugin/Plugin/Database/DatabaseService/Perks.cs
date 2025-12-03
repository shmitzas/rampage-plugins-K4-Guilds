using Dommel;
using K4_Guilds.Database.Models;

namespace K4_Guilds.Database;

public partial class DatabaseService
{
	public async Task<IEnumerable<GuildPerk>> GetGuildPerksAsync(int guildId)
	{
		using var conn = GetConnection();
		conn.Open();
		return await conn.SelectAsync<GuildPerk>(p => p.GuildId == guildId);
	}

	public async Task<GuildPerk?> GetGuildPerkAsync(int guildId, string perkId)
	{
		using var conn = GetConnection();
		conn.Open();
		return (await conn.SelectAsync<GuildPerk>(p => p.GuildId == guildId && p.PerkId == perkId)).FirstOrDefault();
	}

	public async Task<int> GetPerkLevelAsync(int guildId, string perkId)
	{
		var perk = await GetGuildPerkAsync(guildId, perkId);
		return perk?.Level ?? 0;
	}

	public async Task<bool> IsPerkActiveAsync(int guildId, string perkId)
	{
		var perk = await GetGuildPerkAsync(guildId, perkId);
		return perk != null && perk.Level > 0 && perk.Enabled;
	}

	public async Task<int> SetPerkLevelAsync(int guildId, string perkId, int level, bool enabled = true)
	{
		using var conn = GetConnection();
		conn.Open();

		var existing = (await conn.SelectAsync<GuildPerk>(p => p.GuildId == guildId && p.PerkId == perkId)).FirstOrDefault();

		if (existing != null)
		{
			existing.Level = level;
			existing.Enabled = enabled;
			existing.UpdatedAt = DateTime.UtcNow;
			await conn.UpdateAsync(existing);
			return level;
		}
		else
		{
			var perk = new GuildPerk
			{
				GuildId = guildId,
				PerkId = perkId,
				Level = level,
				Enabled = enabled,
				PurchasedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};
			await conn.InsertAsync(perk);
			return level;
		}
	}

	public async Task<bool> TogglePerkEnabledAsync(int guildId, string perkId)
	{
		using var conn = GetConnection();
		conn.Open();

		var existing = (await conn.SelectAsync<GuildPerk>(p => p.GuildId == guildId && p.PerkId == perkId)).FirstOrDefault();
		if (existing == null || existing.Level == 0) return false;

		existing.Enabled = !existing.Enabled;
		existing.UpdatedAt = DateTime.UtcNow;
		await conn.UpdateAsync(existing);
		return existing.Enabled;
	}
}
