using AutoMapper;
using Business.Interfaces;
using Business.Services;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace CoreTests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly UserService _userService;
        private readonly Mock<IEmailService> _emailMock;
        private readonly Mock<ITokenService> _tokenMock;
        public UserServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _loggerMock = new Mock<ILogger<UserService>>();
            _mapperMock = new Mock<IMapper>();
            _emailMock = new Mock<IEmailService>();
            _tokenMock = new Mock<ITokenService>();
            _userService = new UserService(_userRepositoryMock.Object, _emailMock.Object, _tokenMock.Object, _loggerMock.Object, _mapperMock.Object);
        }

        #region Create User

        [Fact]
        public async Task CreateUser_ShouldRegisterUser_WhenValidInputProvided()
        {
            // Arrange
            var newUser = new UserRegistrationModel
            {
                UserName = "newUser",
                Email = "new@example.com",
                Password = "new123Password@",
                Role = "User"
            };
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newUser.Password);
            _userRepositoryMock.Setup(repo => repo.GetByEmail(newUser.Email))
                .ReturnsAsync((User)null);

            _userRepositoryMock.Setup(repo => repo.GetByUserName(newUser.UserName))
                .ReturnsAsync((User)null);

            // Act
            await _userService.CreateUser(newUser);
            var isValidPassword = BCrypt.Net.BCrypt.Verify(newUser.Password, hashedPassword);
            // Assert
            _userRepositoryMock.Verify(repo => repo.Add(It.Is<User>(u =>
                u.Username == newUser.UserName &&
                u.Email == newUser.Email &&
                isValidPassword &&
                u.Role == User.ConvertToUserRole(newUser.Role)
            )), Times.Once);
        }

        [Fact]
        public async Task CreateUser_ShouldThrowException_WhenUserAlreadyExists()
        {
            // Arrange
            var newUser = new UserRegistrationModel
            {
                UserName = "existingUser",
                Email = "existing@example.com",
                Password = "Password00@",
                Role = "User"
            };

            var existingUser = new User("existingUser", BCrypt.Net.BCrypt.HashPassword("hashedPassword"), "existing@example.com", User.ConvertToUserRole("User"));

            _userRepositoryMock.Setup(repo => repo.GetByEmail(newUser.Email))
                .ReturnsAsync(existingUser);

            _userRepositoryMock.Setup(repo => repo.GetByUserName(newUser.UserName))
                .ReturnsAsync(existingUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => _userService.CreateUser(newUser));

            Assert.Equal("User already exists", exception.Message);

            _userRepositoryMock.Verify(repo => repo.Add(It.IsAny<User>()), Times.Never);
        }

        [Theory]
        [InlineData("", "email@example.com", "Password00@", "User")] // Missing UserName
        [InlineData("user", "", "Password00@", "User")]             // Missing Email
        [InlineData("user", "email@example.com", "", "User")]    // Missing Password
        [InlineData("user", "email@example.com", "Password00@", "")]// Missing Role
        public async Task CreateUser_ShouldThrowException_WhenRequiredFieldsAreMissing(
            string userName, string email, string password, string role)
        {
            // Arrange
            var newUser = new UserRegistrationModel
            {
                UserName = userName,
                Email = email,
                Password = password,
                Role = role
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _userService.CreateUser(newUser));

            _userRepositoryMock.Verify(repo => repo.Add(It.IsAny<User>()), Times.Never);
        }



        [Fact]
        public async Task CreateUser_ShouldLogError_AndThrowException_WhenErrorOccurs()
        {
            // Arrange
            var newUser = new UserRegistrationModel
            {
                UserName = "newUser",
                Email = "new@example.com",
                Password = "Password00@",
                Role = "User"
            };

            _userRepositoryMock.Setup(repo => repo.GetByEmail(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var exception = await Assert.ThrowsAsync<Exception>(() => _userService.CreateUser(newUser));

            // Assert
            Assert.Equal($"An error occurred while registering the user with Email {newUser.Email}.", exception.Message);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An error occurred while registering the user")),
                    It.Is<Exception>(ex => ex.Message == "Database error"),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once);

            _userRepositoryMock.Verify(repo => repo.Add(It.IsAny<User>()), Times.Never);
        }



        #endregion

        #region Update User

        [Fact]
        public async Task UpdateUser_ShouldUpdateUser_WhenUserExists()
        {
            // Arrange
            var userId = 1;
            var existingUser = new User("oldUser", BCrypt.Net.BCrypt.HashPassword("oldPassword"), "old@example.com", User.ConvertToUserRole("User"));
            var updatedUserModel = new UserUpdateModel
            {
                UserName = "newUser",
                Email = "new@example.com",
                Password = "newPassword",
                Role = "Admin"
            };

            _userRepositoryMock.Setup(repo => repo.GetById(userId))
                .ReturnsAsync(existingUser);

            // Act
            await _userService.UpdateUser(userId, updatedUserModel);
            var passwordIsValid = BCrypt.Net.BCrypt.Verify(updatedUserModel.Password, existingUser.Password);

            // Assert
            _userRepositoryMock.Verify(repo => repo.Update(It.Is<User>(u =>
                u.Username == updatedUserModel.UserName &&
                u.Email == updatedUserModel.Email &&
                passwordIsValid &&
                u.Role == User.ConvertToUserRole(updatedUserModel.Role)
            )), Times.Once);
            Assert.Equal(updatedUserModel.UserName, existingUser.Username); // Username is updated
            Assert.Equal(updatedUserModel.Email, existingUser.Email); // Email is updated
            Assert.True(BCrypt.Net.BCrypt.Verify(updatedUserModel.Password, existingUser.Password)); // Password is updated
            Assert.Equal(User.ConvertToUserRole(updatedUserModel.Role), existingUser.Role);
        }

        [Fact]
        public async Task UpdateUser_ShouldNotUpdate_WhenNoChangesProvided()
        {
            // Arrange
            var userId = 1;
            var existingUser = new User("oldUser", BCrypt.Net.BCrypt.HashPassword("oldPassword"), "old@example.com", User.ConvertToUserRole("User"));
            var updatedUserModel = new UserUpdateModel
            {
                UserName = "oldUser",
                Email = "old@example.com",
                Password = "oldPassword",
                Role = "User"
            };

            _userRepositoryMock.Setup(repo => repo.GetById(userId))
                .ReturnsAsync(existingUser);

            // Act
            await _userService.UpdateUser(userId, updatedUserModel);

            // Assert
            _userRepositoryMock.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task UpdateUser_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = 1;
            var updatedUserModel = new UserUpdateModel
            {
                UserName = "newUser",
                Email = "new@example.com",
                Password = "newPassword",
                Role = "Admin"
            };

            _userRepositoryMock.Setup(repo => repo.GetById(userId))
                .ReturnsAsync((User)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _userService.UpdateUser(userId, updatedUserModel));

            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("User with ID")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ShouldLogAndThrowException_WhenRepositoryUpdateFails()
        {
            // Arrange
            var userId = 1;
            var existingUser = new User("oldUser", BCrypt.Net.BCrypt.HashPassword("oldPassword"), "old@example.com", User.ConvertToUserRole("User"));
            var updatedUserModel = new UserUpdateModel
            {
                UserName = "newUser",
                Email = "new@example.com",
                Password = "newPassword",
                Role = "Admin"
            };

            _userRepositoryMock.Setup(repo => repo.GetById(userId))
                .ReturnsAsync(existingUser);

            _userRepositoryMock.Setup(repo => repo.Update(It.IsAny<User>()))
                .ThrowsAsync(new Exception("Update failed"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _userService.UpdateUser(userId, updatedUserModel));

            Assert.Equal("An error occurred while updating the user.", exception.Message);

            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("An error occurred while updating the user")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ShouldUpdatePartialFields_WhenOnlySomeFieldsAreChanged()
        {
            // Arrange
            var userId = 1;
            var existingUser = new User("oldUser", BCrypt.Net.BCrypt.HashPassword("oldPassword"), "old@example.com", User.ConvertToUserRole("User"));
            var updatedUserModel = new UserUpdateModel
            {
                Email = "new@example.com"
            };

            _userRepositoryMock.Setup(repo => repo.GetById(userId))
                .ReturnsAsync(existingUser);

            // Act
            await _userService.UpdateUser(userId, updatedUserModel);

            // Assert
            _userRepositoryMock.Verify(repo => repo.Update(It.Is<User>(u =>
                u.Email == updatedUserModel.Email &&
                u.Username == existingUser.Username &&
                u.Password == existingUser.Password &&
                u.Role == existingUser.Role
            )), Times.Once);
        }

        #endregion

    }
}
