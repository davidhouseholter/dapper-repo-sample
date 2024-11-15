using Core.Repository.Entity;
using SampleMVC.Repository;

namespace SampleMVC.Services;

public interface IUserService
{
    User GetUserByID(long id);
    User AddUser(string name);
}

public class UserService : IUserService
{
    private readonly UserRepository _userRepository;
    public UserService(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    public User GetUserByID(long id)
    {
        var user = _userRepository.Find(i => i.Id == id);
        return user;
    }
    public User AddUser(string name)
    {
        var user = new User { Name = name };
        _userRepository.Insert(user);
        return user;
    }
}
