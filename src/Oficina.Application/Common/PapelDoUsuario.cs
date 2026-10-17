namespace Oficina.Application.Common;

[Flags]
public enum PapelDoUsuario
{
    Nenhum = 0,
    Atendente = 1,
    Mecanico = 2,
    Gerente = 4
}
