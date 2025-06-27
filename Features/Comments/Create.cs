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
    public record CommentData(string? Body, string? Author, int? Rate);

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
            // Ưu tiên sử dụng username từ JWT nếu có, nếu không thì dùng author từ tham số
            string? authorUsername = null;
            var currentUsername = currentUserAccessor.GetCurrentUsername();
            
            if (!string.IsNullOrEmpty(currentUsername))
            {
                // Nếu có JWT token, sử dụng username từ JWT
                var author = await context.Persons.FirstOrDefaultAsync(
                    x => x.Username == currentUsername,
                    cancellationToken
                );
                authorUsername = author?.Username;
            }
            else
            {
                // Nếu không có JWT token, sử dụng author từ tham số API
                authorUsername = message.Model.Comment.Author;
            }

            var comment = new Comment
            {
                Author = authorUsername,
                Body = message.Model.Comment.Body ?? string.Empty,
                Rate = message.Model.Comment.Rate ?? 0,
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
