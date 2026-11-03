export type StatusOs =
  | "Recebida"
  | "EmDiagnostico"
  | "AguardandoAprovacao"
  | "Aprovada"
  | "EmExecucao"
  | "Finalizada"
  | "Entregue"
  | "Cancelada";

export type SituacaoDaVersao = "Pendente" | "Aprovada" | "Recusada" | "Superada";

export type TipoPecaInsumo = "Peca" | "Insumo";

export type ClienteDto = {
  id: string;
  nome: string;
  tipoDocumento: "Cpf" | "Cnpj";
  documento: string;
  email: string;
  telefone: string | null;
  ativo: boolean;
};

export type VeiculoDto = {
  id: string;
  placa: string;
  marca: string;
  modelo: string;
  anoFabricacao: number;
  proprietarioId: string;
  ativo: boolean;
};

export type ServicoDto = {
  id: string;
  nome: string;
  descricao: string | null;
  preco: number;
  ativo: boolean;
};

export type PecaInsumoDto = {
  id: string;
  nome: string;
  tipo: TipoPecaInsumo;
  unidadeDeMedida: string;
  preco: number;
  saldo: number;
  ativo: boolean;
};

export type SaldoDto = {
  id: string;
  nome: string;
  unidadeDeMedida: string;
  saldo: number;
};

export type OrdemDeServicoResumoDto = {
  id: string;
  numero: string;
  status: StatusOs;
  clienteId: string;
  veiculoId: string;
  abertaEm: string;
};

export type PaginaDeOrdensDeServicoDto = {
  itens: OrdemDeServicoResumoDto[];
  pagina: number;
  tamanho: number;
  total: number;
};

export type ItemDaVersaoDto = {
  tipo: "Servico" | "PecaInsumo";
  referenciaId: string;
  descricao: string;
  quantidade: number;
  precoUnitario: number;
  subtotal: number;
};

export type VersaoDoOrcamentoDto = {
  numero: number;
  total: number;
  situacao: SituacaoDaVersao;
  geradaEm: string;
  motivoDaRecusa: string | null;
  itens: ItemDaVersaoDto[];
};

export type HistoricoDeStatusDto = {
  origem: StatusOs | null;
  destino: StatusOs;
  momento: string;
  autor: string;
};

export type ServicoDaOsDto = {
  servicoId: string;
  nome: string;
  preco: number;
};

export type PecaInsumoDaOsDto = {
  pecaInsumoId: string;
  nome: string;
  unidadeDeMedida: string;
  quantidade: number;
  preco: number;
};

export type OrdemDeServicoDetalheDto = {
  id: string;
  numero: string;
  status: StatusOs;
  abertaEm: string;
  cliente: ClienteDto;
  veiculo: VeiculoDto;
  servicos: ServicoDaOsDto[];
  versoesDoOrcamento: VersaoDoOrcamentoDto[];
  historico: HistoricoDeStatusDto[];
  pecasInsumos: PecaInsumoDaOsDto[];
  motivoDaUltimaRecusa: string | null;
};

export type ServicoDaCombinacaoDto = {
  id: string;
  nome: string;
  ativo: boolean;
};

export type TempoMedioDaCombinacaoDto = {
  servicos: ServicoDaCombinacaoDto[];
  quantidadeDeOs: number;
  tempoMedioEmSegundos: number;
};

export type TempoMedioDeExecucaoDto = {
  quantidadeDeOs: number;
  tempoMedioEmSegundos: number | null;
  combinacoes: TempoMedioDaCombinacaoDto[];
};

export type ItemDoOrcamentoPublicoDto = {
  descricao: string;
  quantidade: number;
  precoUnitario: number;
  subtotal: number;
};

export type ConsultaPublicaDaOsDto = {
  numero: string;
  status: StatusOs;
  abertaEm: string;
  historico: HistoricoDeStatusDto[];
  orcamentoVigente: {
    versao: number;
    situacao: SituacaoDaVersao;
    itens: ItemDoOrcamentoPublicoDto[];
    total: number;
  } | null;
};

export type PoliticaDto = {
  politica: string;
};
