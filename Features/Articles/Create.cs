using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Articles;

public class Create
{
    public class ArticleData
    {
        public string? Title { get; init; }

        public string? Description { get; init; }

        public string? Body { get; init; }

        public string[]? TagList { get; init; }
    }

    public class ArticleDataValidator : AbstractValidator<ArticleData>
    {
        public ArticleDataValidator()
        {
            RuleFor(x => x.Title).NotNull().NotEmpty();
            RuleFor(x => x.Description).NotNull().NotEmpty();
            RuleFor(x => x.Body).NotNull().NotEmpty();
        }
    }

    public record Command(ArticleData Article) : IRequest<ArticleEnvelope>;

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator() =>
            RuleFor(x => x.Article).NotNull().SetValidator(new ArticleDataValidator());
    }

    public class Handler(ConduitContext context, ICurrentUserAccessor currentUserAccessor)
        : IRequestHandler<Command, ArticleEnvelope>
    {
        public async Task<ArticleEnvelope> Handle(
            Command message,
            CancellationToken cancellationToken
        )
        {
            var author = await context.Persons.FirstAsync(
                x => x.Username == currentUserAccessor.GetCurrentUsername(),
                cancellationToken
            );
            var tags = new List<Tag>();
            foreach (var tag in (message.Article.TagList ?? Enumerable.Empty<string>()))
            {
                var t = await context.Tags.FindAsync(tag);
                if (t == null)
                {
                    t = new Tag { TagId = tag };
                    await context.Tags.AddAsync(t, cancellationToken);
                    //save immediately for reuse
                    await context.SaveChangesAsync(cancellationToken);
                }
                tags.Add(t);
            }

            var article = new Article
            {
                Author = author,
                Body = message.Article.Body,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Description = message.Article.Description,
                Title = message.Article.Title,
                Slug = message.Article.Title.GenerateSlug()
            };
            await context.Articles.AddAsync(article, cancellationToken);

            await context.ArticleTags.AddRangeAsync(
                tags.Select(x => new ArticleTag { Article = article, Tag = x }),
                cancellationToken
            );

            await context.SaveChangesAsync(cancellationToken);

            return new ArticleEnvelope(article);
        }
    }
}
