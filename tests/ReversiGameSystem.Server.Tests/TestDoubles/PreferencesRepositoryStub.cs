using System;
using ReversiGameSystem.Server.Application.Preferences;

namespace ReversiGameSystem.Server.Tests.TestDoubles;

internal sealed class PreferencesRepositoryStub : IPreferencesRepository
{
    internal PlayerPreferences StoredPreferences { get; set; } = new PlayerPreferences
    {
        PreferredLocaleCode = "es-MX",
        PieceStyleCode = "STANDARD",
        TimeZoneId = null
    };

    internal bool IsActiveAccount { get; set; } = true;

    internal PreferencesStatus SaveStatus { get; set; } = PreferencesStatus.Success;

    internal PreferencesStatus SetBoardBackgroundStatus { get; set; } =
        PreferencesStatus.Success;

    internal Exception? AssociationFailure { get; set; }

    internal int ReadCount { get; private set; }

    internal int WriteCount { get; private set; }

    internal long LastAccountId { get; private set; }

    internal string LastImageReference { get; private set; } = string.Empty;

    public PreferencesAccess ReadPreferences(long accountId)
    {
        ReadCount++;
        LastAccountId = accountId;
        PreferencesAccess access = IsActiveAccount
            ? new PreferencesAccess(StoredPreferences)
            : new PreferencesAccess(PreferencesStatus.InactiveAccount);
        return access;
    }

    public PreferencesStatus SavePreferences(long accountId, PlayerPreferences preferences)
    {
        LastAccountId = accountId;
        if (SaveStatus != PreferencesStatus.Success)
        {
            return SaveStatus;
        }

        StoredPreferences = preferences;
        WriteCount++;
        return PreferencesStatus.Success;
    }

    public PreferencesStatus SetBoardBackground(long accountId, string imageReference)
    {
        LastAccountId = accountId;
        LastImageReference = imageReference;
        if (AssociationFailure != null)
        {
            throw AssociationFailure;
        }

        if (SetBoardBackgroundStatus != PreferencesStatus.Success)
        {
            return SetBoardBackgroundStatus;
        }

        StoredPreferences.BoardBackgroundReference = imageReference;
        WriteCount++;
        return PreferencesStatus.Success;
    }
}
