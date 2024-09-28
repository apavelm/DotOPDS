using System;
using System.Threading;
using System.Threading.Tasks;
using DotOPDS.DbLayer;
using DotOPDS.DbLayer.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;

namespace DotOPDS.Server.DevData
{
    public class TestData(IServiceProvider serviceProvider) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = serviceProvider.CreateScope())
            {

                var dbContext = scope.ServiceProvider.GetRequiredService<DotOPDSDbContext>();
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);

                // TODO: Here could be inserted any data to database
                var newRec = new SomeEntity() {Id = Guid.NewGuid(), SomeField = "test rec"};
                await dbContext.SomeEntities.AddAsync(newRec, cancellationToken).ConfigureAwait(false);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                // END OF TODO

            }


            using (var scope = serviceProvider.CreateScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

                if (await manager.FindByClientIdAsync("postman", cancellationToken) is null)
                {
                    await manager.CreateAsync(new OpenIddictApplicationDescriptor
                    {
                        ClientId = "postman",
                        ClientSecret = "postman-secret",
                        DisplayName = "Postman",
                        RedirectUris = {new Uri("https://oauth.pstmn.io/v1/callback")},
                        Permissions =
                        {
                            OpenIddictConstants.Permissions.Endpoints.Authorization,
                            OpenIddictConstants.Permissions.Endpoints.Token,
                            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                            OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                            OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                            OpenIddictConstants.Permissions.Prefixes.Scope + "api",
                            OpenIddictConstants.Permissions.ResponseTypes.Code
                        }
                    }, cancellationToken);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
