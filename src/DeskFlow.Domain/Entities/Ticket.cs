using DeskFlow.Domain.Enums;
using DeskFlow.Domain.Exceptions;

namespace DeskFlow.Domain.Entities;

public class Ticket
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 10_000;
    public const int ResolutionSummaryMaxLength = 5_000;
    public const int ReopenWindowDays = 7;

    private static readonly Dictionary<TicketStatus, HashSet<TicketStatus>> TransicoesPermitidas = new()
    {
        [TicketStatus.New]              = [TicketStatus.Triaged, TicketStatus.Cancelled],
        [TicketStatus.Triaged]          = [TicketStatus.InProgress, TicketStatus.Cancelled],
        [TicketStatus.InProgress]       = [TicketStatus.WaitingRequester, TicketStatus.WaitingThirdParty, TicketStatus.Resolved],
        [TicketStatus.WaitingRequester] = [TicketStatus.InProgress, TicketStatus.Resolved, TicketStatus.Cancelled],
        [TicketStatus.WaitingThirdParty]= [TicketStatus.InProgress, TicketStatus.Resolved],
        [TicketStatus.Resolved]         = [TicketStatus.Closed, TicketStatus.InProgress],
        [TicketStatus.Closed]           = [TicketStatus.InProgress], // apenas reabertura
        [TicketStatus.Cancelled]        = []
    };

    public Guid Id { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid RequesterId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public Guid CategoryId { get; private set; }
    public TicketPriority Priority { get; private set; }
    public TicketStatus Status { get; private set; }
    public Guid? AssignedAgentId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? FirstResponseAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public DateTimeOffset FirstResponseDueAtUtc { get; private set; }
    public DateTimeOffset ResolutionDueAtUtc { get; private set; }
    public string? ResolutionSummary { get; private set; }
    public int ReopenCount { get; private set; }

    // Token de concorrência do EF Core — impede sobrescrita silenciosa por atualizações simultâneas.
    public byte[] RowVersion { get; private set; } = [];

    private readonly List<TicketComment> _comments = [];
    private readonly List<TicketAttachment> _attachments = [];
    private readonly List<TicketStatusHistory> _statusHistory = [];
    private readonly List<TicketAssignmentHistory> _assignmentHistory = [];

    public IReadOnlyList<TicketComment> Comments => _comments.AsReadOnly();
    public IReadOnlyList<TicketAttachment> Attachments => _attachments.AsReadOnly();
    public IReadOnlyList<TicketStatusHistory> StatusHistory => _statusHistory.AsReadOnly();
    public IReadOnlyList<TicketAssignmentHistory> AssignmentHistory => _assignmentHistory.AsReadOnly();
    public TicketRating? Rating { get; private set; }

    private Ticket() { }

    public static Ticket Create(
        string number,
        string title,
        string description,
        Guid requesterId,
        Guid departmentId,
        Guid categoryId,
        TicketPriority priority,
        DateTimeOffset firstResponseDue,
        DateTimeOffset resolutionDue,
        DateTimeOffset now)
    {
        ValidarTitulo(title);
        ValidarDescricao(description);

        return new Ticket
        {
            Id = Guid.NewGuid(),
            Number = number,
            Title = title.Trim(),
            Description = description.Trim(),
            RequesterId = requesterId,
            DepartmentId = departmentId,
            CategoryId = categoryId,
            Priority = priority,
            Status = TicketStatus.New,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            FirstResponseDueAtUtc = firstResponseDue,
            ResolutionDueAtUtc = resolutionDue,
            ReopenCount = 0
        };
    }

    public void Transition(TicketStatus novoStatus, Guid alteradoPorUserId, DateTimeOffset now, string? motivo = null)
    {
        if (!TransicoesPermitidas.TryGetValue(Status, out var permitidos) || !permitidos.Contains(novoStatus))
            throw new InvalidStatusTransitionException(Status, novoStatus);

        var anterior = Status;
        Status = novoStatus;
        UpdatedAtUtc = now;

        if (novoStatus == TicketStatus.Resolved)
            ResolvedAtUtc = now;
        else if (novoStatus == TicketStatus.Closed)
            ClosedAtUtc = now;
        else if (novoStatus == TicketStatus.InProgress && anterior is TicketStatus.Resolved or TicketStatus.Closed)
        {
            ReopenCount++;
            ResolvedAtUtc = null;
            ClosedAtUtc = null;
        }

        _statusHistory.Add(TicketStatusHistory.Create(Id, anterior, novoStatus, alteradoPorUserId, now, motivo));
    }

    public bool CanTransitionTo(TicketStatus novoStatus) =>
        TransicoesPermitidas.TryGetValue(Status, out var permitidos) && permitidos.Contains(novoStatus);

    public void Assign(Guid? agenteId, Guid alteradoPorUserId, DateTimeOffset now, string? motivo = null)
    {
        if (Status == TicketStatus.Cancelled)
            throw new DomainException("Não é possível atribuir um chamado cancelado.");
        if (Status == TicketStatus.Closed)
            throw new DomainException("Não é possível atribuir um chamado encerrado.");

        var anterior = AssignedAgentId;
        AssignedAgentId = agenteId;
        UpdatedAtUtc = now;

        _assignmentHistory.Add(TicketAssignmentHistory.Create(Id, anterior, agenteId, alteradoPorUserId, now, motivo));
    }

    public void Resolve(string resumoResolucao, Guid alteradoPorUserId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(resumoResolucao))
            throw new DomainException("O resumo da resolução é obrigatório para encerrar o chamado.");
        if (resumoResolucao.Length > ResolutionSummaryMaxLength)
            throw new DomainException($"O resumo da resolução não pode ultrapassar {ResolutionSummaryMaxLength} caracteres.");

        ResolutionSummary = resumoResolucao.Trim();
        Transition(TicketStatus.Resolved, alteradoPorUserId, now);
    }

    public void Reopen(Guid alteradoPorUserId, DateTimeOffset now, DateTimeOffset? closedAt = null)
    {
        if (Status != TicketStatus.Resolved && Status != TicketStatus.Closed)
            throw new DomainException("Somente chamados resolvidos ou encerrados podem ser reabertos.");

        var dataReferencia = ClosedAtUtc ?? ResolvedAtUtc ?? now;
        var janelaExpirada = now > dataReferencia.AddDays(ReopenWindowDays);
        if (janelaExpirada)
            throw new DomainException($"O prazo de reabertura de {ReopenWindowDays} dias expirou.");

        Transition(TicketStatus.InProgress, alteradoPorUserId, now, "Chamado reaberto.");
        ResolutionSummary = null;
    }

    public void Cancel(Guid alteradoPorUserId, DateTimeOffset now, string? motivo = null)
    {
        if (Status == TicketStatus.Cancelled)
            throw new DomainException("O chamado já está cancelado.");
        if (Status is TicketStatus.Resolved or TicketStatus.Closed)
            throw new DomainException("Não é possível cancelar um chamado resolvido ou encerrado.");

        Transition(TicketStatus.Cancelled, alteradoPorUserId, now, motivo);
    }

    public void RecordFirstResponse(DateTimeOffset now)
    {
        if (FirstResponseAtUtc.HasValue) return;
        FirstResponseAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void UpdatePriority(TicketPriority novaPrioridade, DateTimeOffset now)
    {
        Priority = novaPrioridade;
        UpdatedAtUtc = now;
    }

    public TicketComment AddPublicComment(Guid autorId, string conteudo, DateTimeOffset now)
    {
        if (Status is TicketStatus.Closed or TicketStatus.Cancelled)
            throw new DomainException("Não é possível adicionar comentários a um chamado encerrado ou cancelado.");

        var comentario = TicketComment.CreatePublic(Id, autorId, conteudo, now);
        _comments.Add(comentario);
        UpdatedAtUtc = now;
        return comentario;
    }

    public TicketComment AddInternalNote(Guid autorId, string conteudo, DateTimeOffset now)
    {
        if (Status is TicketStatus.Closed or TicketStatus.Cancelled)
            throw new DomainException("Não é possível adicionar notas a um chamado encerrado ou cancelado.");

        var nota = TicketComment.CreateInternal(Id, autorId, conteudo, now);
        _comments.Add(nota);
        UpdatedAtUtc = now;
        return nota;
    }

    public TicketAttachment AddAttachment(
        Guid uploadedByUserId,
        string originalFileName,
        string storedFileName,
        string contentType,
        long fileSize,
        string storagePath,
        string fileHash,
        DateTimeOffset now)
    {
        if (_attachments.Count >= TicketAttachment.MaxAttachmentsPerTicket)
            throw new DomainException($"Limite de {TicketAttachment.MaxAttachmentsPerTicket} anexos por chamado atingido.");

        var anexo = TicketAttachment.Create(
            Id, uploadedByUserId, originalFileName, storedFileName,
            contentType, fileSize, storagePath, fileHash, now);
        _attachments.Add(anexo);
        UpdatedAtUtc = now;
        return anexo;
    }

    public TicketRating AddRating(Guid requesterId, int nota, string? comentario, DateTimeOffset now)
    {
        if (requesterId != RequesterId)
            throw new DomainException("Somente o solicitante do chamado pode avaliá-lo.");
        if (Status is not (TicketStatus.Resolved or TicketStatus.Closed))
            throw new DomainException("O chamado só pode ser avaliado após resolução ou encerramento.");
        if (Rating != null)
            throw new DomainException("Este chamado já foi avaliado.");

        Rating = TicketRating.Create(Id, requesterId, nota, comentario, now);
        UpdatedAtUtc = now;
        return Rating;
    }

    public bool IsFirstResponseBreached(DateTimeOffset now) =>
        !FirstResponseAtUtc.HasValue && now > FirstResponseDueAtUtc;

    public bool IsResolutionBreached(DateTimeOffset now) =>
        Status is not (TicketStatus.Resolved or TicketStatus.Closed or TicketStatus.Cancelled)
        && now > ResolutionDueAtUtc;

    private static void ValidarTitulo(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("O título é obrigatório.", nameof(title));
        if (title.Length > TitleMaxLength)
            throw new ArgumentException($"O título não pode ultrapassar {TitleMaxLength} caracteres.", nameof(title));
    }

    private static void ValidarDescricao(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("A descrição é obrigatória.", nameof(description));
        if (description.Length > DescriptionMaxLength)
            throw new ArgumentException($"A descrição não pode ultrapassar {DescriptionMaxLength} caracteres.", nameof(description));
    }
}
