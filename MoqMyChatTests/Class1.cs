using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Moq;
using MyChat.Controllers;
using MyChat.Models;
using System.Security.Claims;
using Xunit;
using Microsoft.AspNetCore.Http;


namespace MoqMyChatTests
{
    public class ChatControllerTests
    {
        private static User FakeUser => new User { Id = 10, UserName = "Bob" };

        private ChatController CreateController(
            IMemoryCache? cache = null,
            List<Message>? messages = null)
        {
            var options = new DbContextOptionsBuilder<MyChatContext>()
                .UseInMemoryDatabase($"TestDB_{Guid.NewGuid()}")
                .Options;

            var context = new MyChatContext(options);

            if (messages != null)
            {
                foreach (var m in messages)
                    context.Messages.Add(m);

                context.SaveChanges();
            }

            if (cache == null) cache = new MemoryCache(new MemoryCacheOptions());

            var mockUserManager = MockUserManager(FakeUser);

            var controller = new ChatController(context, mockUserManager.Object, cache);
            controller.ControllerContext = FakeHttpContext();
            return controller;
        }

        private static Mock<UserManager<User>> MockUserManager(User user)
        {
            var store = new Mock<IUserStore<User>>();
            var mock = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);

            mock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            return mock;
        }

        private static ControllerContext FakeHttpContext()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, FakeUser.Id.ToString()) },
                "mock"));

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task Index_ReturnsViewResult()
        {
            var controller = CreateController();

            var result = await controller.Index();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Index_StoresCurrentUserIdInViewBag()
        {
            var controller = CreateController();

            await controller.Index();

            Assert.Equal(FakeUser.Id, controller.ViewBag.MeId);
        }

        [Fact]
        public async Task Index_ReturnsMessagesFromDatabase_WhenCacheIsEmpty()
        {
            var controller = CreateController(messages: GetFakeMessages());

            var result = await controller.Index();
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Message>>(view.Model);

            Assert.NotNull(model);
            Assert.True(model.Count() <= 30);
        }

        [Fact]
        public async Task Index_ReturnsMessagesFromCache_WhenCacheExists()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cached = GetFakeMessages().Take(1).ToList();
            cache.Set("lastMessages", cached);

            var controller = CreateController(cache: cache, messages: GetFakeMessages());

            var result = await controller.Index();
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Message>>(view.Model);

            Assert.Single(model);
        }

        [Fact]
        public async Task Index_LoadsAtMost30Messages()
        {
            var many = Enumerable.Range(1, 100).Select(i => new Message { Id = i, Text = "M" }).ToList();
            var controller = CreateController(messages: many);

            var result = await controller.Index();
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Message>>(view.Model);

            Assert.True(model.Count() <= 30);
        }

        private static List<Message> GetFakeMessages() =>
            new()
            {
                new Message { Id = 1, Text = "Hi" },
                new Message { Id = 2, Text = "How" },
                new Message { Id = 3, Text = "Yo" }
            };
    }
}
