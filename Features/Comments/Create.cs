using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Comments;

public class Create
{
    public record CommentData(string? Body);

    public record Command(Model Model, string Slug) : IRequest<CommentEnvelope>;

    public record Model(CommentData Comment) : IRequest<CommentEnvelope>;

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator() => RuleFor(x => x.Model.Comment.Body).NotEmpty();
    }

    public class Handler(ConduitContext context, ICurrentUserAccessor currentUserAccessor)
        : IRequestHandler<Command, CommentEnvelope>
    {
        public async Task<CommentEnvelope> Handle(
            Command message,
            CancellationToken cancellationToken
        )
        {
            var article = await context
                .Articles.Include(x => x.Comments)
                .FirstOrDefaultAsync(x => x.Slug == message.Slug, cancellationToken);

            if (article == null)
            {
                throw new RestException(
                    HttpStatusCode.NotFound,
                    new { Article = Constants.NOT_FOUND }
                );
            }
            //abcxyz
            // Lấy thông tin author nếu có JWT token, nếu không thì để null
            Person? author = null;
            var currentUsername = currentUserAccessor.GetCurrentUsername();
            if (!string.IsNullOrEmpty(currentUsername))
            {
                author = await context.Persons.FirstOrDefaultAsync(
                    x => x.Username == currentUsername,
                    cancellationToken
                );
            }

            var comment = new Comment
            {
                Author = author,
                Body = message.Model.Comment.Body ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await context.Comments.AddAsync(comment, cancellationToken);

            article.Comments.Add(comment);

            await context.SaveChangesAsync(cancellationToken);

            return new CommentEnvelope(comment);
        }
    }
}
