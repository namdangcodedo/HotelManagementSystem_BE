using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("sensors")]
[Index("ProjectId", Name = "IX_sensors_project_id")]
public partial class Sensor
{
    [Key]
    [Column("sensor_id")]
    public int SensorId { get; set; }

    [Column("project_id")]
    public int? ProjectId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string? Name { get; set; }

    [Column("type")]
    [StringLength(100)]
    public string? Type { get; set; }

    [Column("unit")]
    [StringLength(50)]
    public string? Unit { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime? CreatedAt { get; set; }

    [InverseProperty("Sensor")]
    public virtual ICollection<LiveDemoSensor> LiveDemoSensors { get; set; } = new List<LiveDemoSensor>();

    [InverseProperty("Sensor")]
    public virtual ICollection<LiveDemo> LiveDemos { get; set; } = new List<LiveDemo>();

    [ForeignKey("ProjectId")]
    [InverseProperty("Sensors")]
    public virtual Project? Project { get; set; }

    [InverseProperty("Sensor")]
    public virtual ICollection<SensorDatum> SensorData { get; set; } = new List<SensorDatum>();
}
