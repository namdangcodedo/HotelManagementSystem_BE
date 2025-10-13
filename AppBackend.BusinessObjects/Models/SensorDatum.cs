using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("sensor_data")]
[Index("SensorId", "Timestamp", Name = "idx_sensor_ts")]
public partial class SensorDatum
{
    [Key]
    [Column("data_id")]
    public long DataId { get; set; }

    [Column("sensor_id")]
    public int? SensorId { get; set; }

    [Column("timestamp")]
    [Precision(0)]
    public DateTime? Timestamp { get; set; }

    [Column("value")]
    public double? Value { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string? Status { get; set; }

    [Column("raw_payload")]
    public string? RawPayload { get; set; }

    [ForeignKey("SensorId")]
    [InverseProperty("SensorData")]
    public virtual Sensor? Sensor { get; set; }
}
