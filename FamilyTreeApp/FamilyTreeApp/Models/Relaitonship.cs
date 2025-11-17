namespace FamilyTreeApp.Models;

public enum RelationshipType
{
    ParentChild, // Ebeveyn -> Çocuk (Dikey çizgi)
    Spouse       // Eş (Yatay çift çizgi)
}

public class Relationship
{
    public string FromPersonId { get; set; }
    public string ToPersonId { get; set; }
    public RelationshipType Type { get; set; }
}