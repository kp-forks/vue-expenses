﻿using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using vue_expenses_api.Domain;
using vue_expenses_api.Dtos;
using vue_expenses_api.Infrastructure;
using vue_expenses_api.Infrastructure.Exceptions;
using vue_expenses_api.Infrastructure.Security;

namespace vue_expenses_api.Features.Expenses
{
    public class ExpenseCategoryCreate
    {
        public class Command : IRequest<ExpenseCategoryDto>
        {
            public Command(
                string name,
                string description)
            {
                Name = name;
                Description = description;
            }

            public string Name { get; }
            public string Description { get; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Name).NotNull().NotEmpty();
            }
        }

        public class Handler : IRequestHandler<Command, ExpenseCategoryDto>
        {
            private readonly ExpensesContext _context;
            private readonly ICurrentUser _currentUser;

            public Handler(
                ExpensesContext db,
                ICurrentUser currentUser)
            {
                _context = db;
                _currentUser = currentUser;
            }

            public async Task<ExpenseCategoryDto> Handle(
                Command request,
                CancellationToken cancellationToken)
            {
                if (await _context.ExpenseCategories.AnyAsync(
                    x => x.Name == request.Name,
                    cancellationToken))
                {
                    throw new HttpException(
                        HttpStatusCode.BadRequest,
                        new
                        {
                            Error = $"There is already a category with name {request.Name}."
                        });
                }

                var user = _context.Users.Single(x => x.Email == _currentUser.EmailId);

                var expenseCategory = new ExpenseCategory(
                    request.Name,
                    request.Description,
                    0,
                    string.Empty,
                    user);

                await _context.ExpenseCategories.AddAsync(
                    expenseCategory,
                    cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                return new ExpenseCategoryDto(
                    expenseCategory.Id,
                    expenseCategory.Name,
                    expenseCategory.Description);
            }
        }
    }
}