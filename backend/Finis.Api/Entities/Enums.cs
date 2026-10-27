namespace Finis.Api.Entities;

public enum TransactionType
{
    Entrada = 1,
    SaidaFixa = 2,
    Diario = 3
}

public enum TransactionStatus
{
    Realizado = 1,
    Previsto = 2
}

public enum PaymentMethod
{
    Pix = 1,
    CartaoCredito = 2,
    CartaoDebito = 3,
    Dinheiro = 4,
    Boleto = 5,
    Transferencia = 6,
    Outro = 7
}

public enum AccountType
{
    Pessoal = 1,
    Empresa = 2,
    Investimento = 3,
    Outro = 4
}
