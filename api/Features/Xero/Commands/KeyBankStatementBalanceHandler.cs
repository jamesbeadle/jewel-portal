using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Commands;

/// <summary>Upserts the one keyed statement balance for an account — keying again replaces the
/// figure, its date and the stamp.</summary>
public sealed class KeyBankStatementBalanceHandler : ICommandHandler<KeyBankStatementBalance, KeyedBankStatementBalance>
{
    private readonly JpmsContext context;

    public KeyBankStatementBalanceHandler(JpmsContext context) { this.context = context; }

    public async Task<KeyedBankStatementBalance> HandleAsync(KeyBankStatementBalance command, CancellationToken cancellationToken)
    {
        var entity = await context.KeyedBankStatementBalances
            .FirstOrDefaultAsync(balance => balance.AccountId == command.AccountId, cancellationToken);
        if (entity is null)
        {
            entity = new KeyedBankStatementBalanceEntity { AccountId = command.AccountId };
            context.KeyedBankStatementBalances.Add(entity);
        }

        entity.AccountName = command.AccountName.Trim();
        entity.Balance = command.Balance;
        entity.StatementDate = command.StatementDate;
        entity.KeyedByEmail = command.KeyedByEmail;
        entity.KeyedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
