using System;
using System.Collections.Generic;

namespace FactoryManagementSystem.Models;

public partial class MESCompleteBatch
{
    public long Id { get; set; }

    public string? ProductionOrder { get; set; }

    public string? BatchNumber { get; set; }

    public decimal? BatchSize { get; set; }

    public string? BatchUOM { get; set; }

    public string? ProductCode { get; set; }

    public string? MachineCode { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? TransferStatus { get; set; }

    public int? RetryCount { get; set; }

    public DateTime? NextRetryAt { get; set; }

    public DateTime? ProcessingAt { get; set; }

    public string? RequestJson { get; set; }

    public string? ResponseContent { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
