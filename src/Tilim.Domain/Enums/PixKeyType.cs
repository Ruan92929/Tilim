namespace Tilim.Domain.Enums;

/// <summary>
/// The five key formats the DICT (Diretório de Identificadores de Contas
/// Transacionais) accepts for Pix, per the Banco Central Pix manuals.
/// </summary>
public enum PixKeyType
{
    Cpf = 0,
    Cnpj = 1,
    Email = 2,
    Telefone = 3,
    Aleatoria = 4
}
