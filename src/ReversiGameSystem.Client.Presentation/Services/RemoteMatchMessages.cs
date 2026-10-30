using System;
using System.ServiceModel;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal static class RemoteMatchMessages
{
    internal static string Key(Exception exception, bool guest = false) => exception switch
    {
        FaultException<Proxy.SessionFault> when guest => "RemoteMatch_GuestUnavailable",
        FaultException<Proxy.SessionFault> => "RemoteMatch_SessionExpired",
        FaultException<Proxy.MatchFault> fault => fault.Detail.Reason switch
        {
            Proxy.MatchFailure.CannotJoinOwnMatch => "JoinMatch_CannotJoinOwnMatchMessage",
            Proxy.MatchFailure.InvalidMatchCode => "JoinMatch_InvalidMatchCodeMessage",
            Proxy.MatchFailure.MatchCodeUnavailable => "JoinMatch_MatchCodeNotAvailableMessage",
            Proxy.MatchFailure.InvalidTurnDuration => "MatchCreation_InvalidTurnTimeMessage",
            Proxy.MatchFailure.MatchNotWaiting => "JoinMatch_NoLongerAvailableMessage",
            _ => "RemoteMatch_AccessRejected"
        },
        FaultException<Proxy.ChatFault> fault => "Chat_" + fault.Detail.Reason + "Message",
        FaultException<Proxy.ServiceFault> => "RemoteMatch_ServerFailure",
        CommunicationException or TimeoutException => "RemoteMatch_ConnectionFailed",
        _ => "RemoteMatch_SessionExpired"
    };
}
