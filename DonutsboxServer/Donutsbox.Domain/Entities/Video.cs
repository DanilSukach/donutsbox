using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Donutsbox.Domain.Entities;

public class Video
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "PENDING"; // PENDING, UPLOADED, PROCESSING, READY, FAILED
    public string ObjectKey { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
