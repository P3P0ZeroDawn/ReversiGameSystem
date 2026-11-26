using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Application.Players;
public interface IPlayerImageRepository
{
    public ProfileImageData ReadImage(string imageReference);
    public ProfileResult<string> SaveImage(ProfileImageData image, PlayerImageKind kind);
    public void DeleteImage(string imageReference);
}
