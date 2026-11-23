using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Players;

public interface IPlayerImageRepository
{
    public ProfileImageData ReadImage(string imageReference);

    public ProfileResult<string> SaveImage(ProfileImageData image);

    public void DeleteImage(string imageReference);
}
