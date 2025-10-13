using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Live_Demo_Sensors")]
[Index("DemoId", "SensorId", Name = "uq_demo_sensor", IsUnique = true)]
public partial class LiveDemoSensor
{
    [Key]
    [Column("lds_id")]
    public int LdsId { get; set; }

    [Column("demo_id")]
    public int DemoId { get; set; }

    [Column("sensor_id")]
    public int SensorId { get; set; }

    [ForeignKey("DemoId")]
    [InverseProperty("LiveDemoSensors")]
    public virtual LiveDemo Demo { get; set; } = null!;

    [ForeignKey("SensorId")]
    [InverseProperty("LiveDemoSensors")]
    public virtual Sensor Sensor { get; set; } = null!;
}
