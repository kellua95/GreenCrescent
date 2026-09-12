using GreenCrescent.Core.Common;

namespace GreenCrescent.Core.Entities;

public sealed class ResponsibleSheikh : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Sponsorship> Sponsorships { get; set; }
        = new List<Sponsorship>();
}