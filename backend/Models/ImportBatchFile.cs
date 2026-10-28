using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class ImportBatchFile
{
    public Guid Id { get; set; }

    public Guid ImportBatchId { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>
    /// SHA-256 för dublettskydd, förhindra att man laddar upp samma fil mer än en gång
    /// </summary>
    public string FileHash { get; set; } = null!;

    public virtual ImportBatch ImportBatch { get; set; } = null!;
}
