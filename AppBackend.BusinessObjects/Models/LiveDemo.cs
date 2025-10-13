using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Live_Demo")]
public partial class LiveDemo
{
    [Key]
    [Column("demo_id")]
    public int DemoId { get; set; }

    [Column("project_id")]
    public int? ProjectId { get; set; }

    [Column("protocol")]
    [StringLength(255)]
    public string? Protocol { get; set; }

    [Column("demo_url")]
    public string? DemoUrl { get; set; }

    [Column("started_at")]
    [Precision(0)]
    public DateTime? StartedAt { get; set; }

    [Column("ended_at")]
    [Precision(0)]
    public DateTime? EndedAt { get; set; }

    [Column("sensor_id")]
    public int? SensorId { get; set; }

    [InverseProperty("Demo")]
    public virtual ICollection<LiveDemoSensor> LiveDemoSensors { get; set; } = new List<LiveDemoSensor>();

    [ForeignKey("ProjectId")]
    [InverseProperty("LiveDemos")]
    public virtual Project? Project { get; set; }

    [ForeignKey("SensorId")]
    [InverseProperty("LiveDemos")]
    public virtual Sensor? Sensor { get; set; }
}
