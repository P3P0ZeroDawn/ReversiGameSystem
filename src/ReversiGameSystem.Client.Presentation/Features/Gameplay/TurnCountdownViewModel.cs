using System;
using System.ComponentModel;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Features.Gameplay;

internal sealed class TurnCountdownViewModel : INotifyPropertyChanged
{
    private const string RemainingTimeFormatKey = "Game_RemainingTurnTimeFormat";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string RemainingTime { get; private set; } = string.Empty;

    internal void Update(MatchStateSnapshot state, int durationSeconds, DateTime nowUtc)
    {
        string remainingTime = string.Empty;
        if (state.State == ConfirmedMatchState.InProgress
            && state.CurrentTurnSeatNumber.HasValue
            && state.TurnStartedAtUtc is DateTime startedAtUtc)
        {
            double elapsedSeconds = Math.Max(0, (nowUtc - startedAtUtc).TotalSeconds);
            int remainingSeconds = (int)Math.Ceiling(
                Math.Max(0, durationSeconds - elapsedSeconds));
            remainingTime = string.Format(
                LocalizationManager.Instance[RemainingTimeFormatKey],
                remainingSeconds);
        }

        if (RemainingTime != remainingTime)
        {
            RemainingTime = remainingTime;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RemainingTime)));
        }
    }
}
