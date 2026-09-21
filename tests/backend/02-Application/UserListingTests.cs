using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Voltflow.Application.Common;
using Voltflow.Application.Interfaces;
using Voltflow.Application.Services;
using Voltflow.Domain.Identity;

namespace Voltflow.Tests.Application;

/// <summary>The list of users an administrator reads: a read model, so V8 (a ceiling on every list) is what it answers to.</summary>
public class UserListingTests
{
    private readonly Mock<IAppUserRepository> _users = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly AuthService _sut;

    public UserListingTests()
    {
        _sut = new AuthService(
            _users.Object, _roles.Object, new Mock<IUserRoleRepository>().Object,
            new Mock<IPasswordHasher<AppUser>>().Object, new Mock<ITokenService>().Object,
            new Mock<IUserSessionRepository>().Object, new Mock<IPasswordResetTokenRepository>().Object,
            new Mock<IHostEnvironment>().Object, new Mock<ICommandJournal>().Object, new Mock<IOperationContext>().Object);
    }

    private void RolesAre(Dictionary<Guid, IReadOnlyList<string>> byUser) =>
        _roles
            .Setup(x => x.GetNamesByUsersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<Guid, IReadOnlyList<string>>)byUser);

    [Fact]
    public async Task ListUsersAsync_ShouldCarryEachUsersRoles_AndTheirApproval()
    {
        var waiting = new AppUser("Waiting Wanda", "wanda@example.com");
        var approved = new AppUser("Approved Adam", "adam@example.com");
        approved.Approve();
        _users
            .Setup(x => x.ListPagedAsync(null, PaginationDefaults.DefaultLimit, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AppUser>(new[] { approved, waiting }, TotalCount: 80, Limit: PaginationDefaults.DefaultLimit, Offset: 0));
        RolesAre(new Dictionary<Guid, IReadOnlyList<string>> { [approved.Id] = new[] { "Manager", "Viewer" } });

        var result = await _sut.ListUsersAsync();

        result.IsSuccess.Should().BeTrue();
        var rows = result.Value!.Items;
        rows.Should().HaveCount(2);
        rows[0].Email.Should().Be("adam@example.com");
        rows[0].IsApproved.Should().BeTrue();
        rows[0].Roles.Should().Equal("Manager", "Viewer");
        rows[1].Email.Should().Be("wanda@example.com");
        rows[1].IsApproved.Should().BeFalse();
        rows[1].Roles.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(80);
        result.Value.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task ListUsersAsync_ShouldPassTheApprovalFilterOn_AndClampAnOversizedLimit()
    {
        _users
            .Setup(x => x.ListPagedAsync(false, PaginationDefaults.MaxLimit, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AppUser>(Array.Empty<AppUser>(), 0, PaginationDefaults.MaxLimit, 0));
        RolesAre(new Dictionary<Guid, IReadOnlyList<string>>());

        await _sut.ListUsersAsync(approved: false, limit: 5000);

        _users.Verify(x => x.ListPagedAsync(false, PaginationDefaults.MaxLimit, 0, It.IsAny<CancellationToken>()), Times.Once);
    }
}
