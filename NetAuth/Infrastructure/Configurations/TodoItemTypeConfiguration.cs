using System.Globalization;
using EFCore.NamingConventions.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetAuth.Domain.TodoItems;
using NetAuth.Domain.Users;

namespace NetAuth.Infrastructure.Configurations;

internal sealed class TodoItemTypeConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.HasKey(todoItem => todoItem.Id);

        builder.Property(todoItem => todoItem.UserId).IsRequired();

        var snakeCaseNameRewriter = new SnakeCaseNameRewriter(CultureInfo.InvariantCulture);
        builder.ComplexProperty(todoItem => todoItem.Title, titleBuilder =>
        {
            titleBuilder.Property(title => title.Value)
                .HasColumnName(snakeCaseNameRewriter.RewriteName(nameof(TodoItem.Title)))
                .IsRequired()
                .HasMaxLength(TodoTitle.MaxLength);
        });

        builder.ComplexProperty(todoItem => todoItem.Description, descriptionBuilder =>
        {
            descriptionBuilder.Property(description => description.Value)
                .HasColumnName(snakeCaseNameRewriter.RewriteName(nameof(TodoItem.Description)))
                .HasMaxLength(TodoDescription.MaxLength);
        });

        builder.Property(todoItem => todoItem.IsCompleted).IsRequired();
        builder.Property(todoItem => todoItem.CompletedOnUtc);

        builder.Property(todoItem => todoItem.DueDateOnUtc).IsRequired();
        builder.Property(todoItem => todoItem.Labels).IsRequired();

        builder.Property(todoItem => todoItem.CreatedOnUtc).IsRequired();
        builder.Property(todoItem => todoItem.ModifiedOnUtc);
        builder.Property(todoItem => todoItem.DeletedOnUtc);
        builder.Property(todoItem => todoItem.IsDeleted).HasDefaultValue(false);

        // -------------------- Indexes & Relationships --------------------

        // Query filter to exclude soft-deleted todo items for any queries targeting the TodoItem entity
        builder.HasQueryFilter(todoItem => !todoItem.IsDeleted && !todoItem.User.IsDeleted);

        // Each todo item is associated with a user
        builder.HasOne<User>(todoItem => todoItem.User)
            .WithMany()
            .HasForeignKey(todoItem => todoItem.UserId);
    }
}