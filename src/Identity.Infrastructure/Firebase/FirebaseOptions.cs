using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Infrastructure.Firebase;

public class FirebaseOptions {
    public string ProjectId { get; set; } = string.Empty;
    public string AuthEmulatorHost { get; set; } = string.Empty;
    public string ServiceAccountJson { get; set; } = string.Empty;
}
