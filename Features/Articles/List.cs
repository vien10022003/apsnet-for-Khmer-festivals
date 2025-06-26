using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Articles;

public class List
{
    public record Query(
        string Tag,
        string Author,
        string FavoritedUsername,
        int? Limit,
        int? Offset,
        bool IsFeed = false
    ) : IRequest<ArticlesEnvelope>;

    public class QueryHandler(ConduitContext context, ICurrentUserAccessor currentUserAccessor)
        : IRequestHandler<Query, ArticlesEnvelope>
    {
        public async Task<ArticlesEnvelope> Handle(
            Query message,
            CancellationToken cancellationToken
        )
        {
            var queryable = context.Articles.GetAllData();

            if (message.IsFeed && currentUserAccessor.GetCurrentUsername() != null)
            {
                var currentUser = await context
                    .Persons.Include(x => x.Following)
                    .FirstOrDefaultAsync(
                        x => x.Username == currentUserAccessor.GetCurrentUsername(),
                        cancellationToken
                    );

                if (currentUser is null)
                {
                    throw new RestException(
                        HttpStatusCode.NotFound,
                        new { User = Constants.NOT_FOUND }
                    );
                }
                queryable = queryable.Where(x =>
                    currentUser.Following.Select(y => y.TargetId).Contains(x.Author!.PersonId)
                );
            }

            if (!string.IsNullOrWhiteSpace(message.Tag))
            {
                var tag = await context.ArticleTags.FirstOrDefaultAsync(
                    x => x.TagId == message.Tag,
                    cancellationToken
                );
                if (tag != null)
                {
                    queryable = queryable.Where(x =>
                        x.ArticleTags.Select(y => y.TagId).Contains(tag.TagId)
                    );
                }
                else
                {
                    return new ArticlesEnvelope();
                }
            }

            if (!string.IsNullOrWhiteSpace(message.Author))
            {
                var author = await context.Persons.FirstOrDefaultAsync(
                    x => x.Username == message.Author,
                    cancellationToken
                );
                if (author != null)
                {
                    queryable = queryable.Where(x => x.Author == author);
                }
                else
                {
                    return new ArticlesEnvelope();
                }
            }

            if (!string.IsNullOrWhiteSpace(message.FavoritedUsername))
            {
                var author = await context.Persons.FirstOrDefaultAsync(
                    x => x.Username == message.FavoritedUsername,
                    cancellationToken
                );
                if (author != null)
                {
                    queryable = queryable.Where(x =>
                        x.ArticleFavorites.Any(y => y.PersonId == author.PersonId)
                    );
                }
                else
                {
                    return new ArticlesEnvelope();
                }
            }

            var articles = await queryable
                .OrderByDescending(x => x.CreatedAt)
                .Skip(message.Offset ?? 0)
                .Take(message.Limit ?? 20)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return new ArticlesEnvelope { Articles = articles, ArticlesCount = queryable.Count() };
        }
    }
}
